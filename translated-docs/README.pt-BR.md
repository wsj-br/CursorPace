# CursorPace

**Leia em outros idiomas:**

<small id="lang-list">[English](../README.md) · [Português (Brasil)](./README.pt-BR.md) · [Deutsch](./README.de.md) · [Español](./README.es.md) · [Français](./README.fr.md) · [हिन्दी](./README.hi.md) · [日本語](./README.ja.md) · [한국어](./README.ko.md) · [简体中文](./README.zh-Hans.md) · [繁體中文](./README.zh-Hant.md)</small>

<p align="center">
  <img src="../Assets/cursor_pace_screenshot.png" alt="CursorPace" width="400">
</p>

Aplicativo de desktop para Windows, Linux e macOS que rastreia a cota do modelo Cursor em um ciclo de faturamento. Faça login com sua conta Cursor para obter o uso automaticamente. O gráfico mostra duas porcentagens independentes: **Modelos Cursor** e **Outros Modelos**.

A linha tracejada **Uso esperado** é um ritmo reto de 0% no início do ciclo para 100% na próxima renovação. Cursor e Outros Modelos seguem suas amostras. Uma estimativa separada projeta o consumo diário e o esgotamento. [QUICKSTART.md](QUICKSTART.pt-BR.md) aborda o gráfico, a bandeja e as configurações.

O login usa um WebView nativo incorporado (WebView2 no Windows, WKWebView no macOS, WebKitGTK no Linux) e sua sessão do painel Cursor. Não há API oficial de plano pessoal nem chave de API de equipe.

## Requisitos

