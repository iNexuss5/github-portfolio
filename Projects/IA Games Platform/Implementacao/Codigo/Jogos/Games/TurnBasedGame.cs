using TMPro;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System.Collections.Generic;

// Classe geral para jogos por turnos que implementa funcionalidades de rede
public abstract class TurnBasedGame : NetworkBehaviour
{
    // Variáveis de controo do estado do jogo
    public bool isAI = false;  // Determina se o jogo será contra IA
    public bool isReviewing = false;  // Determina se o jogador está a rever uma partida
    public GridGenerator gridGenerator;  // Referência para o gerador de grid (tabuleiro)
    protected NetworkVariable<bool> isXTurn = new NetworkVariable<bool>(true);  // Indica se é a vez do jogador X
    public bool IsXTurn() => isXTurn.Value;
    protected NetworkList<FixedString32Bytes> boardState = new NetworkList<FixedString32Bytes>();  // Estado do tabuleiro na rede
    public NetworkVariable<bool> gameEnded = new NetworkVariable<bool>(false);  // Indica se o jogo acabou
    protected NetworkVariable<bool> isBoardReady = new NetworkVariable<bool>(true);  // Indica se o tabuleiro está pronto
    protected NetworkVariable<bool> isLobbyFull = new NetworkVariable<bool>(false);  // Indica se a sala está cheia
    public SendToDatabase sendToDatabase;  // Referência para enviar dados para a base de dados
    protected MCTS mcts;  // Instância do algoritmo de Monte Carlo Tree Search para a IA
    public abstract string[] localBoardState { get; }  // Estado do tabuleiro local

    // Variáveis de estado do jogo
    [Header("Game State")]
    protected NetworkVariable<int> playerXScore = new NetworkVariable<int>(0);  // Placar do jogador X
    protected NetworkVariable<int> playerOScore = new NetworkVariable<int>(0);  // Placar do jogador O

    // Título do jogo e número máximo de jogadores
    [Header("Database Fields")]
    public GameTitle gameTitle;
    public int maxUsers = 2;

    // Referências para a interface de utilizador 
    [Header("UI References")]
    public TMP_InputField msgFeedback;  // Caixa de mensagem de feedback
    public TextMeshProUGUI playerXScoreText;  // Texto para exibir a pontuação do jogador X
    public TextMeshProUGUI playerOScoreText;  // Texto para exibir a pontuação do jogador O
    protected UnityEngine.UI.Button[] buttons;  // Botões do jogo
    public GameObject newGameBtn;  // Botão para iniciar um novo jogo
    public GameObject hostBtn;  // Botão para o host
    public GameObject clientBtn;  // Botão para o cliente
    public GameObject nextBtn;  // Botão para avançar
    public GameObject prevBtn;  // Botão para retroceder

    protected TextMeshProUGUI[] btnTexts;  // Textos associados aos botões

    // Diferenças da IA (dificuldade e outros aspectos)
    protected abstract float[] AiTimes { get; }  // Tempos de jogo da IA
    protected abstract void InitializeBoard();  // Inicializa o tabuleiro
    protected abstract bool CheckForWinner(string[] state, out string winner);  // Verifica se há um vencedor

    // Verifica se o jogo terminou empatado
    protected virtual bool CheckForDraw(string[] state)
    {
        foreach (var cell in state) if (string.IsNullOrEmpty(cell)) return false;  // Se houver uma célula vazia, não terminou
        return true;  // Se não houver células vazias, é empate
    }

    // Obtém a dificuldade da IA a partir das preferências do jogador, guardadas localmente
    public void GetAiDif()
    {
        ai_dif = PlayerPrefs.GetString("ai_difficulty", "0");  // Lê a dificuldade da IA
        isAI = ai_dif != "0";  // Define se é um jogo contra a IA ou não
        prevBtn.SetActive(isReviewing);  // Mostra ou esconde os botões de revisão
        nextBtn.SetActive(isReviewing);

        // Mostra ou esconde os botões de host/client dependendo da dificuldade ou se está a rever
        bool showHostClient = !(isAI || isReviewing);
        if (hostBtn != null) hostBtn.SetActive(showHostClient);
        if (clientBtn != null) clientBtn.SetActive(showHostClient);
    }

    // Verifica se o estado do jogo mudou (vencedor ou empate)
    protected bool CheckGameState()
    {
        if (CheckForWinner(localBoardState, out string winner))  // Se houver um vencedor
        {
            EndGame(winner);  // Finaliza o jogo
            return true;
        }

        if (CheckForDraw(localBoardState))  // Se for empate
        {
            EndGame(null);  // Finaliza o jogo
            return true;
        }
        return false;  // O jogo ainda não terminou
    }

