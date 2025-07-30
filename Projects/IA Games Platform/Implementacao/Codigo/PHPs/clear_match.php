<?php
session_start(); // inicia a sessão
unset($_SESSION['matchId']); // limpa o ID da partida da sessão
echo "OK";
?>
