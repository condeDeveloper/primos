using Xunit;

namespace Conde.Primos.Testes;

/// <summary>
/// O crivo contra a tabela publicada de pi(x), contra a divisão por tentativa, e
/// contra um segundo crivo que não se parece com ele.
/// </summary>
/// <remarks>
/// <para>
/// Contar primos é o raro caso em que existe um juiz publicado e exato. A
/// quantidade de primos até cada potência de dez está catalogada e conferida há
/// mais de um século, antes de haver computador para conferir, e qualquer defeito
/// no crivo aparece na primeira casa decimal.
/// </para>
/// <para>
/// O juiz publicado tem um limite, que é o tamanho da tabela. O ilimitado é a
/// divisão por tentativa, que é lenta demais para produção e óbvia demais para
/// errar.
/// </para>
/// </remarks>
public class CrivoTest
{
    /// <summary>
    /// A quantidade de primos até cada potência de dez.
    /// </summary>
    /// <remarks>
    /// Valores catalogados. Os primeiros são de Legendre e Gauss, no fim do
    /// século XVIII, e foram calculados à mão. O de dez milhões já é do século
    /// XIX.
    /// </remarks>
    private static readonly int[] PiDasPotencias =
    [
        0,          // 10^0
        4,          // 10^1
        25,         // 10^2
        168,        // 10^3
        1_229,      // 10^4
        9_592,      // 10^5
        78_498,     // 10^6
        664_579,    // 10^7
    ];

    [Fact(DisplayName = "pi(10^k) contra a tabela publicada, ate dez milhoes")]
    public void ContraATabela()
    {
        var limite = 1;

        for (var k = 0; k < PiDasPotencias.Length; k++)
        {
            Assert.Equal(PiDasPotencias[k], new Crivo(limite).Quantos);

            limite *= 10;
        }
    }

    [Fact(DisplayName = "o crivo contra a divisao por tentativa, em todo numero ate 300.000")]
    public void ContraADivisaoPorTentativa()
    {
        // O juiz sem limite de tabela: a definicao, aplicada a cada numero.
        static bool EhPrimoDevagar(int n)
        {
            if (n < 2)
            {
                return false;
            }

            for (var d = 2; (long)d * d <= n; d++)
            {
                if (n % d == 0)
                {
                    return false;
                }
            }

            return true;
        }

        var crivo = new Crivo(300_000);

        for (var n = 0; n <= 300_000; n++)
        {
            Assert.True(EhPrimoDevagar(n) == crivo.EhPrimo(n), $"discordaram em {n}");
        }
    }

    [Fact(DisplayName = "o crivo linear acha exatamente os mesmos primos, por outro caminho")]
    public void OsDoisCrivosConcordam()
    {
        // Eratostenes risca cada composto uma vez por fator primo; o linear risca
        // cada composto uma vez so, pelo menor fator. Os codigos nao se parecem,
        // entao concordarem e um fato com algum peso.
        var eratostenes = new Crivo(1_000_000);

        var linear = new Linear(1_000_000);

        Assert.Equal(78_498, linear.Primos.Length);
        Assert.Equal(eratostenes.Quantos, linear.Primos.Length);
        Assert.Equal(eratostenes.Lista(), linear.Primos);

        for (var n = 0; n <= 1_000_000; n++)
        {
            Assert.True(eratostenes.EhPrimo(n) == linear.EhPrimo(n), $"discordaram em {n}");
        }

        // E o menor fator primo bate com a fatoracao: o produto dos fatores e o
        // numero, e cada fator e primo.
        for (var n = 1; n <= 200_000; n++)
        {
            var fatores = linear.Fatorar(n);

            var produto = 1L;

            foreach (var p in fatores)
            {
                Assert.True(linear.EhPrimo(p), $"{p} apareceu na fatoracao de {n} e nao e primo");

                produto *= p;
            }

            Assert.Equal(n, produto);

            // Em ordem crescente, e o primeiro e o menor fator primo.
            Assert.Equal(fatores.OrderBy(p => p), fatores);

            if (n > 1)
            {
                Assert.Equal(linear.MenorFator[n], fatores[0]);
            }
        }
    }

