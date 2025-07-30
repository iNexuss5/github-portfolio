<?php
ob_start(); // inicia o buffer de saída

// define cabeçalhos HTTP para desativar cache no browser
header("Expires: Tue, 01 Jan 2000 00:00:00 GMT"); 
header("Last-Modified: " . gmdate("D, d M Y H:i:s") . " GMT"); 
header("Cache-Control: no-store, no-cache, must-revalidate, max-age=0"); 
header("Cache-Control: post-check=0, pre-check=0", false);
header("Pragma: no-cache");

session_start(); // inicia a sessão
session_unset(); // limpa todas as variáveis da sessão
session_destroy(); // destrói a sessão

// remove o cookie da sessão, se estiver em uso
if (ini_get("session.use_cookies")) {
    $params = session_get_cookie_params(); // Obtém os parâmetros do cookie
    setcookie(session_name(), '', time() - 42000, // Expira o cookie
        $params["path"], $params["domain"],
        $params["secure"], $params["httponly"]
    );
}

// monta a URL para redirecionamento evitando cache (com timestamp único)
$host = $_SERVER['HTTP_HOST'];
$uri = rtrim(dirname($_SERVER['PHP_SELF']), '/\\');
$url = "https://$host$uri/index.php?logout=" . time();

header("Location: $url"); // redireciona para index.php com parâmetro de tempo
exit();

ob_end_flush(); // desbloqueia o buffer de saída
?>
