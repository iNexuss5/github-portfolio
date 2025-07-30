<?php
session_start(); // inicia a sessão 

require_once 'db.php'; // inclui o arquivo de conexão com a base de dados
$conn = db_connect(); // método de conexão à base de dados


if ($_SERVER["REQUEST_METHOD"] == "POST") { // verifica se o formulário de pedido de criação de conta foi enviado 
  // armazena os dados do formulário
  $username = $_POST["username"];
  $password = $_POST["password"];
  $email = $_POST["email"];
  $photo_path = null; 

  if (!empty($username) && !empty($password) && !empty($email)) {   // verifica se os campos obrigatórios estão preenchidos

      $stmt = $conn->prepare("SELECT * FROM player WHERE username = ?");  // verifica se já existe um utilizador com o mesmo nome de utilizador

      $stmt->bind_param("s", $username);
      $stmt->execute();
      $stmt->store_result();

      // se o nome de utilizador já estiver a ser utilizado, não permite a criação de conta
      if ($stmt->num_rows == 1) {
          $msg = "Username already exists. Please choose a different one.";
      } else {
          if (isset($_FILES['profile_photo']) && $_FILES['profile_photo']['error'] === 0) {  // se o utilizador enviou uma imagem de perfil válida
              $uploadDir = 'uploads/'; // pasta de destino de armazenamento das fotos de perfil
              if (!is_dir($uploadDir)) {
                  mkdir($uploadDir, 0755, true); // cria a pasta se não existir
              }

              $ext = pathinfo($_FILES['profile_photo']['name'], PATHINFO_EXTENSION); //pega extensão do arquivo
              $filename = uniqid('user_', true) . '.' . $ext; // cria um nome de arquivo único
              $targetFile = $uploadDir . $filename; // caminho completo para salvar

              // move o arquivo enviado para a pasta de destino
              if (move_uploaded_file($_FILES['profile_photo']['tmp_name'], $targetFile)) {
                  $photo_path = $targetFile; // salva o caminho para inserir na db
              }
          }

          $stmt = $conn->prepare("INSERT INTO player (username) VALUES (?)"); // insere o utilizador na tabela player

          $stmt->bind_param("s", $username);
          $stmt->execute();

          
          $hash = password_hash($password, PASSWORD_DEFAULT);//cria o hash da senha

          //insere o utilizador na tabela person com e-mail, senha e caminho da foto
          $sql = "INSERT INTO person (username, email, passwordHash, photoURL) VALUES (?, ?, ?, ?)";
          $stmt = $conn->prepare($sql);
          $stmt->bind_param("ssss", $username, $email, $hash, $photo_path);
          $stmt->execute();
          $userId = $stmt->insert_id; //armazena o ID do novo utilizador

          $stmt->close();

          // recupera os dados do utilizador criado para armazenar na sessão
          $stmt = $conn->prepare("SELECT * FROM person WHERE username = ?");
          $stmt->bind_param("s", $username);
          $stmt->execute();
          $result = $stmt->get_result();
          if ($result->num_rows == 1) {
              $user = $result->fetch_assoc();
              // armazena os dados na sessão
              $_SESSION["user"] = $user; 
              $_SESSION["user"]["username"] = $username;
              $_SESSION["user"]["userId"] = $userId;

              // redireciona o utilizador para a página principal
              header("Location: index.php");
              exit();
          }
      }
  } else {
      // caso algum campo obrigatório esteja vazio
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

  <!-- barra de navegação -->
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

  <!-- secção de registro -->
  <section class="login-section">
    <div class="login-container">
      <h2>Create Your Account</h2>
      <!-- formulário de criação de conta -->
      <form method="POST" enctype="multipart/form-data">
        <label for="username">Username</label>
        <input type="text" name="username" id="username" placeholder="Enter your username" required />
        
        <label for="email">Email</label>
        <input type="email" name="email" id="email" placeholder="Enter your email" required />
        
        <label for="password">Password</label>
        <input type="password" name="password" id="password" placeholder="Enter your password" required />
        
        <label for="profile_photo">Foto de Perfil:</label>
        <input type="file" name="profile_photo" accept="image/*" />

        <button type="submit" value="Sign Up">Sign Up</button>
      </form>

      <!-- link para login se o usuário já tiver conta -->
      <p>Already have an account? <a href="login.php">Login</a></p>

      <!-- mostra mensagens de erro ou sucesso -->
      <?php if(isset($msg)): ?>
        <p><?= $msg ?></p>
      <?php endif; ?>
    </div>
  </section>

</body>
</html>

