# FF Boost

Otimizador de sistema para `BlueStacks + Free Fire`, desenvolvido em `C#`, `.NET 10` e `WinForms`, com foco em reduzir carga de CPU, pressão de RAM e interferência de processos em segundo plano durante a sessão de jogo.

## Visão Geral

O `FF Boost` aplica uma camada de otimização tática para o ambiente do emulador:

- detecta processos do BlueStacks
- aplica prioridade alta ao emulador
- ajusta afinidade de CPU
- ativa plano de energia de alto desempenho
- encerra ou suspende processos conforme o perfil selecionado
- detecta overlays conhecidos
- registra logs visuais e logs em arquivo
- mantém telemetria local para benchmark e recomendação automática de perfil

O projeto preserva a evolução por versões. As saídas antigas continuam no repositório para referência e testes, e a versão mais recente corrigida está separada em pasta própria.

## Stack

- `C#`
- `.NET 10`
- `WinForms`
- `Windows x64`

## Recursos Principais

### V1

- interface base com `Otimizar Agora`, `Apps Permitidos` e `Restaurar`
- leitura de `config.json`
- blacklist segura
- prioridade alta para o emulador

### V2

- log visual na tela
- restauração real de plano de energia e prioridade
- execução como administrador via `app.manifest`
- lista automática de processos em execução

### V3

- perfis `Seguro`, `Forte` e `Ultra`
- métricas de CPU e RAM antes/depois
- logs salvos em `.txt`
- auto optimize na abertura

### V4

- suspensão de processos em vez de apenas encerramento
- afinidade de CPU para o emulador
- detecção de overlays
- monitoramento do BlueStacks
- hotkey `Ctrl+Shift+F`
- tray icon
- relatório técnico

### V4.1

- ajuste fino da blacklist por perfil
- modo específico `Free Fire + BlueStacks`
- blacklists e allowlists dedicadas para esse cenário

### V4.2

- preset visual dedicado `Free Fire`
- benchmark comparativo por sessão
- recomendação automática de perfil com base no histórico local

## Estrutura

```text
FFboost/
├── FFBoost.Core/          # Regras, modelos e serviços
├── FFBoost.UI/            # Interface WinForms principal
├── FFBoost.Setup/         # Instalador WinForms
├── Installer/             # Payload do instalador
├── scripts/               # Scripts auxiliares de publicação
├── dist/                  # Executáveis publicados por versão
├── config.json            # Configuração principal
├── Directory.Build.props  # Metadados globais do build
└── FFBoost.sln            # Solução principal
```

## Arquitetura

### `FFBoost.Core`

Responsável por:

- leitura e persistência de configuração
- varredura, classificação, encerramento e suspensão de processos
- gerenciamento de plano de energia, prioridade, afinidade e timer
- detecção de overlays
- telemetria local e benchmark
- sessão de otimização e restauração

Arquivos centrais:

- [`FFBoost.Core/Services/OptimizerService.cs`](./FFBoost.Core/Services/OptimizerService.cs)
- [`FFBoost.Core/Services/PerformanceManager.cs`](./FFBoost.Core/Services/PerformanceManager.cs)
- [`FFBoost.Core/Services/ProcessSuspendService.cs`](./FFBoost.Core/Services/ProcessSuspendService.cs)
- [`FFBoost.Core/Services/TelemetryService.cs`](./FFBoost.Core/Services/TelemetryService.cs)
- [`FFBoost.Core/Rules/ProcessRules.cs`](./FFBoost.Core/Rules/ProcessRules.cs)

### `FFBoost.UI`

Responsável por:

- tela principal gamer
- gerenciamento de apps permitidos
- splash screen
- tela “Sobre”
- relatório técnico de otimização

Arquivos centrais:

- [`FFBoost.UI/MainForm.cs`](./FFBoost.UI/MainForm.cs)
- [`FFBoost.UI/AllowedAppsForm.cs`](./FFBoost.UI/AllowedAppsForm.cs)
- [`FFBoost.UI/TechnicalReportForm.cs`](./FFBoost.UI/TechnicalReportForm.cs)

### `FFBoost.Setup`

Responsável por:

- empacotar e instalar o executável publicado
- embutir `FFBoost.exe` e `config.json` como payload

## Configuração

O comportamento do app é controlado por [`config.json`](./config.json).

Entre os principais campos:

- `AllowedProcesses`
- `FreeFireAllowedProcesses`
- `SafeBlacklist`
- `StrongBlacklist`
- `UltraBlacklist`
- `FreeFireSafeBlacklist`
- `FreeFireStrongBlacklist`
- `FreeFireUltraBlacklist`
- `RecordingProcesses`
- `EmulatorProcesses`
- `SelectedProfile`
- `EnableFreeFireMode`
- `EnableWatcher`
- `EnableAffinityTuning`
- `EnableOverlayDetection`
- `EnableTimerResolution`
- `TelemetryEnabled`

## Como Executar no Visual Studio

### Pré-requisitos

- `Visual Studio` com workload `Desenvolvimento para desktop com .NET`
- `.NET 10 SDK`
- Windows `x64`

### Abrir a solução

Abra:

```text
FFBoost.sln
```

Defina `FFBoost.UI` como projeto de inicialização e pressione `F5`.

## Build por Terminal

### Compilar

```powershell
dotnet build -c Release
```

### Executar localmente

```powershell
dotnet run --project .\FFBoost.UI\FFBoost.UI.csproj
```

### Publicar o app

```powershell
dotnet publish .\FFBoost.UI\FFBoost.UI.csproj -c Release
```

### Publicar o instalador

```powershell
dotnet publish .\FFBoost.Setup\FFBoost.Setup.csproj -c Release
```

## Versão Estável Mais Recente

No estado atual do repositório, a saída mais recente com layout corrigido está em:

- app: [`dist/publish_v4_2_layout_fix/FFBoost.exe`](./dist/publish_v4_2_layout_fix/FFBoost.exe)
- instalador: [`dist/installer_v4_2_layout_fix/FFBoost-Setup.exe`](./dist/installer_v4_2_layout_fix/FFBoost-Setup.exe)

## Logs e Telemetria

O projeto salva artefatos locais para análise:

- logs de execução em `logs/`
- histórico de benchmark e recomendação em `telemetry/`
- backups automáticos do `config.json` em `backups/`

Esses dados são locais. O projeto não depende de telemetria remota para funcionamento.

## Segurança e Escopo

O `FF Boost` é um otimizador de sistema local. Ele não promete ganho fixo de FPS e não altera arquivos do jogo.

Pontos importantes:

- usa privilégios de administrador para ampliar a capacidade de otimização
- nunca deve tocar em processos críticos do Windows
- usa blacklist conservadora por padrão
- perfis agressivos exigem validação na máquina real

Antes de uso intensivo, faça testes leves com o seu ambiente:

1. sem BlueStacks aberto
2. com BlueStacks aberto
3. com Discord aberto
4. com navegador aberto
5. com gravador aberto
6. restaurando o sistema ao final

## Roadmap

Possíveis próximos passos:

- gráfico de benchmark por sessão
- aplicação automática do perfil recomendado quando houver confiança suficiente
- preset visual por jogo
- refinamento do modo `Free Fire + BlueStacks`
- assinatura digital real do executável



## Assinatura

Projeto visual e identidade:

`文Ｉｌｕｓｉｏｎ`
