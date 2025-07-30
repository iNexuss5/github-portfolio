<?php
session_start(); // inicia a sessão

// fefine cabeçalhos HTTP para evitar cache no browser
header("Cache-Control: no-cache, must-revalidate");


header('Content-Type: text/html; charset=utf-8');

// retorna uma resposta em formato JSON para o Unity com informações da sessão:
// - 'scene': nome da cena que deve ser carregada (ou 'MenuInicial' se não estiver definido)
// - 'ai_dif': nível de dificuldade da IA (ou '0' por padrão)
// - 'matchId': identificador da partida (ou string vazia se não definido)
echo json_encode([
    'scene' => $_SESSION['scene_to_load'] ?? 'MenuInicial',
    'ai_dif' => $_SESSION['ai_dif'] ?? '0',
    'matchId' => $_SESSION['matchId'] ?? ""
]);
