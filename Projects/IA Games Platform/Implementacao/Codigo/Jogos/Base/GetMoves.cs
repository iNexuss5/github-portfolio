using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Collections;

/*
 Classe responsável por carregar e reproduzir uma sequência de jogadas
 a partir dum histórico armazenado na base de dados.
 */
public class MoveLoader : MonoBehaviour
{
    public TurnBasedGame currentGame;          // Referência ao jogo atual
    private int currentIndex = 0;              // Índice do movimento atual em reprodução
    private MoveList moveList;                 // Lista de movimentos recebida via JSON

    private List<string[]> boardHistory = new List<string[]>();  // Histórico de estados do tabuleiro

    void Start()
    {
        string matchId = PlayerPrefs.GetString("matchId", "");  // Obtém o ID da partida guardado

        if (!string.IsNullOrEmpty(matchId))
        {
            StartCoroutine(LoadMoves(matchId));  // Inicia carregamento dos movimentos se houver matchId
        }
        else
        {
            currentGame.isReviewing = false;  // Desativa modo de revisão
            Debug.Log("Nenhum matchId encontrado. Certifique-se de que o jogo foi iniciado corretamente.");
        }
    }

    /*
     Corrotina que envia uma requisição ao servidor para obter os movimentos
     correspondentes a um determinado matchId.
    */
    public IEnumerator LoadMoves(string matchId)
    {
        currentGame.isReviewing = true;  // Ativa modo de visualização/replay

        // Monta a URL com timestamp para evitar cache
        string url = SendToDatabase.defaultUrl + "get_moves.php?matchId=" + matchId + "&ts=" + System.DateTime.Now.Ticks;

        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();  // Aguarda a resposta do servidor

        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = request.downloadHandler.text;

            moveList = JsonUtility.FromJson<MoveList>(json);  // Desserializa a lista de movimentos

            // Verifica se movimentos foram corretamente carregados
            if (moveList != null && moveList.moves != null)
            {
                for (int i = 0; i < moveList.moves.Length; i++)
                {
                    MoveGet move = moveList.moves[i];

                    // Aplica a jogada ao estado atual do jogo
                    currentGame.ApplyMove(move.origin, move.destiny, move.turn);

                    // Guarda o estado do tabuleiro após a jogada
                    boardHistory.Add((string[])currentGame.localBoardState.Clone());
                }
            }
            else
            {
                Debug.LogError("Erro ao deserializar ou lista de movimentos vazia.");
            }
        }
        else
        {
            Debug.LogError("Erro ao buscar movimentos: " + request.error);
        }

        SetTurn(0);  // Inicializa o jogo no primeiro estado
    }

    /*
     Avança para o movimento seguinte, se disponível.
     */
    public void NextMove()
    {
        if (moveList != null && currentIndex < moveList.moves.Length - 1)
        {
            currentIndex++;
            SetTurn(currentIndex);  // Atualiza o tabuleiro com o próximo estado
        }
        else
        {
            Debug.Log("Não há mais movimentos para exibir.");
        }
    }

    /*
     Define o estado do jogo para o movimento correspondente ao índice fornecido.
     */
    private void SetTurn(int turn)
    {
        // Obtém uma cópia do estado guardado nesse ponto
        string[] state = (string[])boardHistory[turn].Clone();

        // Substitui o estado atual do jogo
        for (int i = 0; i < currentGame.localBoardState.Length; i++)
        {
            currentGame.localBoardState[i] = state[i];
        }

        currentGame.UpdateBoardDisplay();  // Atualiza visualmente o tabuleiro
    }

    /*
     Regride para o movimento anterior, se disponível.
     */
    public void PreviousMove()
    {
        if (moveList != null && currentIndex > 0)
        {
            currentIndex--;
            SetTurn(currentIndex);  // Atualiza o tabuleiro com o estado anterior
        }
        else
        {
            Debug.Log("Não há movimentos anteriores para exibir.");
        }
    }
}

/*
 Representa a lista de movimentos recebida em formato JSON.
 */
[System.Serializable]
public class MoveList
{
    public MoveGet[] moves;  // Array de movimentos
}

/*
 Representa um movimento individual. Igual a classe Move, mas apenas com os dados que o servidor envia.
 */
[System.Serializable]
public class MoveGet
{
    public string turn;     // Turno do jogador que fez a jogada
    public string origin;   // Posição de origem da peça
    public string destiny;  // Posição de destino da peça
    public string piece;    // Tipo de peça 
}