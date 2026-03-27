<p align="center">
  <img src="./assets/banner.png" alt="Banner do FF Boost"/>
</p>

<h1 align="center">FF Boost</h1>

<p align="center">
  Otimizador gamer para <b>BlueStacks + Free Fire</b>, criado para reduzir a carga do sistema,
  melhorar a resposta do emulador e automatizar o ciclo de otimização e restauração durante a jogatina.
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10-512BD4?style=for-the-badge" alt=".NET 10"/>
  <img src="https://img.shields.io/badge/WinForms-Windows-0078D6?style=for-the-badge" alt="WinForms Windows"/>
  <img src="https://img.shields.io/badge/Platform-Win--x64-111827?style=for-the-badge" alt="Win x64"/>
  <img src="https://img.shields.io/badge/Release-4.3.0-FF6C48?style=for-the-badge" alt="Release 4.3.0"/>
</p>

<p align="center">
  <b>Assinatura visual:</b> 文Ｉｌｕｓｉｏｎ
</p>

<p align="center">
  <a href="https://github.com/JacksonDeLima/FFboost/releases/latest">
    <img src="https://img.shields.io/badge/Ver%20Release-GitHub-FF6C48?style=for-the-badge" alt="Ver release no GitHub"/>
  </a>
  <a href="https://github.com/JacksonDeLima/FFboost/releases/download/v4.3/FFBoost-Setup.exe">
    <img src="https://img.shields.io/badge/Baixar%20Instalador-FFBoost-00E5FF?style=for-the-badge" alt="Baixar instalador do FF Boost"/>
  </a>
  <a href="https://github.com/JacksonDeLima/FFboost/releases/download/v4.3/FFBoost.exe">
    <img src="https://img.shields.io/badge/Baixar%20Executavel-FFBoost-111827?style=for-the-badge" alt="Baixar executável do FF Boost"/>
  </a>
</p>

---

## Visão Geral

`FF Boost` é um aplicativo desktop para Windows desenvolvido com `C#`, `.NET 10` e `WinForms`.
Ele foca nas otimizações de sistema que realmente importam em sessões com emulador:

- reduzir carga de processos em segundo plano
- suspender ou encerrar processos selecionados
- elevar a prioridade do emulador
- aplicar ajuste de afinidade de CPU
- alternar para plano de energia de alto desempenho
- restaurar o sistema com segurança ao final da sessão

O app foi pensado para uso diário com BlueStacks e segue um fluxo de automação com foco na bandeja do sistema.

---

## Estrutura Da Release Final

Este repositório está organizado em torno de uma única release ativa.

Os artefatos finais distribuíveis ficam em:

- [`dist/current/FFBoost.exe`](./dist/current/FFBoost.exe)
- [`dist/current/FFBoost-Setup.exe`](./dist/current/FFBoost-Setup.exe)
- [`dist/current/config.json`](./dist/current/config.json)

Para GitHub, a abordagem recomendada é:

- manter o código-fonte no repositório
- manter apenas o pacote distribuível atual em `dist/current`
- anexar os binários finais no GitHub Releases

---

## Principais Recursos

- perfis `Auto`, `Seguro`, `Forte` e `Ultra`
- preset dedicado `Free Fire + BlueStacks`
- allowlist e blacklist controladas por `config.json`
- lista automática de processos em execução
- priorização do emulador
- ajuste de afinidade de CPU para processos do emulador
- timer resolution para menor latência
- detecção de overlays conhecidos
- watcher do BlueStacks para abertura e fechamento
- log visual na tela
- geração de log de sessão em `.txt`
- benchmark local por sessão
- recomendação automática de perfil com base no histórico local
- ícone gamer na bandeja e atalho `Ctrl+Shift+F`
- restauração de afinidade, prioridade e plano de energia
- inicialização automática com Windows na primeira execução
- inicialização minimizada na bandeja
- otimização automática ao abrir o emulador
- restauração automática ao fechar o emulador

---

## Capturas

### Tela principal

<p align="center">
  <img src="./assets/demo.gif" width="760" alt="Demonstração do FF Boost"/>
</p>

### Identidade visual

<p align="center">
  <img src="./assets/logo.png" width="180" alt="Logo do FF Boost"/>
</p>

---

## Arquitetura

