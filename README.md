# primos

Crivos, teste de primalidade e fatoração do zero em C# e .NET 8, conferidos
contra catálogos publicados há mais de um século.

```
$ dotnet medidor.dll bilhao

  Contando os primos ate 1,000,000,000 em blocos de 10,000,000,
  com o crivo segmentado. A memoria nao cresce com o limite.

    achados           50,847,534
    catalogado        50,847,534
    tempo                  9.1 s
```

## Quatro juízes, e três deles são mais velhos que o computador

A quantidade de primos até cada potência de dez está catalogada desde Legendre e
Gauss. As maiores lacunas entre primos consecutivos, e o primo exato em que cada
recorde aparece, estão catalogadas desde o século XIX. A lista dos menores
compostos que enganam o Miller-Rabin com as primeiras `k` bases primas é de 1980
e 1993. Nenhum desses números saiu daqui, e qualquer defeito aparece na primeira
casa.

O quarto juiz é a divisão por tentativa, que não tem limite de tabela, é lenta
demais para qualquer uso e óbvia demais para errar. Ela confere todo número até
300.000, um por um.

Depois disso ainda há dois crivos que se conferem: Eratóstenes risca cada
composto uma vez por fator primo dele, o crivo linear risca cada composto
exatamente uma vez pelo menor fator. Os códigos não se parecem, e eles concordam
em todos os 78.498 primos até um milhão.

São 19 testes.

## As doze bases são mesmo necessárias?

O Miller-Rabin é apresentado como probabilístico, e é, enquanto as bases são
sorteadas. Com os doze primeiros primos como base ele vira uma prova para todo
número de 64 bits, porque está demonstrado que o menor composto que passa por
todos os doze tem 24 dígitos.

A pergunta que nenhum teste aleatório responde é se alguém copiou uma lista maior
do que precisava. O catálogo A014233 responde: ele dá o menor composto que engana
as primeiras `k` bases, para cada `k`.

```
       k   menor composto que engana
  ---------------------------------------------
       1                         2.047
       2                     1.373.653
       3                    25.326.001
       4                 3.215.031.751
       5             2.152.302.898.747
       6             3.474.749.660.383
       8           341.550.071.728.321
      11     3.825.123.056.546.413.051
```

O teste confere, para cada linha, que o número **passa** com `k` bases e **cai**
com `k + 1`. O último tem dezenove dígitos e sobrevive a onze bases. A décima
segunda é o que separa "provavelmente primo" de "primo", e o contraexemplo dela
não cabe no tipo.

## O buraco do teste de Fermat, contado em vez de explicado

Todo texto diz que o teste de Fermat erra e o de Miller-Rabin não. Aqui isso é um
número:

Abaixo de um milhão há **245** compostos que passam no teste de Fermat na base 2.
Desses, só **46** ainda passam no Miller-Rabin com a mesma base. Acompanhar os
quadrados sucessivos, em vez de olhar apenas o destino, derruba 199 dos 245
impostores sem gastar uma base a mais.

E **43** deles são números de Carmichael, que passam no teste de Fermat em toda
base prima com eles. Para esses, nenhuma quantidade de bases salva o teste de
Fermat. Um teste confere isso por força bruta no menor deles: as 319 bases primas
com 561 passam, todas.

Korselt publicou em 1899 o critério que reconhece esses números sem conhecer um
só exemplo. O primeiro, 561, só apareceu dezessete anos depois.

## Fatorar custa a raiz do fator, e não a raiz do número

```
$ dotnet medidor.dll fatorar

   bits do fator   bits de n    ro de Pollard   divisao por tentativa
  ------------------------------------------------------------------------
            10          32          0.07 ms              0.1 ms
            14          34          0.13 ms              0.1 ms
            18          38          0.23 ms              0.9 ms
            22          45          1.44 ms             13.7 ms
            26          53          1.20 ms            245.8 ms
            30          61          8.43 ms         (desistiu)
            31          63          7.64 ms         (desistiu)
```

O rô de Pollard nunca procura o fator: ele espera tropeçar. A sequência
`x := x² + c` módulo `n` repete depois de uns `raiz de n` passos, mas módulo `p`
ela repete depois de uns `raiz de p`, e quando dois valores coincidem módulo `p`
sem coincidir módulo `n`, a diferença entre eles é um múltiplo de `p` que o máximo
divisor comum entrega.