    [Fact(DisplayName = "a soma de phi(d) sobre os divisores de n da exatamente n")]
    public void ASomaDasTotientes()
    {
        // Um juiz do tipo deducao: nao compara com implementacao nenhuma. A
        // identidade vale porque cada numero de 1 a n tem um unico maximo divisor
        // comum com n, e agrupar por ele parte o conjunto em pedacos de tamanho
        // phi(n/d).
        //
        // A identidade e sensivel: errar a totiente de UM numero quebra a soma de
        // todos os multiplos dele.
        const int Limite = 1_000_000;

        var linear = new Linear(Limite);

        var somas = new long[Limite + 1];

        for (var d = 1; d <= Limite; d++)
        {
            for (var m = d; m <= Limite; m += d)
            {
                somas[m] += linear.Totiente[d];
            }
        }

        for (var n = 1; n <= Limite; n++)
        {
            Assert.True(somas[n] == n, $"a soma das totientes dos divisores de {n} deu {somas[n]}");
        }
    }

    [Fact(DisplayName = "a soma de mu(d) sobre os divisores de n da 1 no 1 e zero no resto")]
    public void ASomaDeMobius()
    {
        // A outra identidade, e a razao de a funcao de Mobius existir: ela e o
        // inverso da funcao constante 1 na convolucao de Dirichlet. Tudo que se
        // faz com inversao de Mobius depende desta soma ser zero.
        const int Limite = 1_000_000;

        var linear = new Linear(Limite);

        var somas = new int[Limite + 1];

        for (var d = 1; d <= Limite; d++)
        {
            for (var m = d; m <= Limite; m += d)
            {
                somas[m] += linear.Mobius[d];
            }
        }

        Assert.Equal(1, somas[1]);

        for (var n = 2; n <= Limite; n++)
        {
            Assert.True(somas[n] == 0, $"a soma de Mobius dos divisores de {n} deu {somas[n]}");
        }

        // E a definicao direta: mu e zero se algum primo aparece ao quadrado, e
        // 1 ou -1 conforme a quantidade de fatores distintos.
        for (var n = 1; n <= 200_000; n++)
        {
            var fatores = linear.Fatorar(n);

            var distintos = fatores.Distinct().Count();

            var esperado = fatores.Count != distintos ? 0 : distintos % 2 == 0 ? 1 : -1;

            Assert.Equal(esperado, linear.Mobius[n]);
        }
    }

    /// <summary>
    /// As lacunas máximas entre primos consecutivos, e o primo em que cada uma
    /// aparece pela primeira vez.
    /// </summary>
    /// <remarks>
    /// Sequências A002386 e A005250, catalogadas desde Glaisher, no século XIX, e
    /// estendidas desde então. É um juiz publicado e exigente: não basta contar
    /// certo, é preciso acertar <b>em que primo</b> cada recorde acontece.
    /// </remarks>
    private static readonly (int Depois, int Lacuna)[] LacunasMaximas =
    [
        (2, 1), (3, 2), (7, 4), (23, 6), (89, 8), (113, 14), (523, 18), (887, 20),
        (1_129, 22), (1_327, 34), (9_551, 36), (15_683, 44), (19_609, 52),
        (31_397, 72), (155_921, 86), (360_653, 96), (370_261, 112), (492_113, 114),
        (1_349_533, 118), (1_357_201, 132), (2_010_733, 148), (4_652_353, 154),
        (17_051_707, 180), (20_831_323, 210), (47_326_693, 220),
    ];

