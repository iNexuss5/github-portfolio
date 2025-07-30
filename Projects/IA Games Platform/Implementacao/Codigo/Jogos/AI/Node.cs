using System;
using System.Collections.Generic;

/*
 Representa um nó na árvore de pesquisa MCTS.
 Cada nó armazena o movimento que o gerou, estatísticas de simulação,
 referência ao nó pai e os seus filhos.
*/

public class Node
{
    public string Move { get; set; }                    // Movimento associado ao nó
    public Node Parent { get; set; }                    // Referência ao nó pai
    public int N { get; set; }                          // Número de visitas ao nó
    public double Q { get; set; }                       // Soma das recompensas acumuladas
    public Dictionary<string, Node> Children { get; set; }  // Dicionário de nós filhos, indexados pelo movimento

    // Construtor do nó que recebe o movimento e o nó pai
    public Node(string move, Node parent)
    {
        Move = move;
        Parent = parent;
        N = 0;                     // Inicialmente não visitado
        Q = 0;                     // Sem recompensa acumulada
        Children = new Dictionary<string, Node>();  // Inicializa o conjunto de filhos
    }

    // Adiciona múltiplos filhos ao nó atual
    public void AddChildren(Dictionary<string, Node> children)
    {
        foreach (var child in children)
        {
            Children[child.Key] = child.Value;  // Associa cada filho ao seu movimento
        }
    }

    // Calcula o valor do nó com base na fórmula UCT (Upper Confidence Bound for Trees)
    public double Value()
    {
        double exp = Math.Sqrt(2);  // Constante de exploração típica

        if (N == 0)
        {
            // Dá prioridade máxima a nós não explorados
            return exp == 0 ? 0 : double.MaxValue;
        }
        else
        {
            /*
             Fórmula UCT:
             (Q / N) = valor médio da recompensa
             exp * sqrt(ln(Npai) / N) = termo de exploração
            */
            return (Q / N) + exp * Math.Sqrt(Math.Log(Parent.N + 1) / (N + 1));
        }
    }
}
