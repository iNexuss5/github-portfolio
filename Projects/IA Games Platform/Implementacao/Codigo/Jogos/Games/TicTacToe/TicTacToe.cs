using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System.Collections.Generic;

// Classe principal do jogo do galo, herdando de TurnBasedGame
public class TicTacToe : TurnBasedGame
{
    // Tempos diferentes para a IA pensar, dependendo da dificuldade
    protected override float[] AiTimes => new float[] { 0, .00075f, 0.002f, 0.1f };

    // Estado local do tabuleiro (apenas do lado do cliente)
    private string[] _localBoardState = new string[9];
    public override string[] localBoardState => _localBoardState;

    // Mostra o estado atual do tabuleiro na consola do Unity
    public override void PrintBoardState(string[] localBoardState)
    {
        Debug.Log("Estado atual do tabuleiro:");
        for (int i = 0; i < 3; i++)
        {
            string row = "";
            for (int j = 0; j < 3; j++)
            {
                row += localBoardState[i * 3 + j] + " ";
            }
            Debug.Log(row);
        }
    }

    #region Inicialização
    private void Awake()
    {
        // Só inicializa se não for IA (isto corre no lado do jogador)
        if (!isAI) InitializeGameComponents();
    }
    #endregion

    #region Preparação do Jogo
    protected override void InitializeBoard()
    {
        // Limpa o estado do tabuleiro
        boardState.Clear();
        for (int i = 0; i < 9; i++) boardState.Add(new FixedString32Bytes(""));
        gameEnded.Value = false;
    }
    #endregion

    #region Lógica do Jogo

    // Método chamado quando um botão é carregado (casa do tabuleiro)
    public override void PressButton(int buttonNumber)
    {
        if (!CanPressButton(buttonNumber)) return;

        bool isX = isXTurn.Value;
        bool isPlayerX;

        if (isAI)
        {
            isPlayerX = isX;
        }
        else
        {
            // Verifica se o jogador local é o primeiro da lista de clientes ligados
            isPlayerX = NetworkManager.LocalClientId == NetworkManager.ConnectedClientsIds[0];
        }

        // Verifica se é a vez certa
        if ((isX && !isPlayerX) || (!isX && isPlayerX)) return;

        MakeMove(buttonNumber, isX);
    }

    // Verifica se três posições têm o mesmo símbolo (linha vencedora)
    protected bool CheckLine(string a, string b, string c)
    {
        return !string.IsNullOrEmpty(a) && a == b && b == c;
    }

    public override void MakeMove(int buttonNumber, bool isX)
    {
        if (!isReviewing && !IsValidMove(buttonNumber)) return;

        // Aplica a jogada localmente
        ApplyMove(localBoardState, buttonNumber.ToString());

        UpdateBoardDisplay();
        if (isReviewing) return;

        UpdateButtonInteractivity(true);

        if (isAI)
        {
            // Envia a jogada do jogador para a base de dados
            sendToDatabase.sendMove(isXTurn.Value ? 1 : 2, (buttonNumber + 1).ToString(), (buttonNumber + 1).ToString(), isXTurn.Value ? "X" : "O");

            if (!gameEnded.Value && CheckGameState()) return;

            isXTurn.Value = !isXTurn.Value; // Alterna vez do jogador

            MakeAIMove(buttonNumber.ToString()); // IA faz a jogada usando MCTS
            if (CheckGameState()) return;
        }
        else
        {
            // Jogador humano: envia para o servidor
            PressButtonServerRpc(buttonNumber, isX ? "X" : "O");
        }

        UpdateBoardDisplay();
        UpdateButtonInteractivity();
    }

    [ServerRpc(RequireOwnership = false)]
    private void PressButtonServerRpc(int buttonNumber, string value)
    {
        if (!IsValidMove(buttonNumber)) return;

        // Envia para a base de dados (centraliza jogadas)
        sendToDatabase.sendMove(isXTurn.Value ? 1 : 2, (buttonNumber + 1).ToString(), (buttonNumber + 1).ToString(), localBoardState[buttonNumber]);

        boardState[buttonNumber] = new FixedString32Bytes(value);
        isXTurn.Value = !isXTurn.Value;
    }

    // Verifica se uma jogada é válida
    private bool IsValidMove(int buttonNumber)
    {
        if (isAI)
        {
            return isBoardReady.Value &&
                   !gameEnded.Value &&
                   buttonNumber >= 0 &&
                   buttonNumber < localBoardState.Length &&
                   string.IsNullOrEmpty(localBoardState[buttonNumber]);
        }

        return isBoardReady.Value &&
               !gameEnded.Value &&
               buttonNumber >= 0 &&
               buttonNumber < boardState.Count &&
               string.IsNullOrEmpty(boardState[buttonNumber].ToString());
    }

    #endregion

    #region Estado do Jogo

