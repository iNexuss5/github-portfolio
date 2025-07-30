using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Collections;
using Unity.MLAgents.Integrations.Match3;

/*
 Esta classe é responsável por contactar um servidor externo para obter dinamicamente
 o nome da cena a ser carregada. A informação é transmitida em formato JSON e inclui
 também configurações como a dificuldade da IA e o matchId.
*/
public class SceneLoader : MonoBehaviour
{

    void Start()
    {
        StartCoroutine(LoadSceneFromServer()); // Inicia a rotina assíncrona de carregamento
    }

    // Corrotina responsável por contactar o servidor e carregar uma nova cena
    IEnumerator LoadSceneFromServer()
    {
        // Geração da URL com timestamp para evitar cache do browser/servidor
        string url = SendToDatabase.defaultUrl + "get_scene.php?ts=" + System.DateTime.Now.Ticks;

        UnityWebRequest www = UnityWebRequest.Get(url); // Prepara a requisição HTTP GET
        yield return www.SendWebRequest();              // Aguarda a resposta do servidor

        if (www.result == UnityWebRequest.Result.Success)
        {
            // Limpa preferências anteriores relacionadas à sessão
            PlayerPrefs.DeleteKey("matchId");
            PlayerPrefs.DeleteKey("ai_difficulty");

            string json = www.downloadHandler.text;
            Debug.Log("JSON bruto recebido: " + json);

            try
            {
                // Converte o JSON recebido num objeto C#
                SceneResponse response = JsonUtility.FromJson<SceneResponse>(json);

                // Verifica se a cena recebida está disponível no projeto
                if (Application.CanStreamedLevelBeLoaded(response.scene))
                {
                    // Armazena a dificuldade da IA, se fornecida
                    if (!string.IsNullOrEmpty(response.ai_dif))
                    {
                        PlayerPrefs.SetString("ai_difficulty", response.ai_dif);
                    }

                    // Armazena o ID do jogo, se fornecido; caso contrário, remove o antigo
                    if (!string.IsNullOrEmpty(response.matchId))
                    {
                        PlayerPrefs.SetString("matchId", response.matchId);
                    }
                    else
                    {
                        PlayerPrefs.DeleteKey("matchId");
                    }

                    PlayerPrefs.Save(); // Guarda as preferências localmente

                    // Transição para a cena indicada pelo servidor
                    SceneManager.LoadScene(response.scene);
                }
                else
                {
                    // A cena especificada não existe ou não foi incluída nas build settings
                    Debug.LogError("Cena não encontrada: " + response.scene);
                }
            }
            catch (System.Exception ex)
            {
                // Erro ao interpretar o JSON (possivelmente mal formatado ou inesperado)
                Debug.LogError("Erro ao processar a resposta do servidor: " + ex.Message);
            }
        }
        else
        {
            // Falha na comunicação com o servidor
            Debug.LogError("Erro ao buscar cena: " + www.error);
        }
    }

    /*
     Classe auxiliar para representar a estrutura da resposta JSON do servidor.
    */
    [System.Serializable]
    public class SceneResponse
    {
        public string scene;     // Nome da cena a ser carregada
        public string ai_dif;    // Nível de dificuldade da IA
        public string matchId;   // Identificador da partida
    }
}
