<?php
session_start(); // inicia a sessão

require_once 'db.php'; // inclui o arquivo de conexão com a base de dados
$conn = db_connect(); // método de conexão à base de dados

// Verifica se o usuário está logado
if (!isset($_SESSION['user'])) {
    header('Location: login.php'); // Redireciona se não estiver autenticado
    exit;
}

// dados do utilizador da sessão
$user = $_SESSION['user'];
$username = $_SESSION['user']['username'];
$userId = $_SESSION['user']['userId'];


 // função para atribuir badge se o utilizador assim for elegível
function assignBadgeIfEligible($conn, $userId, $title, $imagePath, $description, $eligibilitySql, $eligibilityParamTypes, ...$eligibilityParamsAndMinCount) {
    $minCount = 1;
    if (count($eligibilityParamsAndMinCount) > 0 && is_int(end($eligibilityParamsAndMinCount))) {
        $minCount = array_pop($eligibilityParamsAndMinCount); // último parâmetro é o mínimo de contagem
    }
    $eligibilityParams = $eligibilityParamsAndMinCount;

    // verifica se o utilizador já tem o badge
    $stmt = $conn->prepare("SELECT COUNT(*) FROM personbadge WHERE userId = ? AND title = ?");
    $stmt->bind_param("is", $userId, $title);
    $stmt->execute();
    $stmt->store_result();
    $stmt->bind_result($count);
    $stmt->fetch();
    $stmt->close();

    $hasBadge = false;

    if ($count > 0) {
        $hasBadge = true; 
    } else {
        // verifica elegibilidade para o badge
        $stmt = $conn->prepare($eligibilitySql);
        $stmt->bind_param($eligibilityParamTypes, ...$eligibilityParams);
        $stmt->execute();
        $stmt->store_result();
        $stmt->bind_result($eligibleCount);
        $stmt->fetch();
        $stmt->close();

        if ($eligibleCount >= $minCount) {
            // atribui o badge
            $stmt = $conn->prepare("INSERT INTO personbadge (userId, title, dtAcquired) VALUES (?, ?, NOW())");
            $stmt->bind_param("is", $userId, $title);
            $stmt->execute();
            $stmt->close();
            $hasBadge = true;
        }
    }

    // exibe badge na interface se o utilizador o tiver
    if ($hasBadge) {
        echo <<<HTML
        <div class="badge-wrapper">
          <img src="$imagePath" alt="Badge" class="badge" />
          <div class="badge-tooltip">
            <strong>$title</strong><br>
            $description
          </div>
        </div>
        HTML;
    }
}

//busca movimentos de uma partida
function getGameMoves($conn, $matchId) {
    $stmt = $conn->prepare("SELECT origin, destiny, piece, turn from gamemove WHERE matchId = ? ORDER BY id");
    $stmt->bind_param("i", $matchId);
    $stmt->execute();
    $result = $stmt->get_result();
    $moves = [];
    while ($row = $result->fetch_assoc()) {
        $moves[] = $row;
    }
    $stmt->close();
    return $moves;
}
?>
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Profile Page</title>
    <link href="https://fonts.googleapis.com/css2?family=Rubik&display=swap" rel="stylesheet" />
    <link rel="stylesheet" href="css/main.css" />
    <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons/font/bootstrap-icons.css" rel="stylesheet">
</head>
<body>

<!-- navbar -->
<nav class="navbar">
    <img src="images/image.png" alt="" width="350">
    <div class="nav-links" id="navLinks">
        <a href="index.php">Home</a>
        <a href="index.php#OurGames">Our Games</a>
        <a href="index.php#LeaderBoard">LeaderBoard</a>
        <a href="index.php#AboutUs">About Us</a>
        <a href="profile.php" class="profile-btn">
            <i class="bi bi-person-circle profile-icon"></i>
        </a>
    </div>
    <div class="menu-toggle" id="menuToggle">☰</div>
</nav>

