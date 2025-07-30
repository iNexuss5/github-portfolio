using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;

/*
 Classe responsável por enviar dados do jogo para um servidor remoto via HTTP POST.
 É usada para:
 - Criar partidas
 - Registar jogadas
 - Registar o vencedor
 Os dados são serializados em JSON e enviados para o `game.php`.
*/
public class SendToDatabase : MonoBehaviour
{
    // URL base do servidor (pode ser reutilizada por outras classes)
    public static string defaultUrl = "https://notenoughgames-e8dke0edddhkckfc.spaincentral-01.azurewebsites.net/";

    // URL específica para envio de dados do jogo
    private string serverUrl = defaultUrl + "game.php";

    /*
     Envia os dados de criação de uma partida para o servidor.
    */
    public void sendMatch(string Title, int maxUsers, string ai_dif)
    {
        StartCoroutine(SendJsonToServer(new MatchData
        {
            title = Title,
            max_users = maxUsers,
            ai_dif = ai_dif
        }));
    }

    /*
     Envia os dados do vencedor de uma partida para o servidor.
    */
    public void sendWinner(string winner, string title)
    {
        WinnerData winnerData = new()
        {
            winner = winner,
            title = title
        };

        Debug.Log("Sending winner");
        StartCoroutine(SendJsonToServer(winnerData));
    }

    /*
     Envia um movimento realizado no jogo para o servidor.
    */
    public void sendMove(int turn, string origin, string destiny, string piece)
    {
        MoveData move = new MoveData
        {
            turn = turn,
            origin = origin,
            destiny = destiny,
            piece = piece
            // matchId será preenchido no servidor ou por outra lógica
        };

        Debug.Log("Sending move");
        StartCoroutine(SendJsonToServer(move));
    }

    /*
     Corrotina que serializa os dados e envia-os ao servidor via HTTP POST com JSON.
    */
    private IEnumerator SendJsonToServer(object data)
    {
        string url = serverUrl;
        Debug.Log("URL: " + url);

        // Converte o objeto C# para JSON
        string jsonData = JsonUtility.ToJson(data);

        // Prepara o pedido HTTP
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // Envia o pedido e aguarda resposta
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Request successful: " + request.downloadHandler.text);
        }
        else
        {
            Debug.Log("Request error: " + request.error);
        }
    }
}

/*
 Estrutura de dados usada para enviar informações de uma nova partida.
*/
[System.Serializable]
public class MatchData
{
    public string type = "sendMatch";    // Indica o tipo de operação no backend
    public string ai_dif;                // Dificuldade da IA
    public string title;                // Título do jogo da partida
     public int max_users;               // Número máximo de jogadores
}

/*
 Estrutura de dados para envio de uma jogada ao servidor.
*/
[System.Serializable]
public class MoveData
{
    public string type = "sendMove";     // Tipo de operação
    public string matchId;               // ID da partida
    public int turn;                     // Turno em que a jogada foi feita
    public string origin;                // Casa de origem
    public string destiny;               // Casa de destino
    public string piece;                // Peça movimentada
}

/*
 Estrutura de dados para envio de informação sobre o vencedor da partida.
*/
[System.Serializable]
public class WinnerData
{
    public string type = "sendWinner";   // Tipo de operação
    public string winner;                // Nome do vencedor
    public string title;                 // Título do jogo da partida
}