    // Verifica se existe um vencedor, percorre todas as linhas, colunas e diagonais
    protected override bool CheckForWinner(string[] localBoardState, out string winner)
    {
        winner = null;
        var board = new string[3, 3]
        {
            { localBoardState[0], localBoardState[1], localBoardState[2] },
            { localBoardState[3], localBoardState[4], localBoardState[5] },
            { localBoardState[6], localBoardState[7], localBoardState[8] }
        };

        for (int i = 0; i < 3; i++)
        {
            if (CheckLine(board[i, 0], board[i, 1], board[i, 2])) winner = board[i, 0];
            if (CheckLine(board[0, i], board[1, i], board[2, i])) winner = board[0, i];
        }

        if (winner == null)
        {
            if (CheckLine(board[0, 0], board[1, 1], board[2, 2])) winner = board[0, 0];
            if (CheckLine(board[0, 2], board[1, 1], board[2, 0])) winner = board[0, 2];
        }

        return winner != null;
    }

    #endregion

    #region Interface Gráfica

    public override void UpdateBoardDisplay()
    {
        if (!isBoardReady.Value || btnTexts == null || btnTexts.Length != 9) return;

        for (int i = 0; i < 9; i++)
        {
            if (btnTexts[i] != null)
                btnTexts[i].text = localBoardState[i];
        }
    }

    protected override void OnBoardChanged(NetworkListEvent<FixedString32Bytes> _)
    {
        if (!isBoardReady.Value || boardState.Count != 9) return;
        base.OnBoardChanged(_);
    }

    protected override void SyncBoardState()
    {
        for (int i = 0; i < 9; i++)
            localBoardState[i] = boardState[i].ToString();
    }

    protected override void ClearLocalBoard()
    {
        for (int i = 0; i < 9; i++) localBoardState[i] = "";
    }

    // Verifica se o botão pode ser pressionado (se está pronto, se a sala está cheia e se o jogo não acabou)
    public override bool CanPressButton(int buttonNumber)
    {
        if (isAI)
        {
            Debug.Log($"CanPressButton: isBoardReady={isBoardReady.Value}, isLobbyFull={isLobbyFull.Value}, gameEnded={gameEnded.Value}, buttonNumber={buttonNumber}, localBoardState.Length={localBoardState.Length}, localBoardState[buttonNumber]={localBoardState[buttonNumber]}");
            return isBoardReady.Value &&
                   !gameEnded.Value &&
                   buttonNumber >= 0 &&
                   buttonNumber < localBoardState.Length &&
                   string.IsNullOrEmpty(localBoardState[buttonNumber]);
        }
        else
        {
            return isBoardReady.Value &&
                   isLobbyFull.Value &&
                   !gameEnded.Value &&
                   buttonNumber >= 0 &&
                   buttonNumber < 9 &&
                   string.IsNullOrEmpty(localBoardState[buttonNumber]);
        }
    }

    // Verifica se o jogo acabou (vitória ou empate)
    public override bool GameOver(string[] state)
    {
        return CheckForWinner(state, out string _) || CheckForDraw(state);
    }

    // Retorna as jogadas possíveis (casas vazias)
    public override List<string> GetLegalMoves(string[] state)
    {
        List<string> legalMoves = new();
        for (int i = 0; i < state.Length; i++)
        {
            if (string.IsNullOrEmpty(state[i]))
            {
                legalMoves.Add(i.ToString());
            }
        }
        return legalMoves;
    }

    // Determina o resultado do jogo
    public override int GetOutcome(string[] state)
    {
        if (CheckForWinner(state, out string winner))
        {
            if (winner == "X") return 1;
            else if (winner == "O") return -1;
        }

        if (CheckForDraw(state)) return 0;

        return 0;
    }

    // Aplica uma jogada ao estado atual
    public override void ApplyMove(string[] state, string move)
    {
        int index = int.Parse(move);

        if (state[index] == "")
        {
            if (GetTurn(state) == -1)
                state[index] = "X";
            else
                state[index] = "O";
        }
        else
        {
            Debug.LogWarning($"Jogada inválida: {move} na posição {index} já está ocupada.");
        }

        UpdateBoardDisplay();
    }

    // IA faz jogada com algoritmo MCTS
    protected override void MakeAIMove(string move)
    {
        mcts.Move(move);
        mcts.Search(AiTimes[int.Parse(ai_dif)]);

        var (num_rollouts, runtime) = mcts.Statistics();
        var bestMove = mcts.BestMove();

        ApplyMove(localBoardState, bestMove);
        Debug.Log($"IA jogou: {bestMove}, Simulações: {num_rollouts}, Tempo: {runtime} segundos");

        int bestMoveIndex = int.Parse(bestMove);

        UpdateBoardDisplay();
        sendToDatabase.sendMove(isXTurn.Value ? 1 : 2, (bestMoveIndex + 1).ToString(), (bestMoveIndex + 1).ToString(), isXTurn.Value ? "X" : "O");

        isXTurn.Value = !isXTurn.Value;
        mcts.Move(bestMove);
    }

    // Determina de quem é a vez (X ou O)
    public override int GetTurn(string[] state)
    {
        int xCount = 0;
        int oCount = 0;

        foreach (var cell in state)
        {
            if (cell == "X") xCount++;
            else if (cell == "O") oCount++;
        }

        return xCount > oCount ? 1 : -1;
    }

    #endregion
}
