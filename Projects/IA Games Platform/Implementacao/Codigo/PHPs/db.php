<?php

// define cabeçalhos HTTP para evitar cache no browser
header("Cache-Control: no-store, no-cache, must-revalidate, max-age=0");
header("Cache-Control: post-check=0, pre-check=0", false);
header("Pragma: no-cache");
date_default_timezone_set('Europe/Lisbon');

function db_connect() {
    $host = "leimprojeto.mysql.database.azure.com";
    $username = "GabrielRodrigues";
    $password = "";
    $dbname = "";

    // caminho para o certificado da CA
    $ssl_ca = 'certs/BaltimoreCyberTrustRoot.crt.pem';

    // estabelecendo a conexão  
    $conn = new mysqli($host, $username, $password, $dbname);

    // configura a conexão para utilizar SSL com o certificado CA fornecido
    if (!$conn->ssl_set(NULL, NULL, $ssl_ca, NULL, NULL)) {
        die("Erro: Não foi possível configurar os parâmetros SSL.");
    }

    // verificando a conexão
    if ($conn->connect_error) {
        die("Erro: " . $conn->connect_error);
    }

    return $conn;
}

?>