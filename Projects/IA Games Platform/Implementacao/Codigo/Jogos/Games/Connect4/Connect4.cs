using TMPro;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.VisualScripting;
using ColorUtility = UnityEngine.ColorUtility;

// Classe principal do jogo Connect4, derivada de TurnBasedGame 
public class Connect4 : TurnBasedGame
{
    // Estado local do tabuleiro (não sincronizado em rede)
    private string[] _localBoardState = new string[42];
    public override string[] localBoardState => _localBoardState;

    // Tempos que a IA levará para jogar conforme dificuldade
    protected override float[] AiTimes => new float[] {
        0f,
        0.004f,
        0.05f,
        0.75f
    };

    // Inicializa o tabuleiro 
    protected override void InitializeBoard()
    {
        boardState.Clear();
        for (int i = 0; i < 42; i++) boardState.Add(new FixedString32Bytes(""));
        gameEnded.Value = false;
    }

    // Pressionar uma coluna para realizar uma jogada
    public void PressColumn(int column)
    {
        bool isX = isXTurn.Value;
        int row = GetLowestEmptyRow(localBoardState, column);
        if (row == -1) return;  // Coluna cheia

        int index = row * 7 + column;
        MakeMove(index, isX);
    }

    // Retorna a linha mais baixa disponível numa coluna
    public override int GetLowestEmptyRow(string[] state, int column)
    {
        for (int row = 5; row >= 0; row--)
        {
            int index = row * 7 + column;
            if (string.IsNullOrEmpty(state[index]))
                return row;
        }
        return -1;
    }

    // Envia a jogada ao servidor para validação e atualização do estado de rede
    [ServerRpc(RequireOwnership = false)]
    private void PressMoveServerRpc(int row, int column, string value)
    {
        int index = row * 7 + column;
        if (!IsValidMove(index)) return;

        // Envia a jogada à base de dados 
        sendToDatabase.sendMove(isXTurn.Value ? 1 : 2, (column + 1).ToString(), (column + 1).ToString(), isXTurn.Value ? "Red" : "Yellow");

        boardState[index] = new FixedString32Bytes(value);  // Atualiza estado de rede
        isXTurn.Value = !isXTurn.Value;  // Alterna o turno
    }

    // Verifica se o índice de jogada é válido
    private bool IsValidMove(int index)
    {
        if (index < 0 || index >= 42) return false;

        if (!isAI)
        {
            if (!(boardState[index] == "")) return false;
        }
        else
        {
            if (!(localBoardState[index] == "")) return false;
        }

        int column = index % 7;
        return GetLowestEmptyRow(localBoardState, column) != -1;
    }

    // Verifica se o jogo terminou em empate
    protected override bool CheckForDraw(string[] boardState)
    {
        foreach (var cell in boardState)
        {
            if (string.IsNullOrEmpty(cell)) return false;
        }
        return true;
    }

    // Verifica se há um vencedor no tabuleiro
    protected override bool CheckForWinner(string[] state, out string winner)
    {
        winner = null;
        for (int row = 0; row < 6; row++)
        {
            for (int col = 0; col < 7; col++)
            {
                string current = GetCell(state, row, col);
                if (string.IsNullOrEmpty(current)) continue;

                // Verifica todas as direções possíveis (→ ↓ ↘ ↙)
                if (CheckDirection(state, row, col, 0, 1, current) ||
                    CheckDirection(state, row, col, 1, 0, current) ||
                    CheckDirection(state, row, col, 1, 1, current) ||
                    CheckDirection(state, row, col, 1, -1, current))
                {
                    winner = current;
                    return true;
                }
            }
        }
        return false;
    }

    // Verifica 4 em linha a partir de um ponto em determinada direção
    private bool CheckDirection(string[] state, int startRow, int startCol, int deltaRow, int deltaCol, string symbol)
    {
        for (int i = 1; i < 4; i++)
        {
            int r = startRow + deltaRow * i;
            int c = startCol + deltaCol * i;

            if (r < 0 || r >= 6 || c < 0 || c >= 7) return false;
            if (GetCell(state, r, c) != symbol) return false;
        }
        return true;
    }

    // Retorna o valor de uma célula a partir de linha e coluna
    private string GetCell(string[] state, int row, int col)
    {
        return state[row * 7 + col];
    }

    // Atualiza as cores dos botões na UI com base no estado do tabuleiro
    public override void UpdateBoardDisplay()
    {
        if (!isBoardReady.Value || buttons == null || buttons.Length != 42) return;

        for (int i = 0; i < 42; i++)
        {
            var btn = buttons[i];
            if (btn == null) continue;

            Image img = btn.GetComponent<Image>();
            if (img == null) continue;

            switch (localBoardState[i])
            {
                case "X":
                    img.color = Color.red;
                    break;
                case "O":
                    img.color = Color.yellow;
                    break;
                default:
                    Color newColor;
                    if (ColorUtility.TryParseHtmlString("#090037", out newColor))
                        img.color = newColor;
                    break;
            }
        }
    }

    // Limpa o estado local do tabuleiro
    protected override void ClearLocalBoard()
    {
        for (int i = 0; i < 42; i++) localBoardState[i] = "";
    }

    // Sincroniza o estado local com o estado de rede
    protected override void SyncBoardState()
    {
        for (int i = 0; i < 42; i++)
            localBoardState[i] = boardState[i].ToString();
    }

