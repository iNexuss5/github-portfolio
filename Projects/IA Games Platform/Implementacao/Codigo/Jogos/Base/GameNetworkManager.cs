using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Netcode;
using TMPro;
using System.Threading.Tasks;
using System.Collections.Generic;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Networking.Transport.Relay;
using System;
using Unity.Netcode.Transports.UTP;

public class GameNetworkManager : MonoBehaviour
{
    // Declaração de variáveis públicas para feedback visual e para receber o código do lobby.
    public TMP_InputField feedbackText;
    public TMP_InputField lobbyCodeInputField;
    
    // Variáveis privadas para armazenar o lobby do host e o intervalo de "heartbeat".
    private Lobby hostLobby;
    private float heartbeatInterval = 15f;
    public TurnBasedGame currentGame;

    private async void Start()
    {
        try
        {
            // Inicializa o Unity Services (essencial para usar os serviços da Unity como Lobby e Relay).
            await UnityServices.InitializeAsync();

            // Realiza o login anonimo do jogador.
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            // Feedback positivo após a autenticação.
            feedbackText.text = "Authenticated!";
        }
        catch (Exception e)
        {
            // Caso ocorra algum erro durante a autenticação, exibe a mensagem de erro no feedback.
            feedbackText.text = "Error: " + e.Message;
        }
    }

    // Método Update, que é chamado a cada frame para monitorizar o "heartbeat" do lobby.
    private void Update()
    {
        HandleLobbyHeartbeat();
    }

    // Método assíncrono que envia o "heartbeat" para o lobby para manter a conexão ativa.
    private async void HandleLobbyHeartbeat()
    {
        if (hostLobby != null)
        {
            // Decrementa o intervalo de tempo a cada quadro.
            heartbeatInterval -= Time.deltaTime;

            // Quando o intervalo atingir 0, envia o "ping" e reinicia o contador.
            if (heartbeatInterval <= 0)
            {
                heartbeatInterval = 15f;

                try
                {
                    // Envia o "ping" para o lobby, para mantê-lo ativo e impedir desconexões.
                    await Lobbies.Instance.SendHeartbeatPingAsync(hostLobby.Id);
                }
                catch (Exception e)
                {
                    // Caso ocorra um erro no envio do "ping", loga a falha.
                    Debug.LogError("Heartbeat failed: " + e.Message);
                }
            }
        }
    }

    // Método assíncrono que configura a alocação do Relay para a partida.
    private async Task<string> SetupRelay()
    {
        try
        {
            // Cria uma alocação de servidor para 1 jogador, o que é suficiente para o host.
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(1);

            // Obtém o código para outros jogadores se juntarem ao jogo.
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            // Cria um objeto com os dados do servidor Relay.
            var relayData = new RelayServerData(allocation, "wss");

            // Configura o Unity Transport para usar os dados do servidor Relay.
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayData);

            return joinCode; // Retorna o código de entrada do Relay para o lobby.
        }
        catch (Exception e)
        {
            // Caso ocorra um erro ao configurar o Relay, exibe uma mensagem de erro.
            feedbackText.text = "Error Relay: " + e.Message;
            return null; // Retorna null para indicar falha.
        }
    }

    // Método que cria o lobby do jogo e configura a rede para que outros jogadores possam se conectar.
    public async void CreateLobby()
    {
        try
        {
            // Se o servidor já estiver a rodar, reinicia o jogo.
            if (NetworkManager.Singleton.IsListening)
            {
                currentGame.ResetGame(); // Reseta o jogo
                NetworkManager.Singleton.Shutdown(); // Desliga o servidor atual
            }

            feedbackText.text = "Creating lobby..."; // Exibe uma mensagem informando que o lobby está a ser criado.
            
            // Configura a alocação do Relay.
            string joinCode = await SetupRelay();

            // Se não conseguiu obter o código de entrada do Relay, exibe erro.
            if (string.IsNullOrEmpty(joinCode))
            {
                feedbackText.text = "Failed to create relay allocation.";
                return;
            }

            // Define opções para a criação do lobby, incluindo o código do Relay para permitir que outros jogadores se conectem.
            var options = new CreateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { "RelayCode", new DataObject(DataObject.VisibilityOptions.Public, joinCode) }
                }
            };

            // Cria o lobby.
            hostLobby = await LobbyService.Instance.CreateLobbyAsync(currentGame.gameTitle.ToString(), 2, options);
            feedbackText.text = $"Lobby created!\nCode: {hostLobby.LobbyCode}"; // Exibe o código do lobby para os jogadores.

            // Inicia o servidor do jogo como host para que os jogadores possam se conectar.
            NetworkManager.Singleton.StartHost();
        }
        catch (Exception e)
        {
            // Caso ocorra algum erro, exibe a mensagem de erro.
            feedbackText.text = "Error: " + e.Message;
        }
    }

    // Método para entrar num lobby existente usando um código fornecido.
    public async void JoinLobby()
    {
        try
        {
            // Se o servidor já estiver a rodar, reinicia o jogo.
            if (NetworkManager.Singleton.IsListening)
            {
                currentGame.ResetGame();
                NetworkManager.Singleton.Shutdown();
            }

            feedbackText.text = "Attempting to join lobby...";

            // Tenta juntar-se ao lobby usando o código fornecido.
            var lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCodeInputField.text.ToUpper());
            string relayCode = lobby.Data["RelayCode"].Value;

            // Verifica se o jogo em que o lobby foi criado corresponde ao título do jogo atual, caso contrário exibe erro.
            if (lobby.Name != currentGame.gameTitle.ToString())
            {
                feedbackText.text = "Wrong game! This lobby is for " + lobby.Name;
                return;
            }

            // Tenta se conectar ao servidor Relay usando o código de entrada.
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(relayCode);
            var relayData = new RelayServerData(allocation, "wss");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayData);

            feedbackText.text = "Joined lobby " + lobbyCodeInputField.text.ToUpper(); 

            // Inicia o cliente e conecta-se ao lobby.
            NetworkManager.Singleton.StartClient();
        }
        catch (Exception e)
        {
            // Caso ocorra algum erro, exibe a mensagem de erro.
            feedbackText.text = "Error: " + e.Message;
        }
    }
}
