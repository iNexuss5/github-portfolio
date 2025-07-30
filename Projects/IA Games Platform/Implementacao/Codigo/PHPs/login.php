<?php
session_start(); // inicia a sessão

require_once 'db.php';  // inclui o arquivo de conexão com a base de dados
$conn = db_connect(); // método de conexão à base de dados

if ($_SERVER["REQUEST_METHOD"] == "POST") {  //  verifica se o formulário de loginfoi submetido via POST
  $username = $_POST["username"];
  $password = $_POST["password"]; 
  
  // valida se os campos username e password não estão vazios
  if (!empty($username) && !empty($password)) {
    // prepara a consulta para buscar o utilizador na db pelo username
    $stmt = $conn->prepare("SELECT * FROM person WHERE username = ?");
    $stmt->bind_param("s", $username); 
    $stmt->execute();  

    $result = $stmt->get_result();  
    if ($result->num_rows == 1) {  
      $user = $result->fetch_assoc();  // obtém os dados do utilizador

      // verifica se a senha digitada corresponde ao hash armazenado na base de dados
      if (password_verify($password, $user['passwordHash'])) {
        $msg = "Login successful!" . $user['id']; 
        $_SESSION["user"] = $user;  // armazena todos os dados do utilizador na sessão
        header("Location: index.php");  // redireciona para a página inicial
        exit();
      }
    }
    // caso username não exista ou senha esteja incorreta
    $msg = "Invalid username or password.";
  } else {
    // caso algum campo esteja vazio
    $msg = "Please fill in all fields.";
  }
}
?>

<!DOCTYPE html>
<html lang="en">

<head>
  <meta charset="UTF-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>Login Page</title>
  <link href="https://fonts.googleapis.com/css2?family=Rubik&display=swap" rel="stylesheet" />
  <link rel="stylesheet" href="css/main.css" />
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
      <a href="login.php" class="login-btn">Login</a>
    </div>

    <div class="menu-toggle" id="menuToggle">
      ☰
    </div>
  </nav>

  <!-- seção de login -->
  <section class="login-section">
    <div class="login-container">
      <h2>Login to Your Account</h2>
      <!-- formulário para enviar username e password -->
      <form method="POST">
        <label for="username">Username</label>
        <input type="text" name="username" id="username" placeholder="Enter your username" required />

        <label for="password">Password</label>
        <input type="password" name="password" id="password" placeholder="Enter your password" required />

        <button type="submit" value="Login">Login</button>
      </form>
      <p>Don't have an account? <a href="signup.php">Sign up</a></p>

      <!-- mostra mensagens de erro ou sucesso, caso existam -->
      <?php if (isset($msg)): ?>
        <p><?= $msg ?></p>
      <?php endif; ?>
    </div>
  </section>

</body>

</html>