```text
FFboost/
|-- FFBoost.Core/          Models, regras e serviços
|-- FFBoost.UI/            Aplicação principal WinForms
|-- FFBoost.Setup/         Instalador WinForms
|-- Installer/             Payload embutido do instalador
|-- assets/                Recursos visuais do README e da release
|-- scripts/               Scripts auxiliares
|-- dist/current/          Artefatos finais distribuíveis
|-- config.json            Arquivo principal de configuração
|-- Directory.Build.props  Metadados compartilhados do build
`-- FFBoost.sln            Solução
```

### Core

`FFBoost.Core` concentra a lógica do produto:

- leitura e normalização da configuração
- regras de risco de processos
- varredura de processos
- comportamento seguro de encerramento e suspensão
- controle de prioridade, afinidade e plano de energia
- telemetria e benchmark
- recomendação de perfil
- política de inicialização automática

Arquivos importantes:

- [`FFBoost.Core/Services/OptimizerService.cs`](./FFBoost.Core/Services/OptimizerService.cs)
- [`FFBoost.Core/Services/PerformanceManager.cs`](./FFBoost.Core/Services/PerformanceManager.cs)
- [`FFBoost.Core/Services/TelemetryService.cs`](./FFBoost.Core/Services/TelemetryService.cs)
- [`FFBoost.Core/Services/ProcessSuspendService.cs`](./FFBoost.Core/Services/ProcessSuspendService.cs)
- [`FFBoost.Core/Services/StartupService.cs`](./FFBoost.Core/Services/StartupService.cs)
- [`FFBoost.Core/Rules/ProcessRules.cs`](./FFBoost.Core/Rules/ProcessRules.cs)

### UI

`FFBoost.UI` concentra a experiência visual:

- painel principal
- tela de apps permitidos
- splash screen
- tela Sobre
- relatório técnico
- menu gamer da bandeja

Arquivos importantes:

- [`FFBoost.UI/MainForm.cs`](./FFBoost.UI/MainForm.cs)
- [`FFBoost.UI/AllowedAppsForm.cs`](./FFBoost.UI/AllowedAppsForm.cs)
- [`FFBoost.UI/TechnicalReportForm.cs`](./FFBoost.UI/TechnicalReportForm.cs)
- [`FFBoost.UI/AboutForm.cs`](./FFBoost.UI/AboutForm.cs)
- [`FFBoost.UI/Program.cs`](./FFBoost.UI/Program.cs)

---

## Fluxo Automático

A release atual foi desenhada para uso contínuo no Windows:

1. o usuário abre o app pela primeira vez
2. o app ativa a inicialização com Windows por padrão
3. no próximo login, o app inicia minimizado na bandeja
4. o watcher monitora o BlueStacks
5. quando o emulador abre, a otimização roda automaticamente
6. quando o emulador fecha, a restauração roda automaticamente

Esse fluxo ainda respeita a escolha do usuário:

- se `Iniciar com Windows` for desativado depois, o app não reativa isso sozinho

---

## Configuração

O comportamento é controlado por [`config.json`](./config.json).

Campos importantes:

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
- `EnableTurboMode`
- `AutoOptimizeOnStartup`
- `LaunchOnWindowsStartup`
- `StartupPreferenceInitialized`
- `TelemetryEnabled`

Uso recomendado:

- use `Seguro` no dia a dia
- use `Forte` ou `Ultra` para limpeza mais agressiva
- ative `EnableFreeFireMode` para o preset dedicado `Free Fire + BlueStacks`
- valide mudanças de blacklist na máquina alvo antes de usar em escala

---

## Execução Local

### Requisitos

- Windows `x64`
- `.NET 10 SDK`
- Visual Studio com `Desktop development with .NET`

### Abrir no Visual Studio

Abra:

```text
FFBoost.sln
```

Defina `FFBoost.UI` como projeto de inicialização e pressione `F5`.

### Compilar pelo terminal

```powershell
dotnet build -c Release
```

### Executar localmente

```powershell
dotnet run --project .\FFBoost.UI\FFBoost.UI.csproj
```

---

## Empacotamento

Para GitHub, o fluxo limpo de release é:

1. manter o repositório focado no código-fonte
2. manter apenas os artefatos finais atuais em `dist/current`
3. publicar `FFBoost.exe` e `FFBoost-Setup.exe` via GitHub Releases

---

## Logs E Dados Locais

Durante o uso, o app pode gerar estes artefatos locais:

- `logs/` para logs de sessão
- `telemetry/` para histórico de benchmark e recomendações
- `backups/` para backups automáticos do `config.json`
- `state/` para persistência temporária de sessão

Essas pastas são artefatos locais de execução e são ignoradas pelo Git.

---

## Segurança E Escopo

`FF Boost` é um otimizador local de sistema. Ele não altera arquivos do jogo e nunca deve atingir processos críticos do Windows.

Perfis mais agressivos ainda exigem validação real na máquina alvo.

Fluxo de teste recomendado:

1. testar sem BlueStacks aberto
2. testar com BlueStacks aberto
3. testar com Discord aberto
4. testar com navegador aberto
5. testar com gravador aberto
6. validar a restauração ao final
7. validar a inicialização minimizada com Windows
8. validar a restauração automática ao fechar o emulador

---

## Metadados Da Release

- **Produto:** FF Boost
- **Empresa:** FF Boost Studio
- **Assembly Version:** `4.3.0.0`
- **File Version:** `4.3.0.0`
- **Version:** `4.3.0`
- **InformationalVersion:** `4.3.0-gamer`

---

## Autor

Jackson De Lima  
GitHub: https://github.com/JacksonDeLima
