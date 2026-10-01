namespace Conde.Primos;

/// <summary>
/// O crivo linear, que de quebra entrega o menor fator primo, a função totiente
/// de Euler e a função de Möbius.
/// </summary>
/// <remarks>
/// <para>
/// O crivo de Eratóstenes risca cada composto uma vez por fator primo dele: o 12
/// é riscado pelo 2 e pelo 3. Este risca cada composto <b>exatamente uma vez</b>,
/// pelo menor fator primo, e é daí que vem o nome. Na prática ele não é mais
/// rápido, porque a conta extra de cada passo come o que o trabalho a menos
/// economiza, e ele gasta um inteiro por número em vez de um bit.
/// </para>
/// <para>
/// O que ele dá de verdade são as três tabelas de graça. E, para esta biblioteca,
/// dá outra coisa: um <b>segundo caminho independente</b> até a lista de primos.
/// Os testes cruzam os dois, e eles concordarem é um fato com algum peso, porque
/// os algoritmos não se parecem.
/// </para>
/// </remarks>
public sealed class Linear
{
    /// <summary>Até onde o crivo foi.</summary>
    public int Limite { get; }

    /// <summary>O menor fator primo de cada número. Vale 0 em 0 e em 1.</summary>
    public int[] MenorFator { get; }

    /// <summary>
    /// A totiente de Euler: quantos números de 1 a n são primos com n.
    /// </summary>
    public int[] Totiente { get; }

    /// <summary>
    /// A função de Möbius: 0 se algum primo aparece ao quadrado, senão 1 ou -1
    /// conforme a quantidade de fatores primos seja par ou ímpar.
    /// </summary>
    public sbyte[] Mobius { get; }

    /// <summary>Os primos até o limite, na ordem em que foram achados.</summary>
    public int[] Primos { get; }

    /// <summary>Peneira de 0 até o limite.</summary>
    public Linear(int limite)
    {
        if (limite < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(limite), limite, "é pelo menos 1");
        }

        Limite = limite;

        MenorFator = new int[limite + 1];
        Totiente = new int[limite + 1];
        Mobius = new sbyte[limite + 1];

        // O 1 é produto de nenhum primo: a totiente dele é 1 e a Möbius também,
        // porque zero é par. É convenção, e é a convenção que faz as duas somas
        // sobre divisores fecharem.
        Totiente[1] = 1;
        Mobius[1] = 1;

        var achados = new List<int>();

        for (var i = 2; i <= limite; i++)
        {
            if (MenorFator[i] == 0)
            {
                MenorFator[i] = i;
                Totiente[i] = i - 1;
                Mobius[i] = -1;

                achados.Add(i);
            }

            foreach (var p in achados)
            {
                if (p > MenorFator[i] || (long)i * p > limite)
                {
                    break;
                }

                var composto = i * p;

                MenorFator[composto] = p;

                if (i % p == 0)
                {
                    // p já divide i, então p aparece ao quadrado no produto: a
                    // Möbius zera e a totiente multiplica por p e não por p-1.
                    Totiente[composto] = Totiente[i] * p;
                    Mobius[composto] = 0;
                }
                else
                {
                    Totiente[composto] = Totiente[i] * (p - 1);
                    Mobius[composto] = (sbyte)-Mobius[i];
                }
            }
        }

        Primos = [.. achados];
    }

    /// <summary>Se o número é primo.</summary>
    public bool EhPrimo(int n) => n >= 2 && n <= Limite && MenorFator[n] == n;

    /// <summary>
    /// Os fatores primos com multiplicidade, em ordem crescente, direto da tabela.
    /// </summary>
    /// <remarks>
    /// Com o menor fator primo tabelado, fatorar é descer dividindo: nenhuma
    /// tentativa, nenhum teste de primalidade. O custo é o número de fatores, que
    /// é no máximo o logaritmo na base 2.
    /// </remarks>
    public List<int> Fatorar(int n)
    {
        if (n < 1 || n > Limite)
        {
            throw new ArgumentOutOfRangeException(nameof(n), n, $"está fora de 1 até {Limite}");
        }

        var fatores = new List<int>();

        while (n > 1)
        {
            var p = MenorFator[n];

            fatores.Add(p);

            n /= p;
        }

        return fatores;
    }
}
