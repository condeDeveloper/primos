using Xunit;

namespace Conde.Primos.Testes;

/// <summary>
/// O Miller-Rabin contra o crivo, e contra o catálogo dos compostos que enganam
/// cada quantidade de bases.
/// </summary>
/// <remarks>
/// <para>
/// O crivo é juiz até onde a memória alcança. Acima disso o juiz é outro, e é dos
/// melhores que existem: a sequência dos menores compostos que passam no
/// Miller-Rabin com as primeiras k bases primas é catalogada, e cada valor dela é
/// um contraexemplo construído por alguém que procurou de propósito.
/// </para>
/// <para>
/// Esse catálogo responde a pergunta que nenhum teste aleatório responde: as doze
/// bases são mesmo necessárias, ou alguém copiou uma lista maior do que precisava?
/// </para>
/// </remarks>
public class TesteTest
{
    /// <summary>
    /// Os menores compostos que passam no Miller-Rabin com as primeiras k bases
    /// primas, e o k de cada um.
    /// </summary>
    /// <remarks>
    /// Sequência A014233. Os primeiros são de Pomerance, Selfridge e Wagstaff, de
    /// 1980, e os maiores de Jaeschke, de 1993. O da décima segunda base passa de
    /// 3 · 10²³ e não cabe num <c>ulong</c>, que é exatamente por que doze bases
    /// bastam aqui.
    /// </remarks>
    private static readonly (ulong Composto, int Bases)[] Mentirosos =
    [
        (2_047UL, 1),
        (1_373_653UL, 2),
        (25_326_001UL, 3),
        (3_215_031_751UL, 4),
        (2_152_302_898_747UL, 5),
        (3_474_749_660_383UL, 6),
        (341_550_071_728_321UL, 8),
        (3_825_123_056_546_413_051UL, 11),
    ];

    [Fact(DisplayName = "cada base da lista e necessaria, e o catalogo prova qual")]
    public void CadaBaseEhNecessaria()
    {
        foreach (var (composto, bases) in Mentirosos)
        {
            // Com k bases ele passa: o teste diz "primo" sobre um composto.
            Assert.True(
                Teste.MillerRabin(composto, Teste.BasesDeterministicas.AsSpan(0, bases)),
                $"{composto} devia enganar as primeiras {bases} bases");

            // Com uma base a mais, cai.
            Assert.False(
                Teste.MillerRabin(composto, Teste.BasesDeterministicas.AsSpan(0, bases + 1)),
                $"{composto} nao devia passar com {bases + 1} bases");

            // E com a lista inteira, cai também.
            Assert.False(Teste.EhPrimo(composto));

            // Composto mesmo: a fatoração mostra.
            var fatores = Fatoracao.Fatorar(composto);

            Assert.True(fatores.Count >= 2, $"{composto} fatorou em {fatores.Count} fatores");

            UInt128 produto = 1;

            foreach (var p in fatores)
            {
                Assert.True(Teste.EhPrimo(p));

                produto *= p;
            }

            Assert.Equal((UInt128)composto, produto);
        }

        // O maior deles tem dezenove digitos e passa por onze bases. A duodecima
        // e o que separa "provavelmente primo" de "primo".
        Assert.Equal(19, Mentirosos[^1].Composto.ToString().Length);
    }

    [Fact(DisplayName = "Miller-Rabin contra o crivo, em todo numero ate um milhao")]
    public void ContraOCrivo()
    {
        var crivo = new Crivo(1_000_000);

        for (var n = 0; n <= 1_000_000; n++)
        {
            Assert.True(crivo.EhPrimo(n) == Teste.EhPrimo((ulong)n), $"discordaram em {n}");
        }
    }

