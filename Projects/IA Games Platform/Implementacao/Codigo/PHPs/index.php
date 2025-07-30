<?php
session_start(); // inicia a sessão 

// cabeçalhos para desativar cache do browser 
header("Cache-Control: no-store, no-cache, must-revalidate, max-age=0");
header("Cache-Control: post-check=0, pre-check=0", false);
header("Pragma: no-cache");

require_once 'db.php'; // inclui o arquivo de conexão com a base de dados
$conn = db_connect(); // método de conexão à base de dados

// função que retorna o ranking dos 3 melhores jogadores de um jogo específico
function getGameLeaderboard($conn, $game)
{
  // prepara a consulta para buscar username, rating e foto dos jogadores
  $stmt = $conn->prepare("
        SELECT person.username, playergame.rating, person.photoURL
        FROM playergame
        JOIN person ON playergame.userId = person.userId
        WHERE playergame.gameTitle = ?
        ORDER BY playergame.rating DESC
        LIMIT 3
    ");

  $stmt->bind_param("s", $game);  
  $result = $stmt->get_result();  // obtém resultados

  $leaderboard = [];
  while ($row = $result->fetch_assoc()) {
    $leaderboard[] = $row;  //adiciona cada jogador ao array do ranking
  }

  return $leaderboard; // retorna lista dos top 3 jogadores
}

?>

<!DOCTYPE html>
<html lang="en">

<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <link href="https://fonts.googleapis.com/css2?family=Rubik&display=swap" rel="stylesheet">
  <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.1/font/bootstrap-icons.css" rel="stylesheet">
  <link rel="stylesheet" type="text/css" href="css/main.css">
</head>

<body>
  <!-- navbar -->
  <nav class="navbar">
    <img src="images/image.png" alt="" width="350">

    <div class="nav-links" id="navLinks">
      <a href="#">Home</a>
      <a href="#OurGames">Our Games</a>
      <a href="#LeaderBoard">LeaderBoard</a>
      <a href="#AboutUs">About Us</a>
      <?php if (isset($_SESSION["user"])): ?>
        <!-- se o utilizador estiver logged in, mostra ícone de perfil -->
        <a href="profile.php" class="profile-btn">
          <i class="bi bi-person-circle profile-icon"></i>
        </a>
      <?php else: ?>
        <!-- caso contrário, mostra botão de login -->
        <a href="login.php" class="login-btn">Login</a>
      <?php endif; ?>
    </div>

    <!-- botão toggle para menu responsivo -->
    <div class="menu-toggle" id="menuToggle">
      <i class="bi bi-list"></i>
    </div>
  </nav>

  <!-- secção de jogos -->
  <section id="OurGames" class="games-section">
    <h2 class="section-title">Our Games</h2>

    <div class="games-grid">
      <!-- cartão do jogo do galo -->
      <div class="game-card">
        <a href="gamepick.php?scene=TicTacToe">
          <img src="images\Remix Icons for Figma (Community) (2).png" alt="Game 2" class="game-image">
        </a>
        <div class="game-info">
          <h3 class="game-title">Tic Tac Toe</h3>
        </div>
      </div>

      <!-- cartão do quatro em linha -->
      <div class="game-card">
        <a href="gamepick.php?scene=Connect4">
          <img src="images\Connect4.png" alt="Game 2" class="game-image">
        </a>
        <div class="game-info">
          <h3 class="game-title">Connect 4</h3>
        </div>
      </div>
    </div>
  </section>

  <!-- secção de quadro de honra com ranking dos jogos -->
  <section id="LeaderBoard" class="leaderboard-section">
    <h2 class="leaderboard-section-title">LeaderBoard</h2>

    <div class="leaderboard-grid">
      <?php
      // vai buscar todos os títulos  dos jogos na db
      $stmt = $conn->prepare("SELECT DISTINCT title FROM game");
      $stmt->execute();
      $result = $stmt->get_result();

      // para cada jogo, obtém o ranking e mostra
      while ($row = $result->fetch_assoc()) {
        $game = $row['title'];
        $leaderboard = getGameLeaderboard($conn, $game);
      ?>
        <div class="leaderboard-card">
          <h3><?php echo $game; ?></h3>
          <ul class="top-players">
            <?php foreach ($leaderboard as $index => $player): ?>
              <li>
                <span class="rank"><?php echo $index + 1; ?></span>
                <img src="<?php echo htmlspecialchars($player['photoURL']); ?>" alt="<?php echo htmlspecialchars($player['username']); ?>">
                <span class="leaderboard-username"><?php echo $player['username']; ?></span>
                <span class="elo">ELO: <?php echo $player['rating']; ?></span>
              </li>
            <?php endforeach; ?>
          </ul>
        </div>
      <?php
      }
      ?>
    </div>
  </section>

  <!-- secção de informação -->
  <section id="AboutUs" class="about-section">
    <h2 class="about-title">About Us</h2>
    <p class="about-text">
      We are a team of college students dedicated to developing games for all ages. We hope to bring classic games through your digital device, whether you're a casual player or competitive.
    </p>
  </section>

  <!-- script para toggle do menu responsivo -->
  <script>
    const menuToggle = document.getElementById('menuToggle');
    const navLinks = document.getElementById('navLinks');
    const icon = menuToggle.querySelector('i');

    menuToggle.addEventListener('click', () => {
      navLinks.classList.toggle('active'); 
      icon.classList.toggle('bi-list');    
      icon.classList.toggle('bi-x'); 
    });
  </script>

</body>

</html>
