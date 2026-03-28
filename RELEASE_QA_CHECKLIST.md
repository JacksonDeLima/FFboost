# FF Boost Checklist De QA Da Release

Release atual: `4.3.0`

## Build

- `dotnet build -c Release` concluído com `0 erros`
- aplicativo principal presente em `dist/current/`
- instalador presente em `dist/current/`

## QA Visual

- painel principal abre sem controles cortados
- texto do menu da bandeja aparece por completo
- tela `Sobre` mostra os metadados da `4.3.0`
- tela de apps permitidos acomoda todos os controles
- logo, banner e assinatura renderizam corretamente

## Fluxo Principal

- o app inicia normalmente na primeira execução
- a primeira execução ativa a inicialização com Windows automaticamente
- o app inicia minimizado na bandeja quando aberto com `--tray`
- detecção do BlueStacks funciona
- otimização automática dispara quando o BlueStacks abre
- restauração automática dispara quando o BlueStacks fecha
- `Otimizar Agora` manual funciona
- `Restaurar` manual funciona

## Perfis

- `Auto` pode ser selecionado
- `Seguro` pode ser selecionado
- `Forte` pode ser selecionado
- `Ultra` pode ser selecionado
- toggle `Free Fire + BlueStacks` altera o comportamento
- toggle `Turbo FPS` altera o comportamento

## Bandeja E Inicialização

- ícone da bandeja aparece quando o app é minimizado
- ícone da bandeja atualiza o uso de RAM
- `Iniciar com Windows` pode ser ativado
- `Iniciar com Windows` pode ser desativado
- itens do menu da bandeja são legíveis e clicáveis
- `Mostrar Painel` restaura a janela principal
- `Sair` encerra o app corretamente

## Logs E Relatórios

- log visual é atualizado durante a otimização
- arquivo de log `.txt` é criado
- histórico de telemetria é atualizado
- relatório técnico abre corretamente
- texto de recomendação aparece corretamente

## Segurança Da Restauração

- plano de energia é restaurado
- prioridades são restauradas
- processos suspensos são retomados quando esperado
- nenhum processo crítico do Windows é afetado

## Instalador

- instalador abre corretamente
- fluxo de instalação conclui
- aplicativo instalado inicia
- fluxo de desinstalação conclui
- comportamento do atalho na área de trabalho está correto quando habilitado

## Release Notes

- README aponta para `dist/current`
- `RELEASE_NOTES.md` corresponde à release atual
- capturas de tela e assets existem
- metadados de versão correspondem a `4.3.0`
- título da release no GitHub e binários anexados correspondem a `dist/current`
