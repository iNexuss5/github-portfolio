using UnityEngine;
using UnityEngine.UI;

/*
 Classe responsável por gerar dinamicamente uma grelha de botões numa interface gráfica
 com base nas dimensões e margens definidas. Cada botão representa uma célula
 do tabuleiro num jogo por turnos.
*/
public class GridGenerator : MonoBehaviour
{
    public GameObject buttonPrefab;           // Prefab que define o aspeto e comportamento de cada botão
    public RectTransform boardArea;           // Área UI (painel) onde os botões serão instanciados
    public int rows = 6;                      // Número de linhas da grelha
    public int columns = 7;                   // Número de colunas da grelha
    public TurnBasedGame gameManager;         // Referência ao jogo
    public float horizontalPadding = 28f;     // Margem horizontal
    public float verticalPadding = 22f;       // Margem vertical
    public float spacing = 4f;                // Espaço entre células (botões)

    /*
     Gera dinamicamente a grelha de botões de acordo com as configurações especificadas.
     Calcula as dimensões de cada botão para que se ajustem corretamente à área disponível.
    */
    public void GenerateGrid()
    {
        // Calcula a largura e altura útil da área do tabuleiro, subtraindo margens
        float totalWidth = boardArea.rect.width - (2 * horizontalPadding);
        float totalHeight = boardArea.rect.height - (2 * verticalPadding);

        // Espaço total ocupado pelos espaçamentos entre colunas e linhas
        float totalSpacingX = (columns - 1) * spacing;
        float totalSpacingY = (rows - 1) * spacing;

        // Calcula a largura e altura de cada célula (botão)
        float cellWidth = (totalWidth - totalSpacingX) / columns;
        float cellHeight = (totalHeight - totalSpacingY) / rows;

        // Criação de cada botão linha a linha, coluna a coluna
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                // Instancia um novo botão a partir do prefab
                GameObject newButton = Instantiate(buttonPrefab, boardArea);

                // Atribui nome e tag para identificação
                newButton.name = $"{y}{x}";
                newButton.tag = "Button";

                Button btn = newButton.GetComponent<Button>();

                // Define se o botão é interativo ou não, consoante o modo do jogo (IA ou humano)
                if (gameManager.isAI)
                    btn.interactable = true;
                else
                    btn.interactable = false;

                // Define o tamanho da célula
                RectTransform rect = newButton.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(cellWidth, cellHeight);

                // Define o sistema de ancoragem e o ponto de origem (canto superior esquerdo)
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0, 1);

                // Calcula a posição do botão dentro da grelha
                float posX = horizontalPadding + x * (cellWidth + spacing);
                float posY = -verticalPadding - y * (cellHeight + spacing);
                rect.anchoredPosition = new Vector2(posX, posY);

                // Define o comportamento de clique para o botão, com base no índice
                int buttonIndex = y * columns + x;
                newButton.GetComponent<Button>().onClick.AddListener(() => OnButtonClick(buttonIndex));
            }
        }

        Debug.Log("grid"); // Log de controlo para verificação visual no console
    }

    /*
     Método chamado quando um botão é pressionado.
     Envia o índice correspondente ao gestor do jogo para processar a jogada.
    */
    void OnButtonClick(int index)
    {
        if (gameManager != null)
        {
            gameManager.PressButton(index); // Pressiona a coluna correspondente ao índice
        }
    }
}
