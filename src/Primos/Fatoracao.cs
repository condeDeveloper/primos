namespace Conde.Primos;

/// <summary>
/// Fatoração por rô de Pollard, na forma de Brent.
/// </summary>
/// <remarks>
/// <para>
/// A divisão por tentativa acha o menor fator em <c>O(raiz de n)</c>. Para um
/// número de dezoito dígitos que seja produto de dois primos de nove, isso é um
/// bilhão de divisões. O rô de Pollard acha o mesmo fator em
/// <c>O(raiz do fator)</c>, que são trinta mil passos.
/// </para>
/// <para>
/// A ideia é o paradoxo do aniversário: a sequência <c>x := x² + c</c> módulo
/// <c>n</c> repete depois de uns <c>raiz de n</c> passos, mas módulo <c>p</c> ela
/// repete depois de uns <c>raiz de p</c>. Quando dois valores coincidem módulo
/// <c>p</c> sem coincidir módulo <c>n</c>, a diferença entre eles é um múltiplo
/// de <c>p</c> que não é múltiplo de <c>n</c>, e o máximo divisor comum entrega o
/// fator. O algoritmo nunca procura <c>p</c>: ele espera tropeçar.
/// </para>
/// </remarks>
public static class Fatoracao
{
    /// <summary>Máximo divisor comum, pelo algoritmo de Euclides.</summary>
    public static ulong Mdc(ulong a, ulong b)
    {
        while (b != 0)
        {
            (a, b) = (b, a % b);
        }

        return a;
    }

    /// <summary>
    /// Os fatores primos com multiplicidade, em ordem crescente.
    /// </summary>
    /// <remarks>
    /// O 1 fatora na lista vazia, que é a convenção certa: ele é o produto de
    /// nenhum primo, e é por isso que o teorema fundamental da aritmética não
    /// precisa abrir exceção para ele.
    /// </remarks>
    public static List<ulong> Fatorar(ulong n)
    {
        if (n == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(n), n, "o zero não fatora");
        }

        var fatores = new List<ulong>();

        Descer(n, fatores);

        fatores.Sort();

        return fatores;
    }

    private static void Descer(ulong n, List<ulong> fatores)
    {
        if (n == 1)
        {
            return;
        }

        // Os primos pequenos saem por tentativa: é mais rápido que o rô e tira do
        // caminho os casos em que ele se comporta mal.
        foreach (var p in (ulong[])[2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37])
        {
            while (n % p == 0)
            {
                fatores.Add(p);

                n /= p;
            }
        }

        if (n == 1)
        {
            return;
        }

        if (Teste.EhPrimo(n))
        {
            fatores.Add(n);

            return;
        }

        var fator = Ro(n);

        Descer(fator, fatores);
        Descer(n / fator, fatores);
    }

    /// <summary>
    /// Um fator não trivial de um composto, pelo rô de Pollard na forma de Brent.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A forma de Brent junta os máximos divisores comuns em lotes e multiplica as
    /// diferenças antes de chamar o algoritmo de Euclides. Como o produto de
    /// múltiplos de <c>p</c> continua múltiplo de <c>p</c>, o fator não se perde,
    /// e o número de chamadas cai por um fator de cem.
    /// </para>
    /// <para>
    /// O lote pode engolir o fator se os dois fatores aparecerem no mesmo lote: o
    /// máximo divisor comum vira o próprio <c>n</c>. Daí o passo a passo do
    /// final, que refaz o lote devagar. Sem ele, o algoritmo falha em alguns casos
    /// e acerta na maioria, que é a pior coisa que um algoritmo pode fazer.
    /// </para>
    /// </remarks>
    public static ulong Ro(ulong n)
    {
        if (n % 2 == 0)
        {
            return 2;
        }

        // A semente e fixa de proposito: um algoritmo que depende de sorte ainda
        // pode ser reproduzivel, e um defeito que so aparece com uma constante
        // tem de aparecer sempre.
        var sorteio = new Random(29);

        while (true)
        {
            var y = (ulong)sorteio.NextInt64(1, long.MaxValue) % n;

            var c = (ulong)sorteio.NextInt64(1, long.MaxValue) % n;

            ulong g = 1, r = 1, produto = 1, x = 0, guardado = 0;

            const ulong Lote = 128;

            while (g == 1)
            {
                x = y;

                for (ulong i = 0; i < r; i++)
                {
                    y = Proximo(y, c, n);
                }

                ulong feitos = 0;

                while (feitos < r && g == 1)
                {
                    guardado = y;

                    for (ulong i = 0; i < Math.Min(Lote, r - feitos); i++)
                    {
                        y = Proximo(y, c, n);

                        produto = (ulong)((UInt128)produto * (x > y ? x - y : y - x) % n);
                    }

                    g = Mdc(produto, n);

                    feitos += Lote;
                }

                r *= 2;
            }

            if (g == n)
            {
                // O lote engoliu os dois fatores de uma vez e o maximo divisor
                // comum virou o proprio n. A saida e refazer o ultimo lote um
                // passo de cada vez, a partir do ponto guardado.
                g = 1;

                while (g == 1)
                {
                    guardado = Proximo(guardado, c, n);

                    g = Mdc(x > guardado ? x - guardado : guardado - x, n);
                }
            }

            if (g != n)
            {
                return g;
            }
        }
    }

    private static ulong Proximo(ulong x, ulong c, ulong n) =>
        (ulong)(((UInt128)x * x + c) % n);

    /// <summary>Todos os divisores, em ordem crescente.</summary>
    public static List<ulong> Divisores(ulong n)
    {
        var divisores = new List<ulong> { 1 };

        foreach (var grupo in Fatorar(n).GroupBy(p => p))
        {
            var quantos = divisores.Count;

            ulong potencia = 1;

            foreach (var _ in grupo)
            {
                potencia *= grupo.Key;

                for (var i = 0; i < quantos; i++)
                {
                    divisores.Add(divisores[i] * potencia);
                }
            }
        }

        divisores.Sort();

        return divisores;
    }

    /// <summary>
    /// A totiente de Euler de um número qualquer, pela fatoração.
    /// </summary>
    public static ulong Totiente(ulong n)
    {
        if (n == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(n), n, "o zero não tem totiente");
        }

        var resultado = n;

        foreach (var p in Fatorar(n).Distinct())
        {
            resultado = resultado / p * (p - 1);
        }

        return resultado;
    }
}
