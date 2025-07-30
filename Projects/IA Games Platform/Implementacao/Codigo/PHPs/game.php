<?php
session_start(); // inicia a sessão
require_once 'db.php'; // inclui o arquivo de conexão com a base de dados
$conn = db_connect(); // método de conexão à base de dados

// verifica se o utilizador está autenticado
if (!isset($_SESSION["user"])) {
    header("Location: login.php");
    exit();
}

// define a cena (nome do jogo)
$scene = $_GET['scene'] ?? 'MenuInicial';
$_SESSION['scene_to_load'] = $scene;

// Guarda o utilizador atual
$user = $_SESSION["user"];

// verifica se há dificuldade da AI
$ai_dif = $_GET['ai'] ?? '0';
$_SESSION['ai_dif'] = $ai_dif;

// se foi passado um matchId  via GET e ainda não está na sessão, guarda-o, , ou seja, o jogo a abrir não é novo e sim para ser revisto
if (!isset($_SESSION['matchId']) && isset($_GET['matchId'])) {
    $_SESSION['matchId'] = $_GET['matchId'];
}

// recebe dados do Unity via JSON
$data = json_decode(file_get_contents("php://input"), true);

// função para buscar o rating de um jogador
function getUserRating($conn, $username, $gameTitle) {
    // obtém userId
    $sql = "SELECT userId FROM person WHERE username = ?";
    $stmt = $conn->prepare($sql);
    $stmt->bind_param("s", $username);
    $stmt->execute();
    $result = $stmt->get_result();
    if (!$row = $result->fetch_assoc()) {
        throw new Exception("Usuário '$username' não encontrado.");
    }
    $userId = $row['userId'];
    $stmt->close();

    // obtém rating do jogo
    $sql = "SELECT rating FROM playergame WHERE userId = ? AND gameTitle = ?";
    $stmt = $conn->prepare($sql);
    $stmt->bind_param("is", $userId, $gameTitle);
    $stmt->execute();
    $result = $stmt->get_result();
    $rating = ($row = $result->fetch_assoc()) ? $row['rating'] : 400; // valor default = 400
    $stmt->close();

    return ['userId' => $userId, 'rating' => $rating];
}

// função para calcular Elo entre dois jogadores
function calcularElo($ra, $rb, $scoreA, $k = 32) {
    $ea = 1 / (1 + pow(10, ($rb - $ra) / 400));
    return $k * ($scoreA - $ea);
}

