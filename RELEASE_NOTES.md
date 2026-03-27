# FF Boost 4.3

Release focada em transformar o app em um fluxo contínuo de uso no Windows, com inicialização automática, operação silenciosa na bandeja e um ciclo completo de otimização e restauração em sessões com BlueStacks.

## Destaques

- inicialização automática com Windows ativada na primeira execução
- abertura minimizada na bandeja
- ícone gamer na bandeja com indicador de RAM
- estilo customizado para o menu da bandeja
- otimização automática quando o BlueStacks é detectado
- restauração automática quando o emulador é fechado
- fluxo de inicialização reforçado para execução elevada
- tela `Sobre` alinhada com os metadados finais `4.3.0`
- README e checklist de QA da release atualizados

## Melhorias Técnicas

- comportamento de inicialização tratado com uma abordagem mais robusta
- watcher do emulador integrado ao fluxo automático
- configuração, interface da bandeja e comportamento em runtime sincronizados
- build final consolidada como release atual

## Metadados Do Build

- `AssemblyVersion: 4.3.0.0`
- `FileVersion: 4.3.0.0`
- `Version: 4.3.0`
- `InformationalVersion: 4.3.0-gamer`

## Artefatos

- `FFBoost.exe`
- `FFBoost-Setup.exe`

## Observações

- o app deve ser executado como administrador
- perfis mais agressivos devem ser validados na máquina alvo
- se `Iniciar com Windows` for desativado depois, o app respeita essa escolha e não ativa novamente sozinho
