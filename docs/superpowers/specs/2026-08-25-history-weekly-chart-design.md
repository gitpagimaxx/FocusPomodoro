# Gráfico semanal no histórico

Data: 2026-08-25  
Status: rascunho

## Objetivo

O painel de histórico passa a abrir num gráfico dos últimos 7 dias locais, com seletor entre focos concluídos e minutos desses focos. Clique num dia mostra as fases daquela data; Voltar retorna ao gráfico.

Substitui a lista agrupada por dia como conteúdo inicial do painel. Não altera o timer, o SQLite nem a retomada de sessão.

## Fora de escopo

- Biblioteca de gráficos (LiveCharts, ScottPlot, Win2D)
- Eixo duplo, duas séries ao mesmo tempo, ou seletor de intervalo
- Atualização ao vivo enquanto o painel permanece aberto
- Mudança de schema, `IHistoryStore`, checkpoint ou diálogo Continuar / Começar de novo
- Estatísticas na janela compacta, streaks ou calendário
- Consulta SQL por intervalo: continua `GetLogsAsync()` e filtra em memória

## Arquitetura

O store e o `DailyHistory` não mudam. O gráfico é uma derivação de leitura:

```
IHistoryStore.GetLogsAsync()
        → DailyHistory.GroupByLocalDay
        → WeeklyHistory.LastSevenDays
        → HistoryViewModel (métrica + visão gráfico/detalhe)
        → HistoryPanelWindow
```

`WeeklyHistory` é puro: recebe os grupos e a data local de “hoje”, devolve sempre 7 pontos de `hoje-6` até `hoje` (esquerda → direita). Dia sem grupo: 0 focos e 0 minutos. Focos e minutos vêm só do `DailyHistoryGroup` (focos `Completed`; `elapsed` só desses).

Nada grava no banco. `settings.json` não ganha chave nova. Tamanho do painel permanece 280×400 DIPs.

## Componentes

| Unidade | Função | Depende de |
| --- | --- | --- |
| `WeeklyHistory` | Janela de 7 dias com buracos em 0 | `DailyHistoryGroup` |
| `WeeklyHistoryPoint` | Data, contagem de focos, minutos (`TimeSpan`) | — |
| `HistoryChartMetric` | `FocusCount` ou `Minutes` | — |
| `HistoryChartPresentation` | Rótulo `dd/MM`, texto do valor, altura da barra 0–1 | ponto + métrica + máximo da semana |
| `HistoryViewModel` | `ChartPoints`, `SelectedMetric`, `IsShowingDetail`, `SelectedDay`; `SelectDay` / `BackToChart` / `SetMetric` | store, helpers |
| `HistoryPanelWindow` | Duas visões no mesmo painel | view model |

## UI

Duas visões, uma de cada vez, no painel atual:

1. **Gráfico (inicial)** — seletor Focos \| Minutos e 7 barras clicáveis. Métrica padrão: Focos. Cor das barras: `#E85D4C` (foco). Valor acima da barra: inteiro de focos (`3`) ou `mm:ss` via `PomodoroPresentation.TimeRemainingText`. Rótulo abaixo: `dd/MM`.
2. **Detalhe** — botão Voltar, cabeçalho `HistoryPresentation.DayHeader`, linhas `HistoryPresentation.Line` (mais recente primeiro). Dia sem log: cabeçalho com 0 focos e lista vazia.

Clique na barra ou no rótulo do dia abre o detalhe. Voltar restaura o gráfico **com a métrica que estava**. Trocar Focos/Minutos recalcula alturas e textos a partir dos pontos já carregados; não relê o store.

Altura da barra: `valor / máximo da semana` na métrica ativa. Valor numérico: contagem de focos, ou `TotalMinutes` do `TimeSpan` de minutos. Máximo 0 → todas as alturas 0 (sem divisão por zero).

`HistoryViewModel` expõe: `ChartPoints` (7), `SelectedMetric`, `IsShowingDetail`, `SelectedDay` (`HistoryDayItem?`). Grupos diários ficam no view model só para montar o detalhe; a lista completa deixa de ser a tela inicial.

Detalhe de dia sem logs: `HistoryDayItem` com cabeçalho de grupo vazio (`0 focos · 00:00`) e `Lines` vazio.

Tema e always-on-top seguem `PomodoroSettings`, como hoje. Sem refresh enquanto o painel fica aberto: `LoadAsync` roda na abertura (já existente em `OnRootLoaded`).

## Erros

Falha de I/O no load: o view model captura a exceção e expõe 7 pontos zerados (hoje-6 … hoje). Sem toast extra. Timer e Exit não mudam.

Banco vazio ou só logs fora da janela: gráfico com sete zeros; detalhe de qualquer dia com lista vazia.

Arquivo corrompido: o store já substitui por `.bak` e recria o banco; o gráfico vê lista vazia nessa abertura.

## Testes (xUnit, sem UI WinUI)

1. **WeeklyHistory** — com “hoje” fixo: 7 datas, inclui hoje e hoje-6; grupo ausente vira 0; grupo fora da janela some; ordem cronológica crescente; valores copiados do grupo (focos/minutos já filtrados pelo `DailyHistory`).
2. **HistoryChartPresentation** — altura 1 no maior valor; 0 quando o máximo é 0; minutos em `mm:ss`; rótulo `dd/MM`.
3. **HistoryViewModel** — load monta 7 pontos; métrica padrão Focos; trocar para Minutos muda valores sem nova leitura; selecionar um dia entra no detalhe com as linhas daquele dia; Voltar volta ao gráfico com a métrica anterior; store com erro → 7 zeros.

Não reabre testes de SQLite nem do timer.

## Critério de pronto

- Abrir o histórico mostra 7 barras dos últimos 7 dias locais (buracos em 0).
- Focos e Minutos alternam no mesmo gráfico.
- Clique no dia lista as fases; Voltar retorna ao gráfico.
- Sem dados ou com `.db` ilegível, o painel abre com a semana zerada.
- Testes novos acima passam.

## Decisões fechadas

- Contagem igual ao cabeçalho atual: focos concluídos e minutos só desses focos.
- Sempre 7 dias, incluindo hoje; dias sem sessão aparecem como 0.
- Um gráfico, seletor de métrica; a lista deixa de ser a tela inicial.
- Barras nativas em XAML; sem pacote novo.
- Sem escuta do timer com o painel aberto.