    [Fact(DisplayName = "as lacunas maximas ate cem milhoes, contra o catalogo")]
    public void AsLacunasMaximas()
    {
        var crivo = new Crivo(100_000_000);

        var achadas = new List<(int Depois, int Lacuna)>();

        var anterior = 0;

        var maior = 0;

        foreach (var p in crivo.Primos())
        {
            if (anterior != 0 && p - anterior > maior)
            {
                maior = p - anterior;

                achadas.Add((anterior, maior));
            }

            anterior = p;
        }

        Assert.Equal(LacunasMaximas, achadas);

        // A maior lacuna ate cem milhoes e 220, enquanto a distancia media entre
        // primos nessa altura ja passa de 18. Lacunas de tamanho arbitrario
        // existem e sao triviais de construir, porque n! + 2 ate n! + n nao tem
        // um primo sequer, mas elas aparecem muito mais tarde do que isso sugere.
        Assert.Equal(220, maior);

        Assert.Equal(5_761_455, crivo.Quantos);
    }

    [Fact(DisplayName = "todo par ate um milhao e soma de dois primos, e o teste procura quem nao e")]
    public void Goldbach()
    {
        // Este teste nao confere uma implementacao: ele procura um contraexemplo
        // para uma conjectura aberta desde 1742. Nao achar nao prova nada, e e
        // justamente por isso que ele e honesto sobre o que faz.
        //
        // O que ele de fato garante e outra coisa, e essa sim vale: se o crivo
        // estivesse errado em qualquer numero, a decomposicao falharia em algum
        // par. Uma conjectura cara de testar vira um teste de regressao barato.
        const int Ate = 1_000_000;

        var crivo = new Crivo(Ate);

        var primos = crivo.Lista();

        var piorPrimo = 0;

        var piorN = 0;

        for (var n = 4; n <= Ate; n += 2)
        {
            var achou = false;

            foreach (var p in primos)
            {
                if (p > n / 2)
                {
                    break;
                }

                if (!crivo.EhPrimo(n - p))
                {
                    continue;
                }

                achou = true;

                if (p > piorPrimo)
                {
                    piorPrimo = p;
                    piorN = n;
                }

                break;
            }

            Assert.True(achou, $"{n} nao e soma de dois primos");
        }

        // O numero que surpreende: o MENOR primo que serve nunca passa de 523
        // abaixo de um milhao, e o pior caso e o 503.222. A conjectura e dificil
        // de provar e facil de satisfazer.
        //
        // Eu tinha escrito 389 aqui, de cabeca, e o teste corrigiu. Nenhum numero
        // deste repositorio foi inventado por mim: ou veio de um catalogo, ou
        // veio de uma medida.
        Assert.Equal(523, piorPrimo);
        Assert.Equal(503_222, piorN);
    }

    [Fact(DisplayName = "o crivo segmentado concorda com o crivo direto, e vai alem dele")]
    public void OSegmentado()
    {
        // Primeiro contra o crivo comum, onde os dois alcancam.
        var crivo = new Crivo(500_000);

        var sorteio = new Random(29);

        for (var caso = 0; caso < 300; caso++)
        {
            var de = sorteio.Next(0, 490_000);

            var ate = de + sorteio.Next(0, 10_000);

            var segmento = Crivo.Segmento(de, ate);

            for (var n = de; n <= ate; n++)
            {
                Assert.True(crivo.EhPrimo(n) == segmento[n - de], $"discordaram em {n}");
            }
        }

        // Agora onde o crivo comum nao chega: um intervalo logo acima de 2^40,
        // conferido contra o Miller-Rabin.
        const long Alto = 1L << 40;

        var alto = Crivo.Segmento(Alto, Alto + 20_000);

        for (var i = 0; i <= 20_000; i++)
        {
            Assert.True(Teste.EhPrimo((ulong)(Alto + i)) == alto[i],
                $"discordaram em {Alto + i}");
        }

        // E as bordas.
        Assert.Equal([false, false, true, true, false, true], Crivo.Segmento(0, 5));
        Assert.Equal([true], Crivo.Segmento(7, 7));

        Assert.Throws<ArgumentOutOfRangeException>(() => Crivo.Segmento(-1, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => Crivo.Segmento(10, 9));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Crivo(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Linear(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Crivo(10).EhPrimo(11));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Linear(10).Fatorar(0));
    }
}