<!-- secção do perfil -->
<section class="profile-section">
    <div class="profile-header">
        <img src="<?= htmlspecialchars($user['photoURL']) ?>" alt="Foto de Perfil" class="profile-photo">
        <div class="username"><?= htmlspecialchars($user['username']) ?></div>
        <div class="email"><?= htmlspecialchars($user['email']) ?></div>
        <div class="logout-container">
            <a href="logout.php" class="logout-btn">Logout</a>
        </div>
    </div>

    <!-- badges -->
    <div class="badge-container">
        <h3>Achievements</h3>
        <div class="badges">
            <?php
            assignBadgeIfEligible($conn, $userId, 'First Match', 'images/primeirapartida.png', 'Played your very first match',
                "SELECT COUNT(*) FROM playermatch WHERE username = ?", "s", $username, 1);
            assignBadgeIfEligible($conn, $userId, 'Grinder', 'images/maratonista.png', 'Played 10 matches in a single day',
                "SELECT COUNT(*) FROM playermatch WHERE username = ?", "s", $username, 10);
            assignBadgeIfEligible($conn, $userId, 'Veteran', 'images/veterano.png', 'Played 100 matches',
                "SELECT COUNT(*) FROM playermatch WHERE username = ?", "s", $username, 100);
            assignBadgeIfEligible($conn, $userId, 'Breakthrough Win', 'images/vencedorestreado.png', 'Won your first match',
                "SELECT COUNT(*) FROM playermatch p JOIN gamematch g ON p.matchId = g.matchId WHERE p.username = ? AND p.playernum = g.winner", "s", $username, 1);
            assignBadgeIfEligible($conn, $userId, 'Unstoppable', 'images/invicto.png', 'Won 5 matches in a day',
                "SELECT COUNT(*) FROM playermatch p JOIN gamematch g ON p.matchId = g.matchId WHERE p.username = ? AND p.playernum = g.winner GROUP BY DATE(g.dhInicio) HAVING COUNT(*) >= 5", "s", $username, 5);
            assignBadgeIfEligible($conn, $userId, 'Game Master', 'images/mestredejogo.png', 'Won 100 matches',
                "SELECT COUNT(*) FROM playermatch p JOIN gamematch g ON p.matchId = g.matchId WHERE p.username = ? AND p.playernum = g.winner", "s", $username, 100);
            assignBadgeIfEligible($conn, $userId, 'High Performer', 'images/altaperformance.png', 'Reached Top 30 in a game',
                "WITH RankedPlayers AS (
                    SELECT pg.userId, pg.gameTitle, pg.rating,
                          ROW_NUMBER() OVER (PARTITION BY pg.gameTitle ORDER BY pg.rating DESC) AS rank_position
                    FROM playergame pg
                  )
                  SELECT 1 FROM RankedPlayers WHERE userId = ? AND rank_position <= 30 LIMIT 1", "i", $userId, 1);
            assignBadgeIfEligible($conn, $userId, 'Elite Ranker', 'images/elitecompetitiva.png', 'Reached Top 10 in a game',
                "WITH RankedPlayers AS (
                    SELECT pg.userId, pg.gameTitle, pg.rating,
                          ROW_NUMBER() OVER (PARTITION BY pg.gameTitle ORDER BY pg.rating DESC) AS rank_position
                    FROM playergame pg
                  )
                  SELECT 1 FROM RankedPlayers WHERE userId = ? AND rank_position <= 10 LIMIT 1", "i", $userId, 1);
            assignBadgeIfEligible($conn, $userId, 'Podium Legend', 'images/lendanopodio.png', 'Reached Top 3 in a game',
                "WITH RankedPlayers AS (
                    SELECT pg.userId, pg.gameTitle, pg.rating,
                          ROW_NUMBER() OVER (PARTITION BY pg.gameTitle ORDER BY pg.rating DESC) AS rank_position
                    FROM playergame pg
                  )
                  SELECT 1 FROM RankedPlayers WHERE userId = ? AND rank_position <= 3 LIMIT 1", "i", $userId, 1);
            assignBadgeIfEligible($conn, $userId, 'Invincible', 'images/invencivel.png', 'Reached Top 1 in a game',
                "WITH RankedPlayers AS (
                    SELECT pg.userId, pg.gameTitle, pg.rating,
                          ROW_NUMBER() OVER (PARTITION BY pg.gameTitle ORDER BY pg.rating DESC) AS rank_position
                    FROM playergame pg
                  )
                  SELECT 1 FROM RankedPlayers WHERE userId = ? AND rank_position <= 1 LIMIT 1", "i", $userId, 1);
            ?>
        </div>
    </div>

    <!-- jogos que o utilizador já jogou e respetivo rating -->
    <?php
    $stmt = $conn->prepare("SELECT 1 FROM playergame WHERE userId = ? LIMIT 1");
    $stmt->bind_param("i", $userId);
    $stmt->execute();
    $result = $stmt->get_result();
    ?>
    <?php if ($result->num_rows > 0): ?>
    <div class="ratings-section">
        <h3>Game Ratings</h3>
        <div class="game-ratings">
            <?php
            $sql = "SELECT gameTitle, rating FROM playergame WHERE userId = ?";
            $stmt = $conn->prepare($sql);
            $stmt->bind_param("i", $userId);
            $stmt->execute();
            $result = $stmt->get_result();
            while ($row = $result->fetch_assoc()) {
                echo '<div class="game-rating">';
                echo "<h4>{$row['gameTitle']}</h4>";
                echo "<p>Rating: {$row['rating']}</p>";
                echo '</div>';
            }
            $stmt->close();
            ?>
        </div>
    </div>
    <?php endif; ?>

    <!-- histórico de partidas -->
    <?php
    $stmt = $conn->prepare("
        SELECT gm.matchId, gm.title, gm.dhFim
        FROM gamematch gm
        JOIN playermatch pm ON gm.matchId = pm.matchId
        WHERE pm.username = ?
        ORDER BY gm.dhFim DESC
    ");
    $stmt->bind_param("s", $username);
    $stmt->execute();
    $result = $stmt->get_result();

    if ($result->num_rows === 0) {
        echo "<p>No previous games found.</p>";
    } else {
        echo '<div class="previous-games">';
        while ($row = $result->fetch_assoc()) {
            $matchId = htmlspecialchars($row['matchId']);
            $title = htmlspecialchars($row['title']);
            $dhFim = htmlspecialchars($row['dhFim']);

            echo '
            <div class="replay-box">
                <h4>' . $title . '</h4>
                <p>Last played: ' . $dhFim . '</p>
                <a href="game.php?matchId=' . $matchId . '&scene='. $title .'" class="review-btn">View Game</a>
            </div>';
        }
        echo '</div>';
    }
    $stmt->close();
    ?>
</section>

<!-- script para mostrar/esconder a descrição do badge -->
<script>
    const badges = document.querySelectorAll('.badge-wrapper');
    badges.forEach(wrapper => {
        const badge = wrapper.querySelector('.badge');
        const tooltip = wrapper.querySelector('.badge-tooltip');
        badge.addEventListener('click', (e) => {
            e.stopPropagation();
            document.querySelectorAll('.badge-tooltip').forEach(t => {
                if (t !== tooltip) t.classList.remove('active');
            });
            tooltip.classList.toggle('active');
        });
    });
    document.addEventListener('click', () => {
        document.querySelectorAll('.badge-tooltip').forEach(t => {
            t.classList.remove('active');
        });
    });
</script>

</body>
</html>