    // Atualiza a interface do tabuleiro 
    public abstract void UpdateBoardDisplay();

    // Inicializa os componentes do jogo
    public virtual void InitializeGameComponents()
    {
        GameObject[] buttonObjects = GameObject.FindGameObjectsWithTag("Button");  // Encontra todos os botões no jogo
        buttons = new UnityEngine.UI.Button[buttonObjects.Length];
        btnTexts = new TextMeshProUGUI[buttonObjects.Length];

        // Inicializa os botões e textos associados
        for (int i = 0; i < buttonObjects.Length; i++)
        {
            buttons[i] = buttonObjects[i].GetComponent<UnityEngine.UI.Button>();
            btnTexts[i] = buttonObjects[i].GetComponentInChildren<TextMeshProUGUI>();
        }
    }

    // Método chamado quando o objeto é instanciado na rede
    public override void OnNetworkSpawn()
    {
        if (IsServer) InitializeServer();  // Se for o servidor, inicializa o servidor
        InitializeClient();  // Inicializa o cliente (em p2p, ambos são clientes)
    }

    // Inicializa a parte do servidor
    protected virtual void InitializeServer()
    {
        InitializeBoard();  // Inicializa o tabuleiro
        isBoardReady.Value = true;  // Marca o tabuleiro como pronto
        NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnection;  // Callback quando um cliente se conecta
    }

    // Inicializa a parte do cliente
    protected virtual void InitializeClient()
    {
        InitializeGameComponents();  // Inicializa os componentes do jogo
        SubscribeToNetworkEvents();  // Inscreve-se para eventos de rede
        OnBoardReadyChanged(false, isBoardReady.Value);  // Configura a UI do jogo
    }

    // Atualiza a interatividade dos botões
    protected void UpdateButtonInteractivity(bool force = false)
    {
        bool enableButtons;
        if (force) enableButtons = false; // Força a desativação dos botões
        else enableButtons = IsLocalPlayersTurn() && isLobbyFull.Value && !gameEnded.Value;  // Só permite interação se for a vez do jogador

        // Atualiza a interatividade de cada botão
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
                buttons[i].interactable = enableButtons && string.IsNullOrEmpty(localBoardState[i]);
        }