    // Quando o botão do tabuleiro é pressionado
    public override void PressButton(int buttonIndex)
    {
        int column = buttonIndex % 7;
        if (!CanPressButton(column)) return;
        PressColumn(column);
    }

    // Verifica se um botão pode ser pressionado
    public override bool CanPressButton(int column)
    {
        return isBoardReady.Value &&
               !gameEnded.Value &&
               GetLowestEmptyRow(localBoardState, column) != -1;
    }

    // Executa a jogada, atualiza UI e envia dados
    public override void MakeMove(int index, bool isX)
    {
        if (!isReviewing)
        {
            if (!IsValidMove(index)) return;
        }

        string piece = isX ? "X" : "O";
        int row = index / 7;
        int column = isReviewing ? index : index % 7;

        ApplyMove(localBoardState, column.ToString());
        if (isReviewing) return;

        UpdateButtonInteractivity(true);

        if (isAI)
        {
            sendToDatabase.sendMove(isXTurn.Value ? 1 : 2, (column + 1).ToString(), (column + 1).ToString(), isXTurn.Value ? "Red" : "Yellow");
            // Pequena mudança na lógica de envio (Red, Yellow) para testes na DB
            if (!gameEnded.Value && CheckGameState()) return;

            isXTurn.Value = !isXTurn.Value;
            MakeAIMove(column.ToString());
            if (CheckGameState()) return;
        }
        else
        {
            PressMoveServerRpc(row, column, piece);
        }

        UpdateBoardDisplay();
        UpdateButtonInteractivity();
    }

    // Evento de mudança do tabuleiro de rede
    protected override void OnBoardChanged(NetworkListEvent<FixedString32Bytes> changeEvent)
    {
        if (!isBoardReady.Value || boardState.Count != 42) return;
        base.OnBoardChanged(changeEvent);
    }

    // Imprime o estado do tabuleiro na consola
    public override void PrintBoardState(string[] localBoardState)
    {
        Debug.Log("Board State:");

        for (int row = 0; row < 6; row++)
        {
            string rowState = "";

            for (int col = 0; col < 7; col++)
            {
                string cell = localBoardState[row * 7 + col];
                rowState += string.IsNullOrEmpty(cell) ? "." : cell;
                rowState += " ";
            }

            Debug.Log(rowState);
        }
    }

    // Verifica se o jogo acabou por vitória ou empate
    public override bool GameOver(string[] state)
    {
        return CheckForWinner(state, out string _) || CheckForDraw(state);
    }

    // Retorna todas as jogadas válidas (colunas disponíveis)
    public override List<string> GetLegalMoves(string[] state)
    {
        List<string> legalMoves = new();

        for (int col = 0; col < 7; col++)
        {
            if (GetLowestEmptyRow(state, col) != -1)
                legalMoves.Add(col.ToString());
        }

        return legalMoves;
    }

    // Determina o resultado do jogo: 1 (X), -1 (O), 0 (empate ou jogo não finalizado)
    public override int GetOutcome(string[] state)
    {
        if (CheckForDraw(state)) return 0;

        if (CheckForWinner(state, out string winner))
        {
            if (winner == "X") return 1;
            else if (winner == "O") return -1;
        }

        return 0;
    }

    // Executa a jogada da IA usando MCTS
    protected override void MakeAIMove(string move)
    {
        mcts.Move(move); // Atualiza o estado da árvore MCTS
        mcts.Search(AiTimes[int.Parse(ai_dif)]); // Executa a pesquisa MCTS com o tempo definido pela dificuldade
        var (num_rollouts, runtime) = mcts.Statistics(); // Obtém estatísticas da pesquisa MCTS
        var bestMove = mcts.BestMove(); // Obtém o melhor movimento da árvore MCTS
        ApplyMove(localBoardState, bestMove); // Aplica o melhor movimento ao estado local

        Debug.Log($"AI made move: {bestMove}, Rollouts: {num_rollouts}, Runtime: {runtime} seconds");

        int bestMoveIndex = int.Parse(bestMove);
        UpdateBoardDisplay();

        sendToDatabase.sendMove(isXTurn.Value ? 1 : 2, (bestMoveIndex + 1).ToString(), (bestMoveIndex + 1).ToString(), isXTurn.Value ? "Red" : "Yellow");
        isXTurn.Value = !isXTurn.Value;
    //Envia a jogada da IA ao servidor
        mcts.Move(bestMove); // Atualiza o estado da árvore MCTS
    }

    // Conta jogadas feitas para determinar de quem é a vez
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

    // Aplica uma jogada ao estado local
    public override void ApplyMove(string[] state, string move)
    {
        int moveInt = int.Parse(move);
        int row = GetLowestEmptyRow(state, moveInt);
        int index = row * 7 + moveInt;

        Debug.Log("Valor de state[" + index + "]: " + (state[index] == null ? "null" : $"'{state[index]}'") + " | Comparando com '': " + (state[index] == ""));

        if (state[index] == "")
        {
            if (GetTurn(state) == -1)
                state[index] = "X";
            else
                state[index] = "O";
        }
        else
        {
            Debug.LogWarning($"Invalid move: {move} at index {index} is already occupied.");
        }

        UpdateBoardDisplay();
        PrintBoardState(state);
    }
}