    [Fact(DisplayName = "o teste de Fermat erra 245 vezes abaixo de um milhao, e o de Miller-Rabin nenhuma")]
    public void OBuracoDoTesteDeFermat()
    {
        // A diferenca entre os dois testes, contada em vez de explicada.
        //
        // Os compostos que passam no teste de Fermat na base 2 sao os numeros de
        // Poulet, e a contagem deles abaixo de cada potencia de dez e catalogada:
        // 3, 22, 78, 245, 750 para 10^3 ate 10^7.
        var crivo = new Crivo(1_000_000);

        var poulet = 0;

        var fortes = 0;

        var carmichael = 0;

        ReadOnlySpan<ulong> soADois = [2];

        for (var n = 3; n <= 1_000_000; n += 2)
        {
            if (crivo.EhPrimo(n))
            {
                continue;
            }

            if (!Teste.Fermat((ulong)n))
            {
                continue;
            }

            poulet++;

            // Entre os que enganam o Fermat, quais ainda enganam o Miller-Rabin
            // com a mesma base?
            if (Teste.MillerRabin((ulong)n, soADois))
            {
                fortes++;
            }

            // E quais sao de Carmichael, isto e, enganam o Fermat em TODA base?
            if (Teste.EhCarmichael(n))
            {
                carmichael++;
            }
        }

        Assert.Equal(245, poulet);

        // Os pseudoprimos FORTES na base 2 abaixo de um milhao sao 46. Ou seja:
        // acompanhar os quadrados sucessivos, em vez de olhar so o destino,
        // derruba 199 dos 245 impostores sem gastar uma base a mais.
        Assert.Equal(46, fortes);

        // E os de Carmichael abaixo de um milhao sao 43. Esses passam no Fermat
        // em toda base prima com eles, entao nenhuma quantidade de bases salva o
        // teste de Fermat. O Miller-Rabin pega todos, e o teste abaixo confere.
        Assert.Equal(43, carmichael);

        foreach (var conhecido in (long[])[561, 1105, 1729, 2465, 2821, 6601, 8911])
        {
            Assert.True(Teste.EhCarmichael(conhecido));
            Assert.True(Teste.Fermat((ulong)conhecido));
            Assert.False(Teste.EhPrimo((ulong)conhecido));
        }

        // Korselt publicou o criterio em 1899 sem conhecer um exemplo. O 561 so
        // apareceu dezessete anos depois.
        Assert.False(Teste.EhCarmichael(562));
        Assert.False(Teste.EhCarmichael(563));
        Assert.False(Teste.EhCarmichael(9));
        Assert.False(Teste.EhCarmichael(2));
        Assert.False(Teste.EhCarmichael(1));
    }

    [Fact(DisplayName = "um numero de Carmichael engana o Fermat em TODA base prima com ele")]
    public void OCarmichaelEnganaTodaBase()
    {
        // A definicao, conferida por forca bruta no menor deles. 561 = 3 x 11 x
        // 17, e as 320 bases primas com ele passam todas.
        const int Carmichael = 561;

        var enganadas = 0;

        for (ulong b = 2; b < Carmichael; b++)
        {
            if (Fatoracao.Mdc(b, Carmichael) != 1)
            {
                continue;
            }

            Assert.True(Teste.Fermat(Carmichael, b), $"a base {b} nao foi enganada");

            enganadas++;
        }

        // phi(561) = 2 x 10 x 16 = 320, menos a base 1 que nao conta.
        Assert.Equal(319, enganadas);
        Assert.Equal(320UL, Fatoracao.Totiente(Carmichael));

        // O Miller-Rabin precisa de quantas bases para derrubar o 561? De uma.
        ReadOnlySpan<ulong> soADois = [2];

        Assert.False(Teste.MillerRabin(Carmichael, soADois));
    }

    [Fact(DisplayName = "a potencia modular contra a conta feita devagar")]
    public void APotenciaModular()
    {
        var sorteio = new Random(29);

        for (var caso = 0; caso < 20_000; caso++)
        {
            var modulo = (ulong)sorteio.Next(2, 10_000);

            var baseP = (ulong)sorteio.Next(0, 10_000);

            var expoente = (ulong)sorteio.Next(0, 200);

            // O juiz: multiplicar uma vez por unidade do expoente.
            UInt128 devagar = 1;

            for (ulong i = 0; i < expoente; i++)
            {
                devagar = devagar * (baseP % modulo) % modulo;
            }

            Assert.Equal((ulong)devagar, Teste.PotenciaMod(baseP, expoente, modulo));
        }

        // E o caso que o UInt128 existe para resolver: modulo perto de 2^64, em
        // que o produto de dois fatores nao cabe em 64 bits.
        const ulong Grande = 18_446_744_073_709_551_557UL;

        Assert.True(Teste.EhPrimo(Grande));

        // Fermat: b^(p-1) = 1 para todo b, se p e primo.
        for (ulong b = 2; b < 50; b++)
        {
            Assert.Equal(1UL, Teste.PotenciaMod(b, Grande - 1, Grande));
        }

        Assert.Equal(0UL, Teste.PotenciaMod(5, 3, 1));
        Assert.False(Teste.EhPrimo(0));
        Assert.False(Teste.EhPrimo(1));
        Assert.True(Teste.EhPrimo(2));
        Assert.True(Teste.EhPrimo(3));
        Assert.False(Teste.EhPrimo(4));
    }
}
