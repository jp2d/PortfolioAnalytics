# Portfolio Analytics API

WebAPI em .NET 8 para análise de performance, risco e rebalanceamento de portfólios de investimento.

## Como executar

```bash
dotnet run --project PortfolioAnalytics.csproj
```

O Swagger abre automaticamente em `https://localhost:xxxx/swagger` (ambiente de desenvolvimento). Os dados são carregados automaticamente do `SeedData.json` no startup, em um banco EF Core InMemory.

## Como testar

```bash
dotnet test
```

**28 testes** no total, todos passando:
- **22 testes unitários** cobrindo os 3 services isolados (`PerformanceCalculator`, `RiskAnalyzer`, `RebalancingOptimizer`), incluindo casos de fronteira das regras de classificação de risco (15%/25%/40%) e da otimização de quantidade em rebalanceamento.
- **6 testes de integração** (`WebApplicationFactory`), subindo a API completa em memória e validando os 3 endpoints via HTTP real, incluindo os casos de `404`.

## Portfólios disponíveis (IDs)

⚠️ O seed não possui um campo `id` dedicado para portfólios — usamos o campo `userId`, já único e visível no próprio `SeedData.json`, como identificador da rota.

| Id | Nome | Perfil |
|---|---|---|
| `user-001` | Portfólio Conservador | Boa diversificação, mas concentração no setor Financial |
| `user-002` | Portfólio Crescimento | Alta concentração em posições individuais (RENT3, TOTS3) |
| `user-003` | Portfólio Dividendos | Alta concentração em ITUB4 e no setor Financial; precisa de rebalanceamento |

## Endpoints

- `GET /api/portfolios/{id}/performance`
- `GET /api/portfolios/{id}/risk-analysis`
- `GET /api/portfolios/{id}/rebalancing`

Retornam `404` se o `id` não existir.

## Arquitetura

```
PortfolioAnalytics/
├── Controllers/
│   └── AnalyticsController.cs         — fino, delega tudo aos services, mapeia DTOs
├── Service/
│   ├── Interface/
│   │   ├── IClock.cs
│   │   ├── IMarketDataProvider.cs
│   │   ├── IPerformanceCalculator.cs
│   │   ├── IRebalancingOptimizer.cs
│   │   └── IRiskAnalyzer.cs
│   ├── FinancialMath.cs               — retornos diários, desvio-padrão, anualização (funções puras, reaproveitadas)
│   ├── AnalyticsRules.cs              — limiares de negócio centralizados (evita números mágicos espalhados)
│   ├── PerformanceCalculator.cs       — endpoint 1; base para os outros dois
│   ├── RiskAnalyzer.cs                — reaproveita IPerformanceCalculator (Sharpe usa retorno anualizado + volatilidade)
│   ├── RebalancingOptimizer.cs        — reaproveita IPerformanceCalculator (pesos atuais); otimização floor/ceil (ver abaixo)
│   ├── MarketDataProvider.cs
│   ├── SeedReferenceClock.cs          — data de referência fixa (ver "Determinismo" abaixo)
│   ├── PerformanceMetrics.cs / PositionPerformanceMetric.cs   — modelos internos de resultado (não são os DTOs de resposta)
│   ├── RiskMetrics.cs / SectorExposure.cs / PositionConcentration.cs
│   └── RebalancingMetrics.cs / AllocationItem.cs / SuggestedTrade.cs
├── Models/                            — entidades que mapeiam o seed (Asset, Portfolio, Position, PricePoint, MarketData, IndexPerformance)
│   ├── DTO/                           — contratos de resposta da API (Performance/Risk/RebalancingResponseDto, etc.)
│   └── Enum/                          — RiskLevel.cs, TradeAction.cs
├── Data/
│   ├── DataContext.cs                 — EF Core InMemory (owned types para Positions/PriceHistory)
│   └── DataSeeder.cs                  — carga do SeedData.json
├── Program.cs
├── appsettings.json
└── SeedData.json

Tests/
├── Helpers/
│   ├── FakeClock.cs                   — implementação de IClock com data controlada pelo teste
│   └── FakePerformanceCalculator.cs   — dublê de IPerformanceCalculator para isolar Risk/Rebalancing
├── Services/                          — 22 testes unitários, services testados isoladamente via fakes
│   ├── PerformanceCalculatorTests.cs
│   ├── RiskAnalyzerTests.cs
│   └── RebalancingOptimizerTests.cs
├── Integration/                       — 6 testes de integração via WebApplicationFactory<Program>
│   ├── CustomWebApplicationFactory.cs
│   └── AnalyticsControllerIntegrationTests.cs
└── SeedData.json                      — linkado do projeto principal (Include + Link no .csproj)
```

