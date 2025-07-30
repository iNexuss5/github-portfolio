using System;
using System.Linq;
using UnityEngine;
using Random = System.Random;

/*
 Classe responsável pela implementação do algoritmo Monte Carlo Tree Search (MCTS)
 aplicado a jogos por turnos. Gera uma árvore de pesquisa baseada em simulações
 aleatórias para determinar o melhor movimento possível em cada estado.
*/

public class MCTS
{
    public TurnBasedGame turnBasedGame;  // Referência à instância geral do jogo por turnos

    private string[] RootState { get; set; }  // Estado do tabuleiro na raiz da árvore
    public Node Root { get; set; }            // Nó raiz da árvore de pesquisa
    public double RunTime { get; set; }       // Tempo total gasto na pesquisa
    public int NodeCount { get; set; }        // Número total de nós gerados
    public int NumRollouts { get; set; }      // Número total de simulações realizadas

    // Construtor que inicializa o MCTS com o estado atual do jogo
    public MCTS(TurnBasedGame turnBasedGame)
    {
        this.turnBasedGame = turnBasedGame;
        RootState = (string[])turnBasedGame.localBoardState.Clone();  // Clona o estado do tabuleiro
        Root = new Node(null, null);  // Cria a raiz sem movimento nem pai
        RunTime = 0;
        NodeCount = 0;
        NumRollouts = 0;
    }

    // Executa a selecção do nó a expandir com base nos valores dos filhos
    public (Node, string[]) SelectNode()
    {
        Node node = Root;
        string[] state = (string[])RootState.Clone();

        // Percorre os filhos com maior valor até encontrar um nó não visitado
        while (node.Children.Count != 0)
        {
            var children = node.Children.Values.ToList();
            double maxValue = children.Max(n => n.Value());
            var maxNodes = children.Where(n => n.Value() == maxValue).ToList();

            node = maxNodes[new Random().Next(maxNodes.Count)];  // Escolhe aleatoriamente entre os melhores
            turnBasedGame.ApplyMove(state, node.Move);           // Aplica o movimento ao estado atual

            if (node.N == 0)
            {
                return (node, state);  // Nó não visitado encontrado
            }
        }

        // Se ainda possível, expande o nó e escolhe um dos filhos
        if (Expand(node, state))
        {
            node = node.Children.Values.ToList()[new Random().Next(node.Children.Count)];
            turnBasedGame.ApplyMove(state, node.Move);
        }

        return (node, state);
    }

    // Expande o nó fornecido criando os seus filhos com todos os movimentos legais
    public bool Expand(Node parent, string[] state)
    {
        if (turnBasedGame.GameOver(state))
        {
            return false;  // Jogo terminado — não há mais expansão possível
        }

        var children = turnBasedGame.GetLegalMoves(state)
            .Select(move => new Node(move, parent))  // Cria novos nós para cada movimento
            .ToList();

        parent.AddChildren(children.ToDictionary(c => c.Move, c => c));  // Adiciona os filhos ao nó pai
        return true;
    }

    // Realiza uma simulação aleatória até ao fim do jogo
    public int RollOut(string[] state)
    {
        while (!turnBasedGame.GameOver(state))
        {
            // Escolhe aleatoriamente um movimento legal e aplica-o
            turnBasedGame.ApplyMove(state, turnBasedGame.GetLegalMoves(state)
                .OrderBy(x => Guid.NewGuid()).First());
        }

        return turnBasedGame.GetOutcome(state);  // Devolve o resultado final (vencedor ou empate)
    }

    // Propaga o resultado da simulação ao longo do caminho até à raiz
    public void BackPropagate(Node node, int turn, int outcome)
    {
        double reward = outcome == turn ? 1 : 0;  // Vitória para o jogador do turno

        while (node != null)
        {
            node.N++;         // Incrementa o número de visitas
            node.Q += reward; // Soma a recompensa
            node = node.Parent;

            // Recompensa inversa para o adversário ou empate
            reward = outcome == 0 ? 0.5 : 1 - reward;
        }
    }

    // Executa o algoritmo MCTS durante o tempo definido (em segundos)
    public void Search(float timeLimit)
    {
        var startTime = DateTime.Now;
        int numRollouts = 0;

        // Continua a realizar rollouts enquanto houver tempo
        while ((DateTime.Now - startTime).TotalSeconds < timeLimit)
        {
            var (node, state) = SelectNode();              // Selecciona nó a simular
            int turn = turnBasedGame.GetTurn(state);       // Turno atual
            int outcome = RollOut(state);                  // Simula até ao fim
            BackPropagate(node, turn, outcome);            // Atualiza estatísticas
            numRollouts++;
        }

        RunTime = (DateTime.Now - startTime).TotalSeconds;  // Regista o tempo total
        NumRollouts = numRollouts;                          // Regista o nº total de simulações
    }

    // Devolve o melhor movimento baseado no UTC
    public string BestMove()
    {
        if (turnBasedGame.GameOver(RootState))
        {
            Debug.LogWarning("O jogo já terminou — não existe melhor jogada possível.");
            return "-1";  // Indicação de jogo terminado
        }

        double maxValue = Root.Children.Values.Max(n => n.N);
        var maxNodes = Root.Children.Values.Where(n => n.N == maxValue).ToList();
        var bestChild = maxNodes[new Random().Next(maxNodes.Count)];

        return bestChild.Move;  // Movimento mais promissor
    }

    // Aplica um movimento no jogo e atualiza a raiz da árvore
    public void Move(string move)
    {
        string[] newState = (string[])RootState.Clone();
        turnBasedGame.ApplyMove(newState, move);

        // Se o movimento existe na árvore, avança para esse nó
        if (Root.Children.ContainsKey(move))
        {
            Root = Root.Children[move];
        }
        else
        {
            Root = new Node(null, null);  // Reinicia a árvore se não existir continuidade
        }

        RootState = newState;  // Atualiza o estado raiz
    }

    // Retorna estatísticas da pesquisa: número de simulações e tempo decorrido
    public (int, double) Statistics()
    {
        return (NumRollouts, RunTime);
    }

    //  Imprime o número de visitas e taxa de vitória de cada movimento
    public void PrintExplorationCounts()
    {
        foreach (var child in Root.Children)
        {
            string move = child.Key;
            int visits = child.Value.N;
            double winRate = child.Value.N > 0 ? child.Value.Q / child.Value.N : 0;

            //Debug.Log($"Movimento: {move}, Visitas: {visits}, Taxa de Vitória: {winRate:F2}");
        }
    }
}
