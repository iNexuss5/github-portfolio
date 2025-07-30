<?php
session_start(); // inicia a sessão 
require 'db.php'; // inclui o arquivo de conexão com a base de dados

// verifica se o utilizador está autenticado
if (!isset($_SESSION['user'])) {
    header('Location: login.php'); 
    exit;
}

// R«recupera os dados do utilizador armazenados na sessão
$user = $_SESSION['user'];

// obtém a cena do Unity da URL, ou define "MenuInicial" como padrão
$scene = $_GET['scene'] ?? 'MenuInicial';
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

    <div class="menu-toggle" id="menuToggle">
        ☰
    </div>
</nav>

<!-- secção com escolha da dificuldade da IA -->
<div class="choose-adversary-section">
    <h1 class="choose-section-title">Pick the AI difficulty</h1>

    <!-- botões de escolha de dificuldade  -->
    <div class="button-container">
        <!-- cada botão redireciona para "game.php" com os parâmetros da cena atual e dificuldade da IA -->
        <a href="game.php?scene=<?= urlencode($scene) ?>&ai=1"><button class="choose-button">Beginner</button></a>
        <a href="game.php?scene=<?= urlencode($scene) ?>&ai=2"><button class="choose-button">Intermediate</button></a>
        <a href="game.php?scene=<?= urlencode($scene) ?>&ai=3"><button class="choose-button">Advanced</button></a>
    </div>
</div>

</body>
</html>