⚠️ Nomenclatura: o namespace raiz dos services é `PortfolioAnalytics.Service` (singular) e das interfaces `PortfolioAnalytics.Service.Interface`; os DTOs ficam em `PortfolioAnalytics.Models.DTO` (singular) e os enums em `PortfolioAnalytics.Models.Enum`. Mantido assim por consistência com o que foi efetivamente criado durante a implementação.

### Decisões técnicas

- **`decimal` para dinheiro, `double` para estatística.** Conversão só na fronteira do cálculo estatístico (`Math.Pow`/`Math.Sqrt` não existem para `decimal`); arredondamento só no DTO de saída, nunca em cálculo intermediário.
- **Services puros, sem dependência de `DbContext`.** Recebem `Portfolio` + dicionário de `Asset` como parâmetros. Isso é o que permite testar os 3 services inteiramente em memória, sem mock de banco.
- **`RiskAnalyzer` e `RebalancingOptimizer` dependem de `IPerformanceCalculator` (interface), não da classe concreta.** Nos testes desses dois services, usamos um `FakePerformanceCalculator` que devolve métricas controladas, isolando cada teste da lógica de cálculo de performance em si.
- **Data de referência fixa via `IClock`, não `DateTime.UtcNow`.** O retorno anualizado depende dos dias decorridos desde `createdAt`; se usássemos o relógio real da máquina, o resultado mudaria a cada execução e os testes ficariam não-determinísticos. Fixamos `2024-10-06` (data de `lastUpdated` dos assets no seed) como "hoje" do sistema.
- **Nome do banco InMemory parametrizável via configuração** (`InMemoryDbName`, com fallback para `"PortfolioAnalyticsDb"`). Isso permite que os testes de integração usem um banco isolado por execução (`TestDb_{Guid}`) sem duplicar a lógica de registro do `DbContext` nem o bloco de seed — o `Program.cs` de produção e o de teste executam exatamente o mesmo caminho de código.
- **`Id` do portfólio = campo `userId` do seed** (ver seção acima — não existe campo `id` nos dados fornecidos).
- **Limiares de negócio centralizados em `AnalyticsRules`** (25%/15% para posição, 40%/25% para setor, 2% de desvio mínimo para rebalanceamento, R$100 de trade mínimo, 0,3% de custo de transação).
- **O que decidimos não fazer**: Clean Architecture multi-projeto, MediatR, AutoMapper, repositório genérico. Para o escopo de 3-4h e 3 endpoints somente-leitura, essas camadas adicionariam cerimônia sem ganho de testabilidade ou clareza mensurável.

## Fórmulas financeiras

| Métrica | Fórmula | Observação |
|---|---|---|
| Total Return | `(currentValue − totalInvestment) / totalInvestment × 100` | `totalInvestment ≤ 0` → `null` |
| Annualized Return | `((1 + totalReturn_fração)^(365/dias) − 1) × 100` | `dias = referenceDate − portfolio.createdAt`; base sempre em **fração**, nunca percentual (armadilha comum: usar `1.085^n` em vez de `1 + 0.085`) |
| Volatility | Desvio-padrão amostral (`n−1`) dos retornos diários da série agregada do portfólio, anualizado por `× √252` | Série agregada = `Σ quantity_i × preço_i,t`, considerando só posições com histórico disponível (cobertura parcial é logada) |
| Sharpe Ratio | `(annualizedReturn − selicRate×100) / volatility` | `volatility` nula ou 0 → `null` |
| Concentration Risk | Maior peso individual; soma dos 3 maiores pesos | Peso = `currentValue_i / currentValue_total × 100` |
| Sector Diversification | Soma dos pesos das posições agrupadas por `Asset.Sector` | — |
| Rebalancing — desvio | `currentWeight − targetWeight` | Considerado apenas se `|desvio| > 2%` |
| Rebalancing — quantidade | Escolhida entre `floor` e `ceil(|Δvalor| / currentPrice)`, priorizando a opção que resulta em **menor desvio residual pós-trade** | Respeita o mínimo de R$100 e nunca ultrapassa a tolerância de 2% no sentido oposto ao alvo (evita "overshoot"). Ver "Otimização do Rebalanceamento" abaixo |
| Rebalancing — custo | `estimatedValue × 0,3%` | — |
| Expected Improvement | Redução percentual na concentração das 3 maiores posições, simulando os trades sugeridos | Escolhida por ser objetiva e simulável sem reimplementar o `RiskAnalyzer` inteiro |

### Regras de classificação de risco

