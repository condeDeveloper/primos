using System.Diagnostics;

using Conde.Primos;

// A ferramenta de medida. Ela REGISTRA numeros; nenhuma afirmacao de tempo mora
// na suite de testes, porque uma afirmacao de relogio falha sem que nada tenha
// piorado, basta a maquina do CI estar ocupada.

var comando = args.Length > 0 ? args[0] : "ajuda";

switch (comando)
{
    case "contar": Contar(); break;
    case "bilhao": Bilhao(); break;
    case "fatorar": Fatorar(); break;
    case "goldbach": Goldbach(); break;
    case "lacunas": Lacunas(); break;
    default:
        Console.WriteLine("uso: medidor <contar|bilhao|fatorar|goldbach|lacunas>");
        break;
}

static void Contar()
{
    Console.WriteLine();
    Console.WriteLine("  pi(x) medido, contra as duas aproximacoes classicas.");
    Console.WriteLine();
    Console.WriteLine("           x        pi(x)        x/ln x    erro         li(x)      erro");
    Console.WriteLine("  " + new string('-', 76));

    var limite = 10;

    for (var k = 1; k <= 8; k++)
    {
        var relogio = Stopwatch.StartNew();

        var crivo = new Crivo(limite);

        var tempo = relogio.Elapsed.TotalMilliseconds;

        double x = limite;

        var ln = Math.Log(x);

        var simples = x / ln;

        // A serie assintotica do logaritmo integral. Ela diverge se continuar, e
        // por isso para no quinto termo: aqui ela ja e boa demais para o
        // tamanho dos numeros em jogo.
        var li = x / ln * (1 + 1 / ln + 2 / (ln * ln) + 6 / (ln * ln * ln)
            + 24 / (ln * ln * ln * ln));

        Console.WriteLine($"  {limite,10:n0}  {crivo.Quantos,11:n0}  {simples,12:n0}"
            + $"  {(simples - crivo.Quantos) / crivo.Quantos,6:+0.00%;-0.00%}"
            + $"  {li,12:n0}  {(li - crivo.Quantos) / crivo.Quantos,8:+0.0000%;-0.0000%}"
            + $"   {tempo,7:0} ms");

        limite *= 10;
    }

    Console.WriteLine();
    Console.WriteLine("  O x/ln x e o enunciado do teorema dos numeros primos, e a aproximacao");
    Console.WriteLine("  dele melhora DEVAGAR: ainda erra 5% em cem milhoes. O logaritmo");
    Console.WriteLine("  integral e a mesma conta com os termos seguintes da serie, e a partir");
    Console.WriteLine("  de dez mil ele erra na quarta casa. Embaixo disso ele e um desastre,");
    Console.WriteLine("  e nao ha nada de errado: a serie e ASSINTOTICA, entao ela so promete");
    Console.WriteLine("  alguma coisa quando x e grande. Gauss propos o li em 1792, com quinze");
    Console.WriteLine("  anos de idade.");
    Console.WriteLine();
}

static void Bilhao()
{
    // O crivo comum de um bilhao pediria 62 MB de bits. O segmentado peneira em
    // blocos que cabem no cache e descarta cada um, entao a memoria e constante
    // e nao depende de ate onde se vai.
    const long Ate = 1_000_000_000L;

    const long Bloco = 10_000_000L;

    Console.WriteLine();
    Console.WriteLine($"  Contando os primos ate {Ate:n0} em blocos de {Bloco:n0},");
    Console.WriteLine("  com o crivo segmentado. A memoria nao cresce com o limite.");
    Console.WriteLine();

    var relogio = Stopwatch.StartNew();

    var quantos = 0L;

    for (var de = 0L; de < Ate; de += Bloco)
    {
        var segmento = Crivo.Segmento(de, Math.Min(de + Bloco - 1, Ate));

        foreach (var ehPrimo in segmento)
        {
            if (ehPrimo)
            {
                quantos++;
            }
        }
    }

    Console.WriteLine($"    achados      {quantos,15:n0}");
    Console.WriteLine($"    catalogado   {50_847_534,15:n0}");
    Console.WriteLine($"    tempo        {relogio.Elapsed.TotalSeconds,13:0.0} s");
    Console.WriteLine();
    Console.WriteLine("  O valor catalogado e de Meissel, 1885, feito a mao e com um erro de 56");
    Console.WriteLine("  para menos que so foi corrigido em 1959.");
    Console.WriteLine();
}

