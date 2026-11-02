# Início rápido

**Leia em outros idiomas:**

<small id="lang-list">[English](../QUICKSTART.md) · [Português (Brasil)](./QUICKSTART.pt-BR.md) · [Deutsch](./QUICKSTART.de.md) · [Español](./QUICKSTART.es.md) · [Français](./QUICKSTART.fr.md) · [हिन्दी](./QUICKSTART.hi.md) · [日本語](./QUICKSTART.ja.md) · [한국어](./QUICKSTART.ko.md) · [简体中文](./QUICKSTART.zh-Hans.md) · [繁體中文](./QUICKSTART.zh-Hant.md)</small>

Guia do usuário final para CursorPace. Para compilar a partir do código-fonte, consulte [dev/DEVELOPMENT.md](../dev/DEVELOPMENT.md).

## Instalar

Baixe uma compilação na página de Lançamentos deste repositório.

**Windows.** Execute `CursorPace-*-win-x64-setup.exe`. Se o SmartScreen avisar que o aplicativo não está assinado, escolha **Mais informações**, depois **Executar mesmo assim**. **Fazer login** requer o [Microsoft Edge WebView2 Runtime](https://go.microsoft.com/fwlink/p/?LinkId=2124703). O instalador oferece a página de download quando ele está ausente.

**Linux.** Para a maioria das distribuições, `chmod +x CursorPace-*-linux-x64.AppImage` (ou o arquivo `linux-arm64`) e execute-o. No Debian ou Ubuntu, `sudo apt install ./CursorPace-*-linux-x64.deb` (ou `linux-arm64`) instala o GTK e o WebKit. O login do Google pode ser bloqueado no WebKit; use GitHub, e-mail ou faça login no Windows.

**macOS.** Abra `CursorPace-*-osx-arm64.dmg` (Apple Silicon) ou `*-osx-x64.dmg` (Intel) e arraste `CursorPace.app` para Aplicativos. Se o Gatekeeper bloquear o aplicativo não assinado, clique com o botão direito em **Abrir** uma vez, depois escolha **Abrir Mesmo Assim** em **Ajustes do Sistema → Privacidade e Segurança**.

## Primeira execução

**Iniciar na bandeja de notificação** está ativado por padrão, então o primeiro lançamento pode mostrar apenas o ícone da bandeja. Abra a janela a partir desse ícone, ou inicie com `--show`.

1. Escolha **Fazer login**.
2. Conclua o login do Google, GitHub ou de dois fatores na janela incorporada. Ela se fecha quando o Cursor aceita a sessão. Se você já vir sua conta, escolha **Continuar**.
3. O ciclo de faturamento e o uso aparecem no gráfico.

Você pode fazer login mais tarde em **Configurações** → **Conta Cursor**. Após uma atualização bem-sucedida, esse cabeçalho exibe **Conta Cursor (conectado)**. **Atualizar** na barra de título, ou **Atualizar agora** em Configurações, busca os dados imediatamente.

**Sair** encerra a sessão do Cursor. No Windows e macOS, um login salvo do Google ou GitHub pode permanecer, então o próximo login pode pular essa senha. No Linux, **Sair** limpa todo o perfil do navegador. As amostras de uso permanecem no disco.

Com **Atualizar uso automaticamente** ativado, as atualizações ocorrem a cada 1, 2, 4, 6 ou 12 horas a partir da meia-noite local. No lançamento, uma atualização é executada somente se o último sucesso tiver ocorrido há pelo menos 20 minutos e um slot agendado foi perdido ou o intervalo completo já tiver decorrido.

## Janela principal

<p align="center">
  <img src="../Assets/cursor_pace_screenshot.png" alt="CursorPace" width="600">
</p>

Três cartões ficam acima do gráfico:

- **Início do ciclo** e **Próxima renovação**. No ciclo ativo, o tempo restante fica ao lado da data de renovação.
- Uma previsão para cada modelo. Dentro do ciclo, este é o tempo de esgotamento. Acima da linha tracejada de uso esperado, é quando essa linha atinge a leitura. Na linha ou abaixo dela, é a porcentagem projetada na renovação. No ciclo ativo, uma data inclui a espera ao lado dela.
- **Última medição**: a amostra mais recente neste ciclo, com Cursor %, Outros %, Uso esperado %, e quão distante cada série está desse ritmo.

Anterior e Próximo no cabeçalho do mês navegam pelos ciclos armazenados. Sob esse cabeçalho, `Cursor dd-MMM HH:mm` é a atualização mais recente. Um ponto vermelho significa que a conta está desconectada ou a atualização falhou. Clique nele para abrir **Conta Cursor**. Quando um servidor de sincronização é configurado, **Servidor de Sincronização** e seu último horário de sucesso também aparecem lá. Um ponto vermelho significa que a última tentativa falhou. Clique nele para abrir **Servidor de Sincronização**.

| Intervalo | O que mostra |
| --- | --- |
| **1D** / **2D** | As últimas 24 ou 48 horas |
| **7D** / **1W** | Os últimos sete dias |
| **2W** | Os últimos 14 dias |
| **1M** | O ciclo inteiro exibido, uma amostra por dia local |

No ciclo ativo, um curto período termina no horário atual. Em um ciclo anterior, ele termina na renovação desse ciclo. O início permanece no Início do ciclo quando a janela, de outra forma, começaria mais cedo.

Arraste sobre o Gráfico para ampliar. O clique com o botão direito retorna para **1M**. A escolha de um botão de intervalo, incluindo o já selecionado, sai do zoom e mostra esse intervalo. **Cada amostra até** (**2D**, **4D**, **7D** ou **14D**, padrão **14D**) define até onde o Gráfico plota cada amostra e rotula a hora local. Intervalos mais longos mantêm a última amostra de cada dia e rotulam o dia do mês. O eixo vertical se ajusta aos valores na tela, até 120%.

Mova o ponteiro para ler Cursor, Outros Modelos e Uso esperado naquele momento. Após a última amostra, Cursor e Outros mostram a estimativa, marcada `estimated`. Durante um zoom, a leitura também mostra cada valor no início do zoom e a alteração a partir daí.

| Controle | Ação |
| --- | --- |
| **Atualizar** | Buscar uso do Cursor agora. Oculto na página Configurações |
| **Configurações** | Abrir Configurações nesta janela |
| **Voltar** | Retornar ao Gráfico |
| **Minimizar** / **Maximizar** | Controles de janela padrão. O aplicativo permanece na bandeja |
| Fechar | Ocultar a janela. O aplicativo permanece na bandeja |

A janela lembra seu tamanho, posição e estado maximizado. Os rótulos informativos podem ser selecionados e copiados.

## Esperado e estimado

- **Uso esperado** no Gráfico é a linha tracejada de 0% no Início do ciclo a 100% na Próxima renovação. O passar do mouse usa esta linha.
- **Cursor** e **Outros Modelos** são as amostras medidas.
- **Esperado** em um CSV de ciclo segue cada amostra, então ajusta a cota restante para 100% na renovação.
- **Estimado** é a projeção pontilhada da última amostra em direção à renovação. Após a última amostra, o passar do mouse usa esta linha.

Modelos Cursor e Outros Modelos permanecem independentes.

## Configurações

As Configurações substituem o Gráfico. **Voltar**, ou o cabeçalho **Configurações**, retorna a ele. A última guia é aberta novamente na próxima vez, inclusive após uma reinicialização.

O **Servidor de Sincronização** é opcional. O [Servidor de sincronização CursorPace](https://github.com/wsj-br/CursorPace-SyncServer) compartilha amostras de uso e limites de ciclo de faturamento entre máquinas em uma rede local, para que um Mac, um PC com Windows e uma máquina Linux no mesmo ambiente de desenvolvimento mostrem o mesmo Gráfico. Instale e execute o servidor a partir desse repositório. Tema, Idioma, posicionamento da janela e opções de Inicialização permanecem em cada computador.

| Guia | O que você pode definir |
| --- | --- |
| Inicialização e Exibição | **Iniciar ao fazer login**, **Iniciar na bandeja de notificação**, tema (Sistema, Claro ou Escuro), **Idioma** (os locais em `locales/ui-languages.json`, ou Inglês quando esse arquivo estiver faltando) e **Cada amostra até** |
| Conta Cursor | **Fazer login** (oculto enquanto Conectado), **Atualizar agora**, **Sair** e atualização automática a cada 1, 2, 4, 6 ou 12 horas |
| Servidor de Sincronização | **Sincronizar com o servidor**, URL do servidor, Token de API, Nome da máquina (o nome do host por padrão) e **Sincronizar novamente agora**. O link abre o repositório do servidor de sincronização |
| Exportar e Backup | **Exportar CSV do Ciclo** para o ciclo na tela, **Exportar Uso** para cada amostra armazenada enquanto conectado, e **Backup** / **Restaurar** de configurações e amostras como um único zip. **Abrir Pasta** abre o destino |
| Sobre | Versão, data e hora da compilação UTC, direitos autorais, Licença MIT e o Repositório do GitHub |

Backup e Restaurar mantêm o login do Cursor nesta máquina.

## Bandeja

Clique no ícone, ou escolha **Abrir**, para exibir a janela. **Sair** encerra. A dica de ferramenta mostra a porcentagem de hoje e, uma vez que haja dados suficientes, a porcentagem projetada na renovação.

No macOS, um clique abre o menu da bandeja, e o ícone do Dock fica oculto enquanto a janela está oculta ou minimizada. Exiba a janela a partir da bandeja, com `--show`, ou iniciando o aplicativo novamente.

Se o ícone estiver faltando, abra o excesso de notificações. No GNOME, a extensão AppIndicator pode ser necessária.

Um segundo lançamento mostra a janela que já está em execução. Um segundo lançamento de `--background` a deixa oculta. `--show` força a abertura da janela. **Iniciar ao fazer login** segue **Iniciar na bandeja de notificação**.

## Dados

```text
Windows: %LocalAppData%\CursorPace\
Linux:   ~/.local/share/CursorPace/
macOS:   ~/Library/Application Support/CursorPace/
```

`settings.json` armazena preferências, posicionamento da janela e limites do ciclo. `usage-samples.json` armazena as amostras. O login do Cursor reside em `WebView2` no Windows, `WebView` no Linux e macOS, ou `WebView-AppImage` ao executar um AppImage do Linux. Um arquivo que não pode ser analisado é copiado para `*.corrupt.json` e substituído por um em branco. Um arquivo bloqueado ou ilegível é deixado como está.

Exclua a pasta para começar de novo. No Windows, desinstalar o aplicativo o remove. No Linux e macOS, exclua-o você mesmo.

## Renovação

Uma busca que relata um novo início de ciclo arquiva o ciclo anterior e mantém suas amostras. Anterior e Próximo abrem esses ciclos. O aplicativo também atualiza quando a data local atinge a renovação armazenada. A janela pode permanecer oculta. O processo precisa estar em execução para essa verificação e para atualizações automáticas.

## Desinstalar

Saia pelo menu da bandeja. No Windows, desinstale **CursorPace** de **Aplicativos instalados**. No Linux e macOS, exclua o aplicativo e, para um início limpo, a pasta de dados acima.

## Solução de problemas

**O aplicativo não inicia.** Encerre qualquer processo `CursorPace` e inicie novamente. No Windows, verifique o Visualizador de Eventos. No Linux, tente `journalctl --user -xe`. No macOS, verifique o Console.app. Se o Linux estiver com 100% de CPU, ou `crash.log` disser `Could not create glyphTypeface`, instale uma versão mais recente que 0.3.0. Se **Iniciar ao fazer login** ainda apontar para um AppImage excluído, execute o novo AppImage uma vez.

**O login falha, ou "O módulo especificado não pôde ser encontrado".** No Windows, instale o [WebView2 Runtime](https://go.microsoft.com/fwlink/p/?LinkId=2124703) (Evergreen x64). No Linux, instale `libwebkit2gtk-4.1-0`. Se o Google bloquear o navegador incorporado, use o GitHub ou e-mail. Escolha **Continuar** quando sua Conta Cursor já estiver na tela.

**O uso não atualiza.** Confirme a **Conta Cursor (conectado)** e as atualizações automáticas, ou escolha **Atualizar**. Verifique a data, hora e fuso horário do sistema. Após um limite de taxa do Cursor, o aplicativo aguarda o próximo intervalo.

**O ícone da bandeja ou da barra de tarefas está faltando.** Abra a área de notificação estendida. No Linux, reinicie o aplicativo para que ele possa gravar `~/.local/share/applications/CursorPace.desktop`. Se o ícone da barra de tarefas ainda for uma engrenagem genérica, faça logout uma vez, ou no X11 pressione Alt+F2, digite `r` e pressione Enter.

**As Configurações não estão sendo salvas, ou a porcentagem de hoje parece errada.** Confirme se você pode gravar na pasta de dados. Se `settings.corrupt.json` existir, exclua-o e `settings.json` para redefinir. Em seguida, verifique o relógio e escolha **Atualizar**.

**Iniciar ao fazer login não faz nada.** Confirme se a configuração está ativada. O Windows armazena `CursorPace` na chave Run do usuário atual e adiciona `--background` quando a inicialização da bandeja está ativada. Um aplicativo macOS assinado aparece em **Abrir ao fazer login**. Uma compilação não assinada usa `~/Library/LaunchAgents/com.cursorpace.app.plist` e inicia o `.app` com `/usr/bin/open`. O Linux usa `~/.config/autostart/cursor-pace.desktop`. Uma entrada AppImage precisa apontar para o arquivo `.AppImage`. Desative e ative a configuração, ou inicie o novo AppImage uma vez, para reescrevê-lo.

**Dois ícones na bandeja, ou desconectado após uma reinicialização do Linux.** Saia da bandeja e inicie o aplicativo uma vez. No Linux, faça login novamente em uma compilação atual para que a sessão seja armazenada em `cookies.sqlite`.

As traduções desta página são produzidas automaticamente. Prefira esta página em inglês quando uma tradução não estiver de acordo com o aplicativo.
