namespace Conde.Primos;

/// <summary>
/// Testes de primalidade que não dependem de peneirar tudo abaixo do número.
/// </summary>
/// <remarks>
/// Um crivo responde por todos os números até o limite, e o limite é a memória.
/// Para perguntar se um número de dezenove dígitos é primo, o caminho é outro: um
/// teste que olha só aquele número.
/// </remarks>
public static class Teste
{
    /// <summary>
    /// As bases que tornam o Miller-Rabin <b>determinístico</b> em todo
    /// <c>ulong</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// O Miller-Rabin é apresentado como probabilístico, e é, enquanto as bases
    /// são sorteadas. Com estas doze, os primeiros doze primos, ele é uma prova:
    /// está demonstrado que nenhum composto abaixo de 3,3 · 10²⁴ passa por todas,
    /// e isso cobre o <c>ulong</c> inteiro com folga de cinco ordens de grandeza.
    /// </para>
    /// <para>
    /// Os testes conferem que cada base da lista é necessária, usando os menores
    /// compostos que enganam os primeiros k primos. Tirar a última base deixa de
    /// quebrar só porque o contraexemplo é grande demais para o <c>ulong</c>.
    /// </para>
    /// </remarks>
    public static readonly ulong[] BasesDeterministicas =
        [2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37];

    /// <summary>Potência modular, com o produto feito em 128 bits.</summary>
    /// <remarks>
    /// O <c>UInt128</c> do .NET 7 resolve o problema mais chato desta conta: o
    /// produto de dois números de 64 bits não cabe em 64 bits, e sem ele seria
    /// preciso multiplicar em pedaços ou ir para a aritmética de Montgomery.
    /// </remarks>
    public static ulong PotenciaMod(ulong baseP, ulong expoente, ulong modulo)
    {
        if (modulo <= 1)
        {
            return 0;
        }

        UInt128 resultado = 1;

        UInt128 atual = baseP % modulo;

        while (expoente > 0)
        {
            if ((expoente & 1) != 0)
            {
                resultado = resultado * atual % modulo;
            }

            atual = atual * atual % modulo;

            expoente >>= 1;
        }

        return (ulong)resultado;
    }

    /// <summary>
    /// O pequeno teorema de Fermat como teste de primalidade.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Se <c>n</c> é primo, então <c>b^(n-1) ≡ 1 (mod n)</c> para todo <c>b</c>
    /// que não seja múltiplo de <c>n</c>. A volta é falsa, e é por isso que este
    /// teste está aqui: para o teste comparar com o Miller-Rabin e mostrar o
    /// tamanho do buraco.
    /// </para>
    /// <para>
    /// O buraco tem nome: os números de Carmichael passam no teste de Fermat em
    /// <b>toda</b> base que não divida eles. Nenhuma quantidade de bases resolve.
    /// </para>
    /// </remarks>
    public static bool Fermat(ulong n, ulong baseP = 2)
    {
        if (n < 2)
        {
            return false;
        }

        if (n % baseP == 0)
        {
            return n == baseP;
        }

        return PotenciaMod(baseP, n - 1, n) == 1;
    }

    /// <summary>
    /// Miller-Rabin com as bases dadas.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A diferença para o teste de Fermat é uma só, e é tudo: em vez de olhar
    /// apenas <c>b^(n-1)</c>, o teste acompanha os quadrados sucessivos a partir
    /// de <c>b^d</c> com <c>n - 1 = d · 2^s</c>. Num primo, a única raiz quadrada
    /// de 1 são 1 e -1, então a sequência só pode chegar em 1 passando por -1.
    /// </para>
    /// <para>
    /// É isso que os números de Carmichael não conseguem fingir: eles acertam o
    /// destino e erram o caminho.
    /// </para>
    /// </remarks>
    public static bool MillerRabin(ulong n, ReadOnlySpan<ulong> bases)
    {
        if (n < 2)
        {
            return false;
        }

        if ((n & 1) == 0)
        {
            return n == 2;
        }

        var d = n - 1;

        var s = 0;

        while ((d & 1) == 0)
        {
            d >>= 1;
            s++;
        }

        foreach (var b in bases)
        {
            // Uma base múltipla de n não diz nada: a potência dela é zero e o
            // teste não tem o que observar. É o caso de n ser a própria base,
            // que é o jeito de o 2 e o 3 passarem por aqui.
            if (b % n == 0)
            {
                continue;
            }

            var atual = PotenciaMod(b % n, d, n);

            if (atual is 1 || atual == n - 1)
            {
                continue;
            }

            var passou = false;

            for (var i = 1; i < s; i++)
            {
                atual = (ulong)((UInt128)atual * atual % n);

                if (atual == n - 1)
                {
                    passou = true;

                    break;
                }
            }

            if (!passou)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Se o número é primo, com certeza, em todo o alcance do <c>ulong</c>.</summary>
    public static bool EhPrimo(ulong n) => MillerRabin(n, BasesDeterministicas);

    /// <summary>
    /// O critério de Korselt: se o número é de Carmichael.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Um número de Carmichael é um composto que passa no teste de Fermat em toda
    /// base prima com ele. Testar isso pela definição exigiria rodar o teste em
    /// todas as bases; Korselt mostrou em 1899 que basta olhar a fatoração: o
    /// número tem de ser livre de quadrados, ter pelo menos três fatores primos,
    /// e <c>p - 1</c> tem de dividir <c>n - 1</c> para todo primo <c>p</c> que o
    /// divide.
    /// </para>
    /// <para>
    /// Korselt provou o critério sem conhecer um só exemplo. O primeiro, 561, só
    /// apareceu dezessete anos depois, com Carmichael.
    /// </para>
    /// </remarks>
    public static bool EhCarmichael(long n)
    {
        if (n < 3 || n % 2 == 0)
        {
            return false;
        }

        var sobra = n;

        var fatores = 0;

        for (long p = 3; p * p <= sobra; p += 2)
        {
            if (sobra % p != 0)
            {
                continue;
            }

            sobra /= p;

            // Livre de quadrados: nenhum primo pode aparecer duas vezes.
            if (sobra % p == 0)
            {
                return false;
            }

            if ((n - 1) % (p - 1) != 0)
            {
                return false;
            }

            fatores++;
        }

        if (sobra > 1)
        {
            if ((n - 1) % (sobra - 1) != 0)
            {
                return false;
            }

            fatores++;
        }

        return fatores >= 3;
    }
}