static void Fatorar()
{
    Console.WriteLine();
    Console.WriteLine("  Fatorar o produto de dois primos do mesmo tamanho. A divisao por");
    Console.WriteLine("  tentativa custa a raiz do NUMERO; o ro de Pollard custa a raiz do");
    Console.WriteLine("  FATOR, que e a raiz da raiz.");
    Console.WriteLine();
    Console.WriteLine("  Cada linha e a media de cinco numeros diferentes do mesmo tamanho,");
    Console.WriteLine("  porque o ro depende de sorte e um numero so nao diz nada.");
    Console.WriteLine();

    static ulong PrimoPertoDe(ulong alvo)
    {
        while (!Teste.EhPrimo(alvo))
        {
            alvo++;
        }

        return alvo;
    }

    static ulong PorTentativa(ulong n)
    {
        for (ulong d = 2; d * d <= n; d++)
        {
            if (n % d == 0)
            {
                return d;
            }
        }

        return n;
    }

    // Esquenta o JIT antes de medir qualquer coisa.
    Fatoracao.Fatorar(1_000_003UL * 1_000_033UL);

    Console.WriteLine("   bits do fator   bits de n    ro de Pollard   divisao por tentativa");
    Console.WriteLine("  " + new string('-', 72));

    foreach (var bitsDoFator in (int[])[10, 14, 18, 22, 26, 30, 31])
    {
        var doRo = 0.0;

        var daTentativa = 0.0;

        var bitsDeN = 0;

        for (var qual = 0; qual < 5; qual++)
        {
            var p = PrimoPertoDe((1UL << bitsDoFator) + (ulong)qual * 1_000);

            var q = PrimoPertoDe((1UL << bitsDoFator) + 500_000 + (ulong)qual * 7_000);

            var n = p * q;

            bitsDeN = 64 - System.Numerics.BitOperations.LeadingZeroCount(n);

            var relogio = Stopwatch.StartNew();

            var fatores = Fatoracao.Fatorar(n);

            doRo += relogio.Elapsed.TotalMilliseconds;

            if (!fatores.SequenceEqual([p, q]))
            {
                throw new InvalidOperationException($"o ro errou em {n}");
            }

            if (bitsDoFator <= 26)
            {
                relogio.Restart();

                PorTentativa(n);

                daTentativa += relogio.Elapsed.TotalMilliseconds;
            }
        }

        var tentativa = bitsDoFator <= 26
            ? $"{daTentativa / 5,14:n1} ms"
            : "      (desistiu)";

        Console.WriteLine($"  {bitsDoFator,12}   {bitsDeN,9}   {doRo / 5,11:0.00} ms   {tentativa}");
    }

    Console.WriteLine();
    Console.WriteLine("  O ro nao procura o fator, ele espera tropecar nele: a sequencia");
    Console.WriteLine("  x := x*x + c repete modulo p depois de uns raiz de p passos, e a");
    Console.WriteLine("  diferenca entre dois valores que repetiram e um multiplo de p.");
    Console.WriteLine();
    Console.WriteLine("  E por isso que a coluna dele nao e suave: dobrar o tamanho do fator");
    Console.WriteLine("  multiplica o tempo esperado por raiz de dois, mas a variancia entre");
    Console.WriteLine("  dois numeros do mesmo tamanho e maior do que isso.");
    Console.WriteLine();
}

static void Goldbach()
{
    const int Ate = 10_000_000;

    Console.WriteLine();
    Console.WriteLine($"  Todo par de 4 a {Ate:n0} e soma de dois primos?");
    Console.WriteLine();

    var relogio = Stopwatch.StartNew();

    var crivo = new Crivo(Ate);

    var primos = crivo.Lista();

    var falhas = 0;

    var piorN = 0;

    var piorPrimo = 0;

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

        if (!achou)
        {
            falhas++;
        }
    }

    Console.WriteLine($"    pares conferidos      {(Ate - 2) / 2,12:n0}");
    Console.WriteLine($"    sem decomposicao      {falhas,12:n0}");
    Console.WriteLine($"    maior primo menor     {piorPrimo,12:n0}   (em n = {piorN:n0})");
    Console.WriteLine($"    tempo                 {relogio.Elapsed.TotalSeconds,10:0.0} s");
    Console.WriteLine();
    Console.WriteLine("  O numero da terceira linha e o que surpreende: para todo par ate dez");
    Console.WriteLine("  milhoes, o MENOR primo que serve e minusculo. A conjectura de Goldbach");
    Console.WriteLine("  segue aberta desde 1742, e esta conferida por computador ate 4 x 10^18.");
    Console.WriteLine("  Nada disso e prova: e so a falta de contraexemplo.");
    Console.WriteLine();
}

static void Lacunas()
{
    const int Ate = 100_000_000;

    Console.WriteLine();
    Console.WriteLine($"  As maiores lacunas entre primos consecutivos ate {Ate:n0}.");
    Console.WriteLine();

    var relogio = Stopwatch.StartNew();

    var crivo = new Crivo(Ate);

    var anterior = 2;

    var maior = 0;

    Console.WriteLine("     depois de           lacuna");
    Console.WriteLine("  " + new string('-', 38));

    foreach (var p in crivo.Primos())
    {
        if (p == 2)
        {
            continue;
        }

        var lacuna = p - anterior;

        if (lacuna > maior)
        {
            maior = lacuna;

            Console.WriteLine($"  {anterior,14:n0}   {lacuna,14:n0}");
        }

        anterior = p;
    }

    Console.WriteLine();
    Console.WriteLine($"  tempo: {relogio.Elapsed.TotalSeconds:0.0} s");
    Console.WriteLine();
    Console.WriteLine("  A maior lacuna ate cem milhoes e de 220, depois do 47.326.693, enquanto a");
    Console.WriteLine("  media entre primos nessa altura ja passa de 18. Lacunas de tamanho");
    Console.WriteLine("  arbitrario existem e sao faceis de construir, porque n! + 2 ate n! + n nao");
    Console.WriteLine("  tem um primo sequer, mas elas aparecem muito mais tarde do que isso sugere.");
    Console.WriteLine();
}
