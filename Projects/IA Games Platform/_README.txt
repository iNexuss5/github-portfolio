# Proposta de Projeto 20 - IA Games Platform

Este projeto trata da implementação de uma plataforma de jogos onde os utilizadores podem competir
tanto contra outros jogadores humanos como contra sistemas de inteligência artificial. 

# Como testar? 

O Projeto foi colocado em produção através dos serviços da Microsoft Azure, nomeadamente, WebApp e Servidor Flexível MySql. 
Foram importados os ficheiros .php (contendo o UnityWebGL para vizualização do jogo) e a base de dados via FTP. Desta forma, qualquer utilizador pode testar 
sem necessidade de instalar o Unity.

Encontra-se disponível em: https://notenoughgames-e8dke0edddhkckfc.spaincentral-01.azurewebsites.net.(30-07-2025) Pode ser necessário fornecer um certificado 
(certificado SSL automático exige domínio privado -> pago). Por motivos de segurança, o nome da base de dados e respetiva password encontram-se vazios no db.php.

AVISO: Existem problemas de cache com a aplicação, podendo algumas informações não serem devidamente atualizadas. Para corrigir, sugere-se:

- Recarregue a página com Ctrl+F5;
- Desative o Cache do site;

Além disso, não foi implementado um sistema de tratamento de erros para jogos inacabados. Para tal, pede-se que acabe todos os jogos para evitar
mau funcionamento.


# Como recriar o ambiente?

1- Caso queira recriar o ambiente de desenvolvimento do projeto, instale o Unity 2022.3.20f1. -> unityhub://2022.3.20f1/61c2feb0970d;
2- Crie um 2D project;
3- Import Package > Custom Package e selecione o ficheiro presente em \03_Implementacao\Export;


# Organização

O diretório de \03_Implementacao\Codigo está dividido em 3 categorias:

- Código C# desenvolvido para o unity (\Jogos), que por sua vez está organizado com base nas funções de cada script;
- Código PHP que gere a interface gráfica (HTML + CSS + JS), conexão com Unity e com a base de dados (\PHPs)
- Export .sql da base de dados (\DB)
