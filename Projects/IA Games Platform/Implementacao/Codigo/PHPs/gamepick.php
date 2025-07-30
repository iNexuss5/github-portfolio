<?php
session_start(); // inicia a sessão

require 'db.php'; // inclui o arquivo de conexão com a base de dados

// verifica se o utilizador está logged in, se não estiver, redireciona para a página de login
if (!isset($_SESSION['user'])) {
    header('Location: login.php');
    exit;
}

// recupera os dados do utilizador 
$user = $_SESSION['user'];

// recupera qual é a cena do Unity da URL, se não estiver presente, usa 'MenuInicial' como padrão
$scene = $_GET['scene'] ?? 'MenuInicial';
?>



<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Profile Page</title>

    <!-- Fonte Rubik do Google Fonts -->
    <link href="https://fonts.googleapis.com/css2?family=Rubik&display=swap" rel="stylesheet" />

    <!-- CSS principal do site -->
    <link rel="stylesheet" href="css/main.css" />

    <!-- Ícones do Bootstrap (como o ícone de perfil) -->
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
        <a href="index.php#AboutUs">About</a>
        <a href="#">Contact Us</a>

        <a href="profile.php" class="profile-btn">
            <i class="bi bi-person-circle profile-icon"></i> 
        </a>
    </div>

    <div class="menu-toggle" id="menuToggle">
        ☰
    </div>
</nav>

<!-- secção para escolher o adversário -->
<div class="choose-adversary-section">
    <h1 class="choose-section-title">Pick your Opponent</h1>

    <div class="button-container">
        <!-- botão para jogar contra a IA e redireciona para a página de escolha de dificuldade -->
        <a href="dificultypick.php?scene=<?= urlencode($scene) ?>">
            <button class="choose-button">COM</button>
        </a>

        <!-- botão para jogar multiplayer (humano vs humano), IA desativada -->
        <a href="game.php?scene=<?= urlencode($scene) ?>&ai=0">
            <button class="choose-button">Multiplayer</button>
        </a>
    </div>
</div>

</body>
</html>