A coluna dele não é suave de propósito. Dobrar o tamanho do fator multiplica o
tempo esperado por `raiz de 2`, só que a variação entre dois números do mesmo
tamanho é maior que isso, e por isso cada linha é a média de cinco números
diferentes.

O juiz da fatoração está de graça: multiplicar de volta custa microssegundos.
Mas reconstruir o número não basta, porque `12 = 4 · 3` reconstrói e está errado.
A segunda metade é testar cada fator, e aí a resposta é única pelo teorema
fundamental da aritmética.

## Duas identidades que não comparam com implementação nenhuma

A soma de `phi(d)` sobre todos os divisores de `n` dá exatamente `n`. A soma de
`mu(d)` sobre os divisores de `n` dá 1 no 1 e zero no resto.

As duas são conferidas para todo `n` até um milhão. Não há nada para comparar: é
a aritmética que tem de fechar. E elas são sensíveis, porque errar a totiente de
um único número quebra a soma de todos os múltiplos dele.

## Quanto o teorema dos números primos erra

```
$ dotnet medidor.dll contar

           x        pi(x)        x/ln x    erro         li(x)      erro
  ----------------------------------------------------------------------------
       1,000          168           145  -13.83%           176  +4.7324%
      10,000        1,229         1,086  -11.66%         1,241  +0.9907%
     100,000        9,592         8,686  -9.45%         9,617  +0.2650%
   1,000,000       78,498        72,382  -7.79%        78,592  +0.1203%
  10,000,000      664,579       620,421  -6.64%       664,799  +0.0331%
  100,000,000    5,761,455     5,428,681  -5.78%     5,761,727  +0.0047%
```

O `x/ln x` é o enunciado do teorema, e a aproximação melhora devagar: ainda erra
5,8% em cem milhões. O logaritmo integral é a mesma conta com os termos seguintes
da série, e a partir de dez mil ele erra na quarta casa. Abaixo disso é um
desastre, e não há nada de errado nisso: a série é assintótica, então só promete
alguma coisa quando `x` é grande.

Gauss propôs o logaritmo integral em 1792, com quinze anos de idade.

## Goldbach, e o que um teste de conjectura faz de útil

Um dos testes procura um par até um milhão que não seja soma de dois primos. Ele
não acha, e não achar não prova nada: a conjectura está aberta desde 1742 e
conferida por computador até 4 · 10¹⁸.

O que ele de fato garante é outra coisa, e essa vale: se o crivo estivesse errado
em qualquer número, a decomposição falharia em algum par. Uma conjectura cara de
testar vira um teste de regressão barato.

O número que surpreende é outro. O **menor** primo que serve nunca passa de 523
abaixo de um milhão, e o pior caso é o 503.222. Eu tinha escrito 389 nesse
assert, de cabeça, e o teste corrigiu antes de o commit existir.

## Rodar

.NET 8. Zero dependências fora do xUnit, e só nos testes.

```
dotnet test testes/Primos.Testes/Primos.Testes.csproj -c Release
dotnet run --project ferramentas/Medidor/Medidor.csproj -c Release -- fatorar
```

## O que ele não faz

Não tem crivo de Atkin, que é assintoticamente melhor e perde na prática para o
de Eratóstenes com roda de 2, 3 e 5.

Não tem roda nenhuma, aliás. Pular os múltiplos de 3 e de 5 além dos de 2
economizaria mais de metade do trabalho restante, ao preço de um índice que
ninguém lê sem apanhar.

Não tem curva elíptica nem crivo quadrático. O rô de Pollard dá conta de 64 bits
e desiste bem antes dos tamanhos que a criptografia usa, que é o ponto: fatorar é
difícil, e esta é a demonstração barata disso.

Não conta primos por fórmula. Legendre, Meissel e Lehmer sabem contar `pi(x)` sem
listar os primos, e é assim que se chega a `10²⁷`. Aqui tudo é peneirado de fato.

Não é thread-safe, embora o crivo segmentado praticamente peça para ser paralelo.

## Licença

MIT.