        // Atualiza o status do jogo
        if (!isAI) UpdateGameStatusMessage(enableButtons);
    }

    // Atualiza a pontuação com base no vencedor
    private void UpdateScore(string winner)
    {
        if (winner == "X") playerXScore.Value++;  // Se o vencedor for X
        else playerOScore.Value++;  // Se o vencedor for O
    }

    // Finaliza o jogo e atualiza os componentes da UI
    protected virtual void EndGame(string winner)
    {
        UpdateButtonInteractivity(true);  // Garante que os botões fiquem desativados

        if (IsServer || isAI)  // Se for o servidor ou se for um jogo contra a IA
        {
            if (winner != null)  // Se houver vencedor
            {
                UpdateScore(winner);  // Atualiza a pontuação
                sendToDatabase.sendWinner(winner == "X" ? "1" : "2", gameTitle.ToString());  // Envia o vencedor a base de dados
            }
            else
            {
                sendToDatabase.sendWinner("draw", gameTitle.ToString());  // Envia o empate
            }

            gameEnded.Value = true;  // Marca o jogo como terminado
            if (newGameBtn != null)
                newGameBtn.SetActive(true);  // Ativa o botão de novo jogo
        }
        msgFeedback.text = "Game Over!";  // Exibe mensagem de fim de jogo
        UpdateButtonInteractivity(true);  // Atualiza a interatividade dos botões
        UpdateAllUI();  // Atualiza toda a interface do jogo
    }

    // Método chamado quando o estado do tabuleiro é alterado
    protected virtual void OnBoardChanged(NetworkListEvent<FixedString32Bytes> _)
    {
        Debug.Log("Board state changed, updating UI...");
        SyncBoardState();  // Sincroniza o estado do tabuleiro
        UpdateBoardDisplay();  // Atualiza a interface gráfica do tabuleiro

        if (!gameEnded.Value)  // Se o jogo não acabou
        {
            CheckGameState();  // Verifica se o jogo terminou
            UpdateButtonInteractivity();  // Atualiza os botões
        }
    }

    // Imprime o estado do tabuleiro na console
    public abstract void PrintBoardState(string[] state);

    // Atualiza a mensagem de status do jogo
    protected void UpdateGameStatusMessage(bool enableButtons)
    {
        if (msgFeedback == null) return;

        msgFeedback.text = !isLobbyFull.Value ? "Waiting for another player..." :
                          gameEnded.Value ? "Game over!" :
                          enableButtons ? "Your turn!" : "Waiting for opponent...";
    }

    protected virtual void OnTurnChanged(bool _, bool __) => UpdateButtonInteractivity();  // Chama quando a vez do jogador muda
    protected void OnScoreChanged(int _, int __) => UpdateScoreDisplays();  // Atualiza os displays de pontuação

    // Inscreve-se para eventos da rede
    protected virtual void SubscribeToNetworkEvents()
    {
        gameEnded.OnValueChanged += OnGameEndedChanged;  // Quando o jogo terminar
        isLobbyFull.OnValueChanged += OnLobbyFullChanged;  // Quando a sala estiver cheia
        isBoardReady.OnValueChanged += OnBoardReadyChanged;  // Quando o tabuleiro estiver pronto
        isXTurn.OnValueChanged += OnTurnChanged;  // Quando a vez do jogador mudar
        playerXScore.OnValueChanged += OnScoreChanged;  // Quando a pontuação do jogador X mudar
        playerOScore.OnValueChanged += OnScoreChanged;  // Quando a pontuação do jogador O mudar
    }

    // Quando a sala fica cheia
    protected virtual void OnLobbyFullChanged(bool _, bool isFull)
    {
        if (!isFull) return;

        if (IsServer) isXTurn.Value = true;  // Se for o servidor, inicia o jogo com o jogador X
        sendToDatabase.sendMatch(gameTitle.ToString(), maxUsers, ai_dif);  // Envia informações do jogo a base de dados
        hostBtn.SetActive(false);  // Desativa o botão de host
        clientBtn.SetActive(false);  // Desativa o botão de cliente
    }

    // Quando o estado do jogo mudar
    protected virtual void OnGameEndedChanged(bool _, bool newValue)
    {
        if (newValue)
        {
            UpdateButtonInteractivity(true);  // Desativa os botões quando o jogo terminar
        }
    }

    // Quando o tabuleiro estiver pronto para jogar
    protected virtual void OnBoardReadyChanged(bool _, bool isReady)
    {
        if (isReady) SetupGame();  // Se estiver pronto, configura o jogo
        else boardState.OnListChanged -= OnBoardChanged;  // Se não estiver, remove o ouvinte do evento de mudança no tabuleiro
    }

    // Configura o jogo quando o tabuleiro estiver pronto
    protected void SetupGame()
    {
        SetupBoardListeners();  // Configura os listeners de mudanças no tabuleiro
        UpdateAllUI();  // Atualiza toda a interface de utilizador
    }

    // Configura os listeners de mudança no tabuleiro
    protected void SetupBoardListeners()
    {
        boardState.OnListChanged -= OnBoardChanged;  // Remove o ouvinte anterior
        boardState.OnListChanged += OnBoardChanged;  // Adiciona o novo ouvinte
    }

    // Atualiza a exibição da pontuação na interface
    protected void UpdateScoreDisplays()
    {
        if (playerXScoreText != null) playerXScoreText.text = $"{playerXScore.Value}";  // Atualiza a pontuação de X
        if (playerOScoreText != null) playerOScoreText.text = $"{playerOScore.Value}";  // Atualiza a pontuação de O
    }

    // Atualiza toda a interface do jogo
    protected void UpdateAllUI()
    {
        UpdateBoardDisplay();  // Atualiza o tabuleiro
        UpdateScoreDisplays();  // Atualiza a pontuação
    }

    // Verifica se um jogador pode pressionar o botão (jogar)
    public abstract bool CanPressButton(int buttonNumber);

    #region Reset & Cleanup

    // Chama quando o objeto é removido da rede, remove os listeners de eventos
    public override void OnNetworkDespawn()
    {
        isBoardReady.OnValueChanged -= OnBoardReadyChanged;
        isXTurn.OnValueChanged -= OnTurnChanged;
        playerXScore.OnValueChanged -= OnScoreChanged;
        playerOScore.OnValueChanged -= OnScoreChanged;
        boardState.OnListChanged -= OnBoardChanged;
        gameEnded.OnValueChanged -= OnGameEndedChanged;

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnection;

        isLobbyFull.OnValueChanged -= OnLobbyFullChanged;
    }

    // Método de início do jogo
    void Start()
    {
        GetAiDif();  // Obtém a dificuldade da IA
        gridGenerator.GenerateGrid();  // Gera o tabuleiro da UI
        InitializeGameComponents();  // Inicializa os componentes do jogo
        ClearLocalBoard();  // Limpa o tabuleiro local

        if (isReviewing)
        {
            isAI = false;
            UpdateButtonInteractivity(true);  // Não pemite interagir durante a revisão
        }

        if (isAI)
        {
            ClearLocalBoard();  // Limpa o tabuleiro
            mcts = new MCTS(this);  // Cria a instância da IA
            sendToDatabase.sendMatch(gameTitle.ToString(), maxUsers, ai_dif);  // Envia dados para a base de dados
        }

        newGameBtn.SetActive(false);  // Desativa o botão de novo jogo inicialmente
    }

    // Reinicia o jogo
    public void ResetGame()
    {
        if (!isAI)
        {
            ResetBoardServerRpc();  // Se não for IA, reinicia o tabuleiro no servidor
        }
        else
        {
            ClearLocalBoard();  // Limpa o tabuleiro
            UpdateBoardDisplay();  // Atualiza o tabuleiro UI
            mcts = new MCTS(this);  // Reinicializa a IA
            gameEnded.Value = false;  // Marca que o jogo não acabou
            newGameBtn.SetActive(false);  // Desativa o botão de novo jogo
        }
    }

    // Reinicia e envia dados a base de dados
    public void ResetAndSend()
    {
        ResetGame();
        sendToDatabase.sendMatch(gameTitle.ToString(), maxUsers, ai_dif);  // Envia os dados do jogo
        UpdateButtonInteractivity();  // Atualiza os botões
    }

    // Reinicia o tabuleiro no servidor
    [ServerRpc(RequireOwnership = false)]
    protected void ResetBoardServerRpc()
    {
        InitializeBoard();  // Inicializa o tabuleiro
        isXTurn.Value = true;  // Inicia com a vez do jogador X
        isBoardReady.Value = true;  // Marca o tabuleiro como pronto
        ClearLocalBoard();  // Limpa o tabuleiro
        UpdateBoardDisplay();  // Atualiza a exibição do tabuleiro
        NotifyGameResetClientRpc();  // Notifica o cliente sobre o reset
        newGameBtn.SetActive(false);  // Desativa o botão de novo jogo
    }

    // Notifica o cliente sobre o reset do jogo
    [ClientRpc]
    protected void NotifyGameResetClientRpc()
    {
        ClearLocalBoard();  // Limpa o tabuleiro local no cliente
        gameEnded.Value = false;  // Marca que o jogo não terminou
        msgFeedback.text = "";  // Limpa a mensagem de feedback
        UpdateAllUI();  // Atualiza a interface do utilizador
    }

    // Notifica o cliente que a sala está cheia e o jogo vai começar
    [ClientRpc]
    protected void NotifyLobbyFullClientRpc()
    {
        msgFeedback.text = "Game starting!";  // Exibe a mensagem de que o jogo vai começar
        UpdateButtonInteractivity();  // Atualiza os botões
    }

    #endregion

    #region Utility Methods

    // Verifica se é a vez do jogador local jogar
    public bool IsLocalPlayersTurn()
    {
        bool isX = isXTurn.Value;
        if (!isAI && !isReviewing)
        {
            // Verifica se o jogador local é o jogador X (primeiro cliente conectado)
            bool isPlayerX = NetworkManager.LocalClientId == NetworkManager.ConnectedClientsIds[0];
            // Retorna true se for a vez do jogador local (X ou O)
            return (isX && isPlayerX) || (!isX && !isPlayerX);
        }
        else
        {
            // Se for contra IA ou estiver a rever, assume que o jogador local é sempre X
            return isX;
        }
    }

    /*
    Métodos abstratos que devem ser implementados pelas subclasses
    */

    // Pressiona um botão do jogo
    public abstract void PressButton(int b);

    // Limpa o tabuleiro local
    protected abstract void ClearLocalBoard();

    // Sincroniza o estado do tabuleiro
    protected abstract void SyncBoardState();

    // Verifica se o jogo acabou
    public abstract bool GameOver(string[] state);

    // Obtém os movimentos legais no estado atual do tabuleiro
    public abstract List<string> GetLegalMoves(string[] state);

    // Obtém o resultado do jogo (vencedor, empate ou em andamento)
    public abstract int GetOutcome(string[] state);

    // Obtém a linha mais baixa disponível numa coluna (para jogos como Connect4)
    public virtual int GetLowestEmptyRow(string[] state, int column)
    {
        return -1;  // Implementação padrão para jogos que nao implementam esta lógica
    }

    // Obtém a vez do jogador no estado atual do tabuleiro
    public abstract int GetTurn(string[] state);

    // Aplica o movimento no estado do tabuleiro
    public abstract void ApplyMove(string[] state, string move);

    // Faz um movimento no jogo
    public abstract void MakeMove(int index, bool isX);

    // Aplica o movimento do IA
    protected abstract void MakeAIMove(string move);

    #endregion
}