// se foram recebidos dados do Unity
if ($data) {
    $type = $data['type'];

    // envio de partida
    if ($type == "sendMatch") {
        $title = $data['title'];
        $maxUsers = $data['max_users'];
        $ai_dif = $data['ai_dif'] ?? '0';

        // verifica se existe um GameMatch com só 1 jogador
        $sql = "
            SELECT gm.matchId
            FROM GameMatch gm
            WHERE gm.matchId IN (
                SELECT pm.matchId
                FROM PlayerMatch pm
                GROUP BY pm.matchId
                HAVING COUNT(*) = 1
            )
        ";
        $stmt = $conn->prepare($sql);
        $stmt->execute();
        $result = $stmt->get_result();

        if ($result->num_rows > 0) {
            // existe partida inacabada, o utilizador junta-se
            $row = $result->fetch_assoc();
            $matchId = $row['matchId'];

            // regista jogador na tabela playermatch que associa o utilizador à partida
            $sql = "INSERT INTO playermatch (username, matchId, playernum) VALUES (?, ?, ?)";
            $playerNum = $result->num_rows + 1;
            $stmt = $conn->prepare($sql);
            $stmt->bind_param("sii", $user['username'], $matchId, $playerNum);
            $stmt->execute();
            $stmt->close();

        } else {
            // cria nova partida se não existe nenhuma
            $sql = "INSERT INTO gamematch (title, maxUsers) VALUES (?, ?)";
            $stmt = $conn->prepare($sql);
            $stmt->bind_param("si", $title, $maxUsers);
            $stmt->execute();
            $matchId = $stmt->insert_id;

            // adiciona jogador 1
            $sql = "INSERT INTO playermatch (username, matchId, playernum) VALUES (?, ?, ?)";
            $playernum = 1;
            $stmt = $conn->prepare($sql);
            $stmt->bind_param("sii", $user['username'], $matchId, $playernum);
            $stmt->execute();
            $stmt->close();

            // se for contra IA, adiciona-a como jogador 2
            if ($ai_dif != '0') {
                $aiusername = match ($ai_dif) {
                    '1' => 'EasyAI',
                    '2' => 'IntermediateAI',
                    default => 'ExpertAI'
                };

                $aiNum = 2;
                $sql = "INSERT INTO playermatch (username, matchId, playernum) VALUES (?, ?, ?)";
                $stmt = $conn->prepare($sql);
                $stmt->bind_param("sii", $aiusername, $matchId, $aiNum);
                $stmt->execute();
                $stmt->close();
            }
        }

        // salva o matchId na sessão
        unset($_SESSION['matchId']);
        $_SESSION['matchId'] = $matchId;
        session_write_close();

        echo "Sent Match";

    } elseif ($type == "sendMove" && isset($_SESSION['matchId'])) {
        // envio de jogada
        $matchId = $_SESSION['matchId'];
        $origin = $data['origin'];
        $destiny = $data['destiny'];
        $piece = $data['piece'];
        $turn = $data['turn'];

        // insere a jogada na tabela move
        $sql = "INSERT INTO move (matchId, turn, origin, destiny, piece) VALUES (?, ?, ?, ?, ?)";
        $stmt = $conn->prepare($sql);
        $stmt->bind_param("issss", $matchId, $turn, $origin, $destiny, $piece);
        $stmt->execute();
        $stmt->close();

        echo "Sent Move session id:" . $_SESSION['matchId'];

    } elseif ($type == "sendWinner" && isset($_SESSION['matchId'])) {
        // envio de fim de partida
        $matchId = $_SESSION['matchId'];
        $winnerInput = $data['winner'];
        $gameTitle = $data['title'];

        // atualiza o jogo depois de terminar
        $sql = "UPDATE gamematch SET winner = ?, dhFim = CURRENT_TIMESTAMP WHERE matchId = ?";
        $stmt = $conn->prepare($sql);
        $stmt->bind_param("si", $winnerInput, $matchId);
        $stmt->execute();
        $stmt->close();

        // obtém jogadores da partida
        $sql = "SELECT username, playerNum FROM playermatch WHERE matchId = ?";
        $stmt = $conn->prepare($sql);
        $stmt->bind_param("i", $matchId);
        $stmt->execute();
        $result = $stmt->get_result();

        $players = [];
        while ($row = $result->fetch_assoc()) {
            $players[$row['playerNum']] = $row['username'];
        }
        $stmt->close();

        if (count($players) < 2) {
            echo "Erro: menos de dois jogadores na partida.";
            exit;
        }

        // obtém dados dos jogadores
        $playersData = [];
        foreach ($players as $playerNum => $username) {
            $playersData[$playerNum] = getUserRating($conn, $username, $gameTitle);
            $playersData[$playerNum]['username'] = $username;
        }

        // define pontuações
        $scores = [];
        if ($winnerInput === "draw") {
            foreach ($playersData as $pn => $_) $scores[$pn] = 0.5;
        } else {
            $winnerNum = (int)$winnerInput;
            foreach ($playersData as $pn => $_) $scores[$pn] = ($pn === $winnerNum) ? 1 : 0;
        }

        // calcula os novos ratings
        $ratingAdjustments = [];
        foreach ($playersData as $pnA => $pdataA) {
            $adj = 0;
            foreach ($playersData as $pnB => $pdataB) {
                if ($pnA === $pnB) continue;
                $adj += calcularElo($pdataA['rating'], $pdataB['rating'], $scores[$pnA]);
            }
            $ratingAdjustments[$pnA] = $adj;
        }

        // atualiza ratings na db
        foreach ($playersData as $pn => $pdata) {
            $newRating = round($pdata['rating'] + $ratingAdjustments[$pn]);

            $sql = "INSERT INTO playergame (userId, gameTitle, rating)
                    VALUES (?, ?, ?)
                    ON DUPLICATE KEY UPDATE rating = ?";
            $stmt = $conn->prepare($sql);
            $stmt->bind_param("isii", $pdata['userId'], $gameTitle, $newRating, $newRating);
            $stmt->execute();
            $stmt->close();
        }

        // limpa matchId da sessão
        unset($_SESSION['matchId']);

        echo "Ended Match";
    }
}
?>