- Windows 10 ou 11 x64, Linux x64 ou ARM64, ou macOS (Intel ou Apple Silicon)
- Para uma versão do Windows, instale: o payload do aplicativo é autocontido. O **Fazer login** também precisa do [Microsoft Edge WebView2 Runtime](https://go.microsoft.com/fwlink/p/?LinkId=2124703) (já presente na maioria dos PCs com Windows 11; o instalador oferece a página de download se estiver faltando)
- Linux: WebKitGTK 4.1 (`libwebkit2gtk-4.1-0`; WPE pode funcionar dependendo da compilação do Avalonia WebView). Os ícones da bandeja do GNOME podem precisar da extensão AppIndicator. O aplicativo usa o Inter empacotado e ignora as fontes `.woff` do sistema, para que uma face padrão WOFF não trave ou cause falha na inicialização.
- macOS: WKWebView (integrado). As versões não são assinadas; o Gatekeeper pode exigir **Abrir Mesmo Assim** em **Ajustes do Sistema → Privacidade e Segurança**
- Para compilar a partir do código-fonte: [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Instalar

1. Baixe a compilação para sua plataforma na página de Lançamentos deste repositório:
   - **Windows**: `CursorPace-*-win-x64-setup.exe`
   - **Linux**: `CursorPace-*-linux-x64.AppImage` ou `*-linux-x64.deb` (x86_64), ou os arquivos `linux-arm64` correspondentes (ARM64)
   - **macOS**: `CursorPace-*-osx-arm64.dmg` (Apple Silicon) ou `*-osx-x64.dmg` (Intel)
2. **Windows**: execute o instalador. Se o SmartScreen avisar que o aplicativo não está assinado, escolha **Mais informações** e depois **Executar mesmo assim**. Se o WebView2 Runtime estiver faltando, abra a página de download que o instalador oferece.
3. **Linux**: torne o AppImage executável (`chmod +x`), depois execute-o. O primeiro lançamento pode demorar um pouco enquanto o pacote é extraído. No Debian ou Ubuntu, `sudo apt install ./CursorPace-*.deb` instala o pacote e puxa as dependências GTK/WebKit.
4. **macOS**: abra o DMG, arraste `CursorPace.app` para Aplicativos, depois abra-o. Se o Gatekeeper bloquear o aplicativo não assinado, clique com o botão direito em **Abrir** uma vez, depois escolha **Abrir Mesmo Assim** em **Ajustes do Sistema → Privacidade e Segurança**.
5. Faça login no Cursor. Se **Iniciar na bandeja de notificação** estiver ativado (o padrão), abra a janela a partir do ícone da bandeja primeiro, ou inicie com `--show`.

Consulte [QUICKSTART.md](QUICKSTART.pt-BR.md) para configuração inicial, login na conta Cursor, o gráfico de uso, comportamento da bandeja e solução de problemas.

## Recursos

- Faça login no Cursor e atualize o uso sob demanda, ou automaticamente a cada 1, 2, 4, 6 ou 12 horas
- O início do ciclo de faturamento e a próxima renovação vêm do Cursor. Anterior e Próximo abrem ciclos armazenados anteriores
- Cartões de resumo para as datas do ciclo, a previsão de cada modelo e a leitura mais recente versus o ritmo esperado
- Intervalos do gráfico **1D**, **2D**, **7D**, **1W**, **2W** e **1M**, com arrastar para zoom e leitura do ponteiro
- Fecha para a bandeja de notificação. Lançamento opcional ao fazer login. Um segundo lançamento abre a janela que já está em execução
- Tema claro, escuro ou do sistema, e uma escolha de idioma retirada de `locales/ui-languages.json`. Se esse arquivo estiver faltando, a única escolha é o inglês. A janela lembra seu tamanho, posição e estado maximizado
- Exporte o uso como CSV e faça backup ou restaure as configurações e amostras
- **Opcional.** [Servidor de sincronização CursorPace](https://github.com/wsj-br/CursorPace-SyncServer) mantém as instalações do CursorPace em uma rede local sincronizadas. Ele compartilha amostras de uso e limites do ciclo de faturamento, para que um Mac, um PC com Windows e uma máquina Linux no mesmo ambiente de desenvolvimento mostrem o mesmo gráfico. Instale e execute o servidor a partir desse repositório.

## Compilar a partir do código-fonte

No Windows (PowerShell) ou Linux/macOS (bash):

```bash
dotnet restore
dotnet build
dotnet test ./Tests/CursorPace.Tests.csproj
dotnet run --project ./CursorPace.csproj
```

Ou use os scripts do mantenedor: `.\scripts\dev.ps1` / `./scripts/dev.sh`. Esses scripts mostram a janela por padrão; use `.\scripts\dev.ps1 -NoShow` ou `./scripts/dev.sh --no-show` para usar a visibilidade normal de inicialização do aplicativo.

Publicação autocontida (mantenha `PublishSingleFile=false`):

```bash
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=false
dotnet publish -c Release -r linux-arm64 --self-contained -p:PublishSingleFile=false
dotnet publish -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=false
dotnet publish -c Release -r osx-x64 --self-contained -p:PublishSingleFile=false
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=false
```

O login no Linux requer o WebKitGTK 4.1 instalado na máquina que executa o binário publicado.
Uma atualização de uso em segundo plano no Linux usa um host WebView transparente 1x1 para que não
pisque a janela de login; macOS e Windows mantêm esse host no tamanho de login fora da tela.

O projeto inclui uma pequena compilação de compatibilidade do Avalonia.Controls.WebView 12.1.0 sob `vendor/` para a correção da ABI do macOS `CGRect`/`CGSize`. Nenhum runtime WebView separado para macOS é necessário.

Empacotamento de lançamento:

```powershell
.\scripts\build.ps1          # Windows: Inno Setup installer
```

```bash
./scripts/build.sh             # Linux: AppImage and .deb; macOS: DMG
```

Publicar apenas (ignorar empacotamento): adicione `--skip-installer` / `-SkipInstaller`.

O fluxo de trabalho completo do colaborador está em [dev/DEVELOPMENT.md](../dev/DEVELOPMENT.md).

## Documentação

| Documento | Público |
| --- | --- |
| [QUICKSTART.md](QUICKSTART.pt-BR.md) | Instalação, login, uso diário, solução de problemas |
| [dev/DEVELOPMENT.md](../dev/DEVELOPMENT.md) | Compilar, testar, executar, empacotar, contribuir |
| [dev/CHANGELOG.md](../dev/CHANGELOG.md) | Alterações visíveis para o usuário |

## Aviso Legal

[Cursor](https://cursor.com/home) é um produto da Anysphere, Inc. CursorPace é um projeto independente e não está associado ou é endossado por Cursor ou Anysphere.

## Licença

Copyright 2026 Waldemar Scudeller Jr.

Este software é licenciado sob a Licença MIT. Consulte o arquivo [LICENSE](../LICENSE) para obter detalhes.

As traduções desta página são produzidas automaticamente. Prefira esta página em inglês quando uma tradução não estiver de acordo com o aplicativo.
