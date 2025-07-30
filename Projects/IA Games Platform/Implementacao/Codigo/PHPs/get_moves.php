<?php
// define o tipo de conteúdo da resposta como JSON
header("Content-Type: application/json");

require_once 'db.php'; // inclui o arquivo de conexão com a base de dados
$conn = db_connect(); // método de conexão à base de dados

// verifica se o matchId foi passado no URL 
if (!isset($_GET['matchId'])) {
    // se não foi passado, retorna um erro em formato JSON e encerra a execução
    echo json_encode(["error" => "matchId is required"]);
    exit;
}

// Recupera o valor de matchId da URL
$matchId = $_GET['matchId'];

// prepara uma query para buscar os movimentos da partida com o matchId fornecido
// a ordenação por 'id' garante que os movimentos sejam retornados na ordem em que ocorreram
$stmt = $conn->prepare("SELECT origin, destiny, piece, turn FROM move WHERE matchId = ? ORDER BY id");

$stmt->bind_param("i", $matchId);

$stmt->execute();

$result = $stmt->get_result();

//cria um array para armazenar os movimentos
$moves = [];

// percorre todas as linhas do resultado e adiciona ao array
while ($row = $result->fetch_assoc()) {
    $moves[] = $row;
}

// retorna os movimentos em formato JSON com a chave "moves"
echo json_encode(["moves" => $moves]);

$stmt->close();
$conn->close();
?>