| Nível | Posição individual | Setor |
|---|---|---|
| Alto | > 25% | > 40% |
| Médio | 15% – 25% (25% exato = Médio) | 25% – 40% (40% exato = Médio) |
| Baixo | < 15% | < 25% |

`overallRisk` do portfólio = o pior nível encontrado entre todas as posições e setores.

### Otimização do Rebalanceamento (diferencial)

O arredondamento ingênuo de quantidade (sempre `floor`) tem dois problemas: sub-corrige o desvio sistematicamente, e descarta trades legítimos que ficam abaixo do valor mínimo de R$100 só por arredondar para baixo — mesmo quando arredondar para cima resolveria isso com um desvio residual menor.

A implementação atual calcula **as duas opções** (`floor` e `ceil` da quantidade bruta), simula o peso resultante de cada uma e escolhe a que deixa o menor `|pesoFinal − pesoAlvo|`, com uma trava de segurança que rejeita a opção `ceil` caso ela "ultrapasse" o alvo para o lado oposto além da tolerância de 2% (evitando comprar/vender demais na tentativa de arredondar).

## Premissas adotadas

- **`totalInvestment` do seed é sistematicamente maior que a soma de `quantity × averagePrice` das posições**, nos 3 portfólios (gaps de 23% a 37% do capital declarado). Tratamos como caixa não alocado / capital fora das posições listadas e usamos o campo `totalInvestment` como fonte da verdade — é o valor semanticamente mais próximo de "capital total investido pelo cliente". O gap é logado como aviso (`LogWarning`) quando detectado.
- **`TargetAllocation` que não soma 100%** é normalizada proporcionalmente (`target_i / Σtargets`), com log de aviso. Nos 3 portfólios principais do seed a soma já é 100%; a normalização foi validada via teste unitário dedicado com dados sintéticos.
- **Sem histórico de preços → `volatility = null`** (conforme resposta do FAQ do enunciado).
- **Quantidades de ações sugeridas em rebalanceamento são inteiras**, escolhidas entre `floor` e `ceil` pelo critério de menor desvio residual (ver seção de otimização acima) — não pelo `floor` puro. Isso significa que a soma de vendas e compras sugeridas não necessariamente se cancela de forma exata; o resíduo é aceito e esperado.
- **Preço atual inválido (`≤ 0`)**: fallback para o último preço do histórico disponível e, na ausência deste, para `AveragePrice`. Situação logada como aviso.
- **Data de referência fixa (`2024-10-06`)** para todos os cálculos dependentes de "hoje" (ver `IClock` acima), garantindo resultados determinísticos e reprodutíveis entre execuções — inclusive nos testes de integração, que sobem a API real.

## Edge cases tratados

- Portfólio inexistente → `404` em todos os 3 endpoints (validado tanto em testes unitários quanto de integração).
- `totalInvestment ≤ 0` → `totalReturn`/`annualizedReturn` = `null`.
- `AveragePrice ≤ 0` em uma posição → `return` daquela posição = `null`.
- Histórico de preços ausente ou com menos de 2 pontos → `volatility = null`.
- Preço zero/ausente dentro do histórico → par de retorno correspondente é ignorado no cálculo (evita divisão por zero).
- Símbolo de posição sem `Asset` correspondente no dicionário → posição excluída do cálculo, com aviso logado.
- `CurrentPrice` inválido → fallback (histórico → `AveragePrice`), com aviso logado.
- `volatility = 0` ou `null` → `sharpeRatio = null`.
- `TargetAllocation` somando ≠ 100% → normalização proporcional, com aviso logado.
- Trade sugerido com valor estimado abaixo de R$100 → só descartado se **nem `floor` nem `ceil`** da quantidade atingirem o mínimo.
- Arredondamento de quantidade que "ultrapassaria" o alvo para o lado oposto além da tolerância de 2% → opção rejeitada, mantém a mais conservadora.

## Sobre o dataset de teste

Os 3 portfólios do seed classificam como risco **`High`** nas regras dadas (nenhum cai naturalmente em `Medium` ou `Low`) — característica do dataset fornecido, não limitação da implementação. Os ramos `Medium`/`Low` das fronteiras de classificação (15%, 25%, 40%) são cobertos explicitamente por testes unitários com fixtures dedicadas.

O arquivo `SeedData.json` também inclui uma seção `testScenarios` com resultados esperados; conferimos manualmente e os valores não reproduzem exatamente com os preços atuais dos assets no seed (parecem calculados sobre outro snapshot de preços). Por isso, usamos esses cenários apenas como checagem direcional (ex.: "deveria acusar risco alto") e não como asserts de igualdade exata — os testes reais usam fixtures com valores calculados à mão.