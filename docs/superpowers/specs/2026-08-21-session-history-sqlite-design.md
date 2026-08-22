# Histórico SQLite e retomada de sessão

Data: 2026-08-21  
Status: aprovado

## Objetivo

Persistir cada fase do Pomodoro em SQLite local e permitir retomar uma sessão interrompida pelo Exit. O timer compacto permanece; um painel ao lado mostra histórico agrupado por dia.

## Fora de escopo

- Sincronização na nuvem, exportação, edição ou exclusão de registros
- Gráficos, sequências (streaks) ou calendário
- EF Core
- Tratar minimizar para a bandeja como interrupção (o processo continua vivo)
- Estatísticas na janela compacta

## Arquitetura

O `PomodoroTimerService` continua a fonte de verdade do relógio. Um `SessionHistoryService` escuta eventos do timer e o Exit; não atualiza UI.

Arquivo: `pomodoro.db` em `ApplicationData.Current.LocalFolder` (mesmo diretório de `settings.json`). Pacote: `Microsoft.Data.Sqlite`. Schema via `PRAGMA user_version = 1`.

`settings.json` não muda. O painel de histórico só lê `phase_logs`.

```
Timer  --Checkpoint / PhaseTransitioned-->  SessionHistoryService
                                                 |
                                                 v
                                          IHistoryStore (SQLite)
                                           phase_logs
                                           session_state
                                                 ^
                                                 |
                                          HistoryViewModel (somente leitura)
```

## Schema

### `phase_logs`

| Coluna | Tipo | Notas |
| --- | --- | --- |
| `id` | INTEGER PK | |
| `phase` | TEXT | `Focus`, `ShortBreak`, `LongBreak` |
| `cycle` | INTEGER | Ciclo no momento do início |
| `started_at` | TEXT | UTC, round-trip ISO-8601 |
| `ended_at` | TEXT NULL | UTC; null enquanto `InProgress` |
| `planned_duration_ms` | INTEGER | Duração configurada no início |
| `elapsed_ms` | INTEGER | Tempo realmente feito |
| `outcome` | TEXT | `InProgress`, `Completed`, `Skipped`, `Interrupted` |

Índice: `(started_at)` para agrupamento por dia.

### `session_state`

Uma linha (`id = 1`):

- `phase`, `cycle`, `remaining_ms`, `total_duration_ms`, `is_running`, `is_paused`, `updated_at` (UTC)

Não persiste a cada tick. Enquanto `IsRunning`, grava um checkpoint no máximo a cada 15s (além dos eventos). `remaining_ms` é o restante congelado no último checkpoint — tempo com o processo morto não conta.

## Regras de gravação

`elapsed_ms` usa o restante do timer: `planned - remaining`, mínimo 0, máximo `planned`. Fase concluída no tempo: `elapsed_ms = planned_duration_ms`.

| Ação | `phase_logs` | `session_state` |
| --- | --- | --- |
| Start a partir de idle | Abre linha `InProgress` | Grava |
| Pause / Resume | Sem linha nova | Grava restante congelado |
| Tick | Nada | Nada (exceto checkpoint a cada 15s se rodando) |
| Término no tempo | Fecha `Completed` | Grava |
| Skip | Fecha `Skipped` | Grava |
| Restart da fase | Fecha `Interrupted`; se ainda rodando/pausado, abre nova `InProgress` da mesma fase | Grava |
| Reset do ciclo | Fecha `Interrupted` se havia fase ativa; não abre linha nova | Idle, Foco, ciclo 1 |
| Auto-start da próxima fase | Fecha a anterior; `Start` abre a próxima | Grava |
| Sem auto-start | Fecha a anterior; próxima linha só no Start | Idle |
| Hide para bandeja | Nada extra | Nada extra |
| Exit com fase rodando ou pausada | Linha permanece `InProgress` | Congela `remaining_ms` (tempo com o app fechado **não** conta) |
| Exit idle | Sem prompt na próxima abertura | Snapshot idle (`is_running` e `is_paused` falsos) |

No máximo uma linha `InProgress`. Se o store encontrar mais de uma na abertura, fecha as mais antigas como `Interrupted` e mantém a mais recente.

## Retomada na abertura

Depois de mostrar a janela principal, se existir snapshot retomável **e** uma linha `InProgress`:

- Retomável = (`is_running` ou `is_paused`) e `remaining_ms > 0`
- Snapshot inválido (fase desconhecida, restante negativo, log ausente) → mesmo fluxo de “começar de novo”, sem diálogo

Diálogo no estilo de `CloseChoiceWindow`, texto: “Continuar 12:40 restantes?” (restante formatado como no timer) com ações Continuar e Começar de novo.

1. **Continuar** — `timer.Restore(snapshot)`. Tempo parado enquanto o processo esteve morto. Se `is_running` era true, volta a contar a partir de agora + restante. A mesma linha `InProgress` permanece.
2. **Começar de novo** — fecha o log como `Interrupted` com o `elapsed_ms` do snapshot; `ResetCycle()` (Foco, ciclo 1, idle).

## Eventos do timer

`PhaseTransition` ganha `PhaseEndReason`: `Completed`, `Skipped`, `Interrupted`.

`PhaseTransitioned` passa a disparar também em Restart e ResetCycle (quando havia fase ativa), com `Interrupted`.

Novo evento `Checkpoint` (não no tick): Start, Pause, Resume, Restart, ResetCycle, Skip, Complete. O histórico ainda persiste `session_state` no máximo a cada 15s enquanto rodando, sem depender desse evento.

`Restore(PomodoroSession state)` aplica fase, ciclo, restante, duração total, flags; se `IsRunning`, inicia o ticker com `EndTime = agora + remaining`.

`NotificationService` e `SoundService` **ignoram** `Interrupted` (hoje já reagem a skip; isso permanece).

## Componentes

| Unidade | Função | Depende de |
| --- | --- | --- |
| `PhaseOutcome`, `PhaseLog`, `SessionSnapshot` | Dados | — |
| `IHistoryStore` / `SqliteHistoryStore` | SQL, schema, CRUD | arquivo `.db` |
| `SessionHistoryService` | Mapeia Checkpoint/transição/Exit → store | timer, store |
| `DailyHistory` (puro) | Agrupa logs por **data local** | lista de `PhaseLog` |
| `HistoryPanelLayout` (puro) | Posição do painel | `WindowLayout` |
| `HistoryViewModel` | Resumo + lista | store |
| `HistoryPanelWindow` | Painel ao lado do compacto | view model |
| `ContinueChoiceWindow` | Continuar vs recomeçar | — |

`SqliteHistoryStore` é o único tipo que importa `Microsoft.Data.Sqlite`. Testes do store usam arquivo temporário.

`SessionHistoryService` registra-se no DI como singleton e dá `Attach()` em `OnLaunched`, no mesmo momento que som e notificação.

## UI do histórico

Ícone na barra do compacto abre/fecha o painel. Tamanho padrão: 280×400 DIPs. Preferência: colado à **direita** do timer, folga de 8 px. Se não couber na área de trabalho, à **esquerda**. O painel acompanha o arrasto da janela principal. Fecha junto no Exit.

Conteúdo: lista rolável agrupada por dia local, mais recente primeiro. Sem seletor de calendário nesta versão.

- **Cabeçalho do dia:** focos **concluídos** + soma de `elapsed_ms` só desses focos.
- **Linhas:** hora local de `started_at`, nome da fase, tempo feito (`elapsed` ou restante se em andamento) e rótulo do resultado.

Tema e always-on-top seguem `PomodoroSettings`, como Configurações.

## Erros

Falha de I/O no SQLite não derruba o timer: histórico pode ficar vazio ou atrasado; o painel mostra estado vazio.

Arquivo corrompido ou schema ilegível: renomeia para `pomodoro.db.bak` (substitui backup anterior) e cria banco novo. Sem prompt de retomada nessa abertura.

Exit: tenta um último checkpoint e fecha mesmo se a gravação falhar.

Escritas `async`; o serviço captura exceções de I/O. Um único processo, uma conexão, sem write a cada segundo.

## Testes (xUnit, sem UI WinUI)

O projeto de testes continua linkando arquivos portáteis; novos tipos de domínio/store/serviço entram nesse padrão. `Microsoft.Data.Sqlite` nas duas csproj.

1. **SqliteHistoryStore** — schema em arquivo novo; abrir/fechar fase; snapshot round-trip; consulta por intervalo; recuperação de arquivo corrompido (`.bak` + banco novo).
2. **SessionHistoryService** + store fake — Start abre log; complete/skip/interrupt fecham com o motivo certo; Exit mantém `InProgress`; Continuar restaura a mesma linha; Começar de novo interrompe e reseta; Checkpoint não ocorre em tick.
3. **PomodoroTimerService** — `Restore` recoloca fase/ciclo/restante e retoma o ticker; skip vs término emitem razões diferentes; restart/reset emitem `Interrupted`; `Checkpoint` não dispara no tick.
4. **DailyHistory** — agrupa por data local (não UTC); resumo conta só Focus `Completed`.
5. **HistoryPanelLayout** — prefere direita; usa esquerda se a direita ultrapassar a área de trabalho.

## Critério de pronto

- Fechar e reabrir o app no meio de um foco pergunta Continuar / Começar de novo, e as duas opções se comportam como acima.
- Histórico ao lado lista fases do dia com resumo de focos concluídos.
- Skip, restart, reset e Exit (com “começar de novo”) aparecem com o resultado correto.
- Timer funciona se o `.db` estiver inacessível ou corrompido.
- Testes novos cobrindo store, serviço, restore, agrupamento e layout passam.