<!DOCTYPE html>
<html lang="en-us">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Unity WebGL Player | ProjetoLEIM</title>
    <link rel="shortcut icon" href="Build/TemplateData/favicon.ico">
    <link rel="stylesheet" href="Build/TemplateData/style.css">
    <link rel="manifest" href="Build/manifest.webmanifest">
    <link href="https://fonts.googleapis.com/css2?family=Rubik&display=swap" rel="stylesheet">
    <link rel="stylesheet" href="css/main.css">
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.10.5/font/bootstrap-icons.css">
</head>

<body class="game-body">

    <!-- navbar -->
    <nav class="navbar">
        <img src="images/image.png" alt="" width="350">
        <div class="nav-links">
            <a href="index.php">Home</a>
            <a href="index.php#OurGames">Our Games</a>
            <a href="index.php#LeaderBoard">LeaderBoard</a>
            <a href="index.php#AboutUs">About Us</a>
            <a href="profile.php" class="profile-btn">
                <i class="bi bi-person-circle profile-icon"></i>
            </a>
        </div>
        <div class="menu-toggle">☰</div>
    </nav>

    <!-- canvas do unity -->
    <div id="unity-container">
        <canvas id="unity-canvas" tabindex="-1"></canvas>
        <div id="unity-loading-bar">
            <div id="unity-progress-bar-empty">
                <div id="unity-progress-bar-full"></div>
            </div>
        </div>
        <div id="unity-warning"></div>
    </div>

    <!-- Unity loader script -->
    <script>
        window.addEventListener("beforeunload", function () {
            navigator.sendBeacon("clear_match.php");
        });

        window.addEventListener("load", function () {
            if ("serviceWorker" in navigator) {
                navigator.serviceWorker.register("ServiceWorker.js");
            }
        });

        function unityShowBanner(msg, type) {
            var warningBanner = document.querySelector("#unity-warning");
            var div = document.createElement('div');
            div.innerHTML = msg;
            div.style = type === 'error' ? 'background: red; padding: 10px;' : 'background: yellow; padding: 10px;';
            warningBanner.appendChild(div);
            if (type !== 'error') setTimeout(() => div.remove(), 5000);
        }

        // Configurações da build 
        var buildUrl = "Build";
        var loaderUrl = buildUrl + "/projeto.loader.js";
        var config = {
            dataUrl: buildUrl + "/projeto.data",
            frameworkUrl: buildUrl + "/projeto.framework.js",
            codeUrl: buildUrl + "/projeto.wasm",
            streamingAssetsUrl: buildUrl + "/StreamingAssets",
            companyName: "DefaultCompany",
            productName: "ProjetoLEIM",
            productVersion: "1.0",
            showBanner: unityShowBanner,
        };

        var script = document.createElement("script");
        script.src = loaderUrl;
        script.onload = () => {
            createUnityInstance(document.querySelector("#unity-canvas"), config, (progress) => {
                document.querySelector("#unity-progress-bar-full").style.width = 100 * progress + "%";
            }).then(() => {
                document.querySelector("#unity-loading-bar").style.display = "none";
            }).catch(alert);
        };
        document.body.appendChild(script);
    </script>
</body>
</html>
