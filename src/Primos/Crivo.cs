namespace Conde.Primos;

/// <summary>
/// O crivo de Eratóstenes, guardando só os ímpares.
/// </summary>
/// <remarks>
/// <para>
/// Metade dos números é par e nenhum deles é primo depois do 2, então guardar
/// essa metade é desperdiçar exatamente metade da memória. Com um bit por ímpar,
/// o crivo de cem milhões cabe em seis megabytes, que é o que permite rodá-lo num
/// contêiner de CI sem pedir nada a ninguém.
/// </para>
/// <para>
/// A marcação começa em <c>p²</c> e não em <c>2p</c>: todo múltiplo de <c>p</c>
/// menor que <c>p²</c> tem um fator menor que <c>p</c> e já foi riscado. É a
/// diferença entre <c>O(n·log log n)</c> e algo bem pior.
/// </para>
/// </remarks>
public sealed class Crivo
{
    private readonly ulong[] _compostos;

    /// <summary>Até onde o crivo foi.</summary>
    public int Limite { get; }

    /// <summary>Quantos primos há até o limite, isto é, <c>pi(limite)</c>.</summary>
    public int Quantos { get; }

    /// <summary>Peneira todos os números de 0 até o limite.</summary>
    public Crivo(int limite)
    {
        if (limite < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(limite), limite, "é pelo menos 1");
        }

        Limite = limite;

        // O bit i representa o ímpar 2i+1. O 1 ocupa o bit 0 e é marcado na mão,
        // porque ele não é primo e nenhum crivo o risca.
        var bits = limite / 2 + 1;

        _compostos = new ulong[(bits + 63) / 64];

        Marcar(0);

        for (var i = 1L; (2 * i + 1) * (2 * i + 1) <= limite; i++)
        {
            if (Marcado(i))
            {
                continue;
            }

            var p = 2 * i + 1;

            for (var j = p * p / 2; j < bits; j += p)
            {
                Marcar(j);
            }
        }

        var quantos = limite >= 2 ? 1 : 0;

        for (var i = 1L; 2 * i + 1 <= limite; i++)
        {
            if (!Marcado(i))
            {
                quantos++;
            }
        }

        Quantos = quantos;
    }

    private bool Marcado(long i) => (_compostos[i >> 6] & (1UL << (int)(i & 63))) != 0;

    private void Marcar(long i) => _compostos[i >> 6] |= 1UL << (int)(i & 63);

    /// <summary>Se o número é primo.</summary>
    public bool EhPrimo(int n)
    {
        if (n > Limite)
        {
            throw new ArgumentOutOfRangeException(nameof(n), n, $"passa do limite {Limite}");
        }

        if (n < 2)
        {
            return false;
        }

        if (n % 2 == 0)
        {
            return n == 2;
        }

        return !Marcado(n / 2);
    }

    /// <summary>Os primos em ordem crescente.</summary>
    public IEnumerable<int> Primos()
    {
        if (Limite >= 2)
        {
            yield return 2;
        }

        for (var n = 3; n <= Limite; n += 2)
        {
            if (!Marcado(n / 2))
            {
                yield return n;
            }
        }
    }

    /// <summary>Os primos num vetor.</summary>
    public int[] Lista()
    {
        var lista = new int[Quantos];

        var onde = 0;

        foreach (var p in Primos())
        {
            lista[onde++] = p;
        }

        return lista;
    }

    /// <summary>
    /// O crivo segmentado: peneira um intervalo qualquer sem peneirar tudo abaixo
    /// dele.
    /// </summary>
    /// <returns>Um vetor em que a posição <c>i</c> fala do número <c>de + i</c>.</returns>
    /// <remarks>
    /// <para>
    /// Para saber quais números entre dez bilhões e dez bilhões e mil são primos,
    /// o crivo comum precisaria de dez bilhões de bits. O segmentado precisa dos
    /// primos até a raiz do fim do intervalo, que são cem mil, e de mil bits.
    /// </para>
    /// <para>
    /// É também o jeito de contar primos num intervalo grande sem estourar a
    /// memória: peneira em blocos que cabem no cache e descarta cada bloco.
    /// </para>
    /// </remarks>
    public static bool[] Segmento(long de, long ate)
    {
        if (de < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(de), de, "não é negativo");
        }

        if (ate < de)
        {
            throw new ArgumentOutOfRangeException(nameof(ate), ate, "não é menor que o início");
        }

        var ehPrimo = new bool[ate - de + 1];

        Array.Fill(ehPrimo, true);

        if (de < 2)
        {
            for (var n = de; n <= Math.Min(1, ate); n++)
            {
                ehPrimo[n - de] = false;
            }
        }

        var raiz = (int)Math.Sqrt(ate) + 1;

        foreach (var p in new Crivo(Math.Max(raiz, 2)).Primos())
        {
            // O primeiro múltiplo de p dentro do intervalo, nunca menor que p²:
            // abaixo disso o número tem um fator menor e seria riscado por ele.
            var primeiro = Math.Max((long)p * p, (de + p - 1) / p * p);

            for (var m = primeiro; m <= ate; m += p)
            {
                ehPrimo[m - de] = false;
            }
        }

        return ehPrimo;
    }
}
