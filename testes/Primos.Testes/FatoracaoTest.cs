using Xunit;

namespace Conde.Primos.Testes;

/// <summary>
/// A fatoração contra a tabela do crivo linear, e contra ela mesma por
/// reconstrução.
/// </summary>
/// <remarks>
/// <para>
/// Fatoração tem a propriedade rara de ser difícil de fazer e trivial de
/// conferir: multiplicar os fatores de volta custa microssegundos. Então o juiz
/// ilimitado está ali de graça, para qualquer número, inclusive os que nenhuma
/// tabela alcança.
/// </para>
/// <para>
/// Só que reconstruir o número não basta: <c>12 = 4 · 3</c> reconstrói e está
/// errado. A segunda metade do juiz é o teste de primalidade de cada fator, e aí
/// a resposta é única pelo teorema fundamental da aritmética.
/// </para>
/// </remarks>
public class FatoracaoTest
{
    [Fact(DisplayName = "o ro de Pollard contra a tabela do crivo linear, em todo numero ate 200.000")]
    public void ContraOCrivoLinear()
    {
        // Dois caminhos que não se parecem: um desce pela tabela do menor fator
        // primo, o outro tropeça num fator por acaso e recursiona.
        var linear = new Linear(200_000);

        for (var n = 1; n <= 200_000; n++)
        {
            var pelaTabela = linear.Fatorar(n).Select(p => (ulong)p).ToList();

            var peloRo = Fatoracao.Fatorar((ulong)n);

            Assert.True(pelaTabela.SequenceEqual(peloRo),
                $"{n}: tabela deu [{string.Join(",", pelaTabela)}] e ro deu [{string.Join(",", peloRo)}]");
        }

        // O 1 fatora na lista vazia: ele é o produto de nenhum primo.
        Assert.Empty(Fatoracao.Fatorar(1));

        Assert.Throws<ArgumentOutOfRangeException>(() => Fatoracao.Fatorar(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Fatoracao.Totiente(0));
    }

    [Fact(DisplayName = "numeros grandes: o produto volta e cada fator e primo")]
    public void OsGrandes()
    {
        var sorteio = new Random(29);

        for (var caso = 0; caso < 2_000; caso++)
        {
            var n = (ulong)sorteio.NextInt64(1, long.MaxValue);

            var fatores = Fatoracao.Fatorar(n);

            UInt128 produto = 1;

            foreach (var p in fatores)
            {
                Assert.True(Teste.EhPrimo(p), $"{p} apareceu na fatoracao de {n} e nao e primo");

                produto *= p;
            }

            Assert.Equal((UInt128)n, produto);

            Assert.Equal(fatores.OrderBy(p => p), fatores);
        }
    }

    [Fact(DisplayName = "o caso duro: produto de dois primos de nove digitos cada")]
    public void OSemiprimo()
    {
        // É para isto que o rô existe. A divisão por tentativa acharia o menor
        // fator depois de um bilhão de divisões; o rô tropeça nele em uns trinta
        // mil passos, porque o custo é a raiz do FATOR e não a raiz do número.
        static ulong PrimoPertoDe(ulong alvo)
        {
            while (!Teste.EhPrimo(alvo))
            {
                alvo++;
            }

            return alvo;
        }

        var p = PrimoPertoDe(1_000_000_007);

        var q = PrimoPertoDe(2_147_483_647);

        var n = p * q;

        Assert.Equal([p, q], Fatoracao.Fatorar(n));

        // E o caso em que o rô se comporta mal: a potência de um primo. Os dois
        // fatores são iguais, então muitas das diferenças que ele calcula são
        // múltiplas dos dois ao mesmo tempo e o máximo divisor comum vira o
        // próprio n.
        foreach (var pequeno in (ulong[])[1_000_003, 1_000_033, 2_000_003])
        {
            Assert.Equal([pequeno, pequeno], Fatoracao.Fatorar(pequeno * pequeno));

            Assert.Equal([pequeno, pequeno, pequeno],
                Fatoracao.Fatorar(pequeno * pequeno * pequeno));
        }

        // Três primos grandes de uma vez.
        var tres = (ulong)104_729 * 1_299_709 * 15_485_863;

        Assert.Equal([104_729UL, 1_299_709UL, 15_485_863UL], Fatoracao.Fatorar(tres));

        // O maior primo que cabe num ulong.
        Assert.Equal([18_446_744_073_709_551_557UL],
            Fatoracao.Fatorar(18_446_744_073_709_551_557UL));

        // E uma potência de dois do tamanho do tipo.
        Assert.Equal(Enumerable.Repeat(2UL, 63), Fatoracao.Fatorar(1UL << 63));
    }

    [Fact(DisplayName = "os divisores contra a enumeracao direta, e contra a formula")]
    public void OsDivisores()
    {
        for (var n = 1; n <= 30_000; n++)
        {
            // O juiz óbvio e caro: testar todo número de 1 a n.
            var naMao = new List<ulong>();

            for (var d = 1; d <= n; d++)
            {
                if (n % d == 0)
                {
                    naMao.Add((ulong)d);
                }
            }

            var achados = Fatoracao.Divisores((ulong)n);

            Assert.True(naMao.SequenceEqual(achados), $"discordaram em {n}");

            // E a dedução: a quantidade de divisores é o produto dos expoentes
            // mais um, e a soma deles é o produto das séries geométricas.
            var fatores = Fatoracao.Fatorar((ulong)n);

            var quantosPelaFormula = 1;

            ulong somaPelaFormula = 1;

            foreach (var grupo in fatores.GroupBy(p => p))
            {
                var expoente = grupo.Count();

                quantosPelaFormula *= expoente + 1;

                ulong parcela = 1, potencia = 1;

                for (var i = 0; i < expoente; i++)
                {
                    potencia *= grupo.Key;

                    parcela += potencia;
                }

                somaPelaFormula *= parcela;
            }

            Assert.Equal(quantosPelaFormula, achados.Count);

            ulong soma = 0;

            foreach (var d in achados)
            {
                soma += d;
            }

            Assert.Equal(somaPelaFormula, soma);
        }
    }

    [Fact(DisplayName = "a totiente pela fatoracao contra a totiente pelo crivo")]
    public void ASTotientes()
    {
        var linear = new Linear(200_000);

        for (var n = 1; n <= 200_000; n++)
        {
            Assert.True((ulong)linear.Totiente[n] == Fatoracao.Totiente((ulong)n),
                $"discordaram em {n}");
        }

        // E a definição, por força bruta, onde ela ainda é barata: quantos
        // números de 1 a n são primos com n.
        for (var n = 1; n <= 2_000; n++)
        {
            var naMao = 0;

            for (var k = 1; k <= n; k++)
            {
                if (Fatoracao.Mdc((ulong)k, (ulong)n) == 1)
                {
                    naMao++;
                }
            }

            Assert.Equal((ulong)naMao, Fatoracao.Totiente((ulong)n));
        }

        // phi(1) = 1: o único número de 1 a 1 é primo com 1. É a convenção que
        // faz a soma sobre divisores fechar.
        Assert.Equal(1UL, Fatoracao.Totiente(1));
    }

    [Fact(DisplayName = "o maximo divisor comum, contra a definicao")]
    public void OMdc()
    {
        var sorteio = new Random(13);

        for (var caso = 0; caso < 5_000; caso++)
        {
            var a = (ulong)sorteio.Next(0, 3_000);

            var b = (ulong)sorteio.Next(0, 3_000);

            var d = Fatoracao.Mdc(a, b);

            if (a == 0 && b == 0)
            {
                Assert.Equal(0UL, d);

                continue;
            }

            // Divide os dois.
            Assert.True(a % d == 0 && b % d == 0);

            // E é o maior que divide os dois.
            for (var maior = d + 1; maior <= Math.Max(a, b); maior++)
            {
                Assert.False(a % maior == 0 && b % maior == 0,
                    $"{maior} divide {a} e {b}, e o mdc deu {d}");
            }
        }
    }
}
