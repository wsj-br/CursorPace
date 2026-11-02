# CursorPace

**Leer en otros idiomas:**

<small id="lang-list">[English](../README.md) · [Português (Brasil)](./README.pt-BR.md) · [Deutsch](./README.de.md) · [Español](./README.es.md) · [Français](./README.fr.md) · [हिन्दी](./README.hi.md) · [日本語](./README.ja.md) · [한국어](./README.ko.md) · [简体中文](./README.zh-Hans.md) · [繁體中文](./README.zh-Hant.md)</small>

<p align="center">
  <img src="../Assets/cursor_pace_screenshot.png" alt="CursorPace" width="400">
</p>

Aplicación de escritorio para Windows, Linux y macOS que rastrea la cuota de modelos de Cursor a lo largo de un ciclo de facturación. Inicie sesión con su cuenta de Cursor para obtener el uso automáticamente. El gráfico muestra dos porcentajes independientes: **Modelos de Cursor** y **Otros modelos**.

La línea discontinua de **Uso esperado** es un ritmo constante del 0% al inicio del ciclo hasta el 100% en la próxima renovación. Cursor y Otros modelos siguen sus muestras. Una estimación separada proyecta el consumo diario y el agotamiento. [QUICKSTART.md](QUICKSTART.es.md) cubre el gráfico, la bandeja y la configuración.

Iniciar sesión utiliza un WebView nativo incrustado (WebView2 en Windows, WKWebView en macOS, WebKitGTK en Linux) y tu sesión del panel de control de Cursor. No hay API oficial para planes personales y ninguna clave de API de equipo.

## Requisitos

- Windows 10 u 11 x64, Linux x64 o ARM64, o macOS (Intel o Apple Silicon)
- Para una versión de Windows: la carga útil de la aplicación es autocontenida. **Iniciar sesión** también necesita el [Microsoft Edge WebView2 Runtime](https://go.microsoft.com/fwlink/p/?LinkId=2124703) (ya está en la mayoría de los PC con Windows 11; el instalador ofrece la página de descarga si falta)
- Linux: WebKitGTK 4.1 (`libwebkit2gtk-4.1-0`; WPE puede funcionar dependiendo de la compilación de Avalonia WebView). Los iconos de la bandeja de GNOME pueden necesitar la extensión AppIndicator. La aplicación utiliza Inter incluido e ignora las fuentes `.woff` del sistema, por lo que una fuente predeterminada WOFF no puede colgar o bloquear el inicio.
- macOS: WKWebView (integrado). Las versiones no están firmadas; Gatekeeper puede requerir **Abrir de todos modos** en **Configuración del sistema → Privacidad y seguridad**
- Para compilar desde el código fuente: [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Instalación

1. Descargue la compilación para su plataforma desde la página de versiones de este repositorio:
   - **Windows**: `CursorPace-*-win-x64-setup.exe`
   - **Linux**: `CursorPace-*-linux-x64.AppImage` o `*-linux-x64.deb` (x86_64), o los archivos `linux-arm64` coincidentes (ARM64)
   - **macOS**: `CursorPace-*-osx-arm64.dmg` (Apple Silicon) o `*-osx-x64.dmg` (Intel)
2. **Windows**: ejecute el instalador. Si SmartScreen advierte que la aplicación no está firmada, elija **Más información** y luego **Ejecutar de todos modos**. Si falta WebView2 Runtime, abra la página de descarga que ofrece el instalador.
3. **Linux**: haga que el AppImage sea ejecutable (`chmod +x`) y luego ejecútelo. El primer inicio puede tardar un momento mientras se extrae el paquete. En Debian o Ubuntu, `sudo apt install ./CursorPace-*.deb` instala el paquete y extrae las dependencias de GTK/WebKit.
4. **macOS**: abra el DMG, arrastre `CursorPace.app` a Aplicaciones y luego ábralo. Si Gatekeeper bloquea la aplicación sin firmar, haga clic derecho en **Abrir** una vez y luego elija **Abrir de todos modos** en **Configuración del sistema → Privacidad y seguridad**.
5. Inicie sesión en Cursor. Si **Iniciar en la bandeja de notificaciones** está activado (el valor predeterminado), abra primero la ventana desde el icono de la bandeja o inicie con `--show`.

Consulte [QUICKSTART.md](QUICKSTART.es.md) para la configuración inicial, el inicio de sesión en la cuenta de Cursor, el gráfico de uso, el comportamiento de la bandeja y la resolución de problemas.

## Características

- Iniciar sesión en Cursor y actualizar el uso bajo demanda, o automáticamente cada 1, 2, 4, 6 o 12 horas
- El inicio del ciclo de facturación y la próxima renovación se obtienen de Cursor. Anterior y Siguiente abren ciclos almacenados anteriormente
- Tarjetas de resumen para las fechas del ciclo, el pronóstico de cada modelo y la lectura más reciente frente al ritmo esperado
- Rangos del Gráfico **1D**, **2D**, **7D**, **1W**, **2W** y **1M**, con zoom por arrastre y una lectura del cursor
- Se cierra a la bandeja de notificaciones. Inicio opcional al iniciar sesión. Un segundo inicio abre la ventana que ya se está ejecutando.
- Tema claro, oscuro o del sistema, y una elección de idioma tomada de `locales/ui-languages.json`. Si ese archivo falta, la única opción es el inglés. La ventana recuerda su tamaño, posición y estado maximizado.
- Exportar uso como CSV, y hacer una copia de seguridad o restaurar la configuración y las muestras.
- **Opcional.** [Servidor de sincronización de CursorPace](https://github.com/wsj-br/CursorPace-SyncServer) mantiene las instalaciones de CursorPace en una red local sincronizadas. Comparte muestras de uso y límites del ciclo de facturación, para que un Mac, un PC con Windows y una máquina Linux en el mismo entorno de desarrollo muestren el mismo gráfico. Instale y ejecute el servidor desde ese repositorio.

## Compilar desde el código fuente

En Windows (PowerShell) o Linux/macOS (bash):

```bash
dotnet restore
dotnet build
dotnet test ./Tests/CursorPace.Tests.csproj
dotnet run --project ./CursorPace.csproj
```

O use los scripts de mantenimiento: `.\scripts\dev.ps1` / `./scripts/dev.sh`. Estos scripts muestran la ventana por defecto; use `.\scripts\dev.ps1 -NoShow` o `./scripts/dev.sh --no-show` para usar la visibilidad de Inicio normal de la aplicación.

Publicación autocontenida (mantener `PublishSingleFile=false`):

```bash
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=false
dotnet publish -c Release -r linux-arm64 --self-contained -p:PublishSingleFile=false
dotnet publish -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=false
dotnet publish -c Release -r osx-x64 --self-contained -p:PublishSingleFile=false
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=false
```

El inicio de sesión en Linux requiere WebKitGTK 4.1 instalado en la máquina que ejecuta el binario publicado.
Una actualización de uso en segundo plano en Linux utiliza un host WebView transparente de 1x1 para que no
parpadee la ventana de inicio de sesión; macOS y Windows mantienen ese host con el tamaño de inicio de sesión fuera de la pantalla.

El proyecto incluye una pequeña compilación de compatibilidad de Avalonia.Controls.WebView 12.1.0 bajo
`vendor/` para la corrección de la ABI de macOS `CGRect`/`CGSize`. No se requiere un tiempo de ejecución de WebView de macOS separado.

Empaquetado de la versión:

```powershell
.\scripts\build.ps1          # Windows: Inno Setup installer
```

```bash
./scripts/build.sh             # Linux: AppImage and .deb; macOS: DMG
```

Solo publicar (omitir empaquetado): añadir `--skip-installer` / `-SkipInstaller`.

El flujo de trabajo completo del colaborador se encuentra en [dev/DEVELOPMENT.md](../dev/DEVELOPMENT.md).

## Documentación

| Documento | Audiencia |
| --- | --- |
| [QUICKSTART.md](QUICKSTART.es.md) | Instalación, inicio de sesión, uso diario, solución de problemas |
| [dev/DEVELOPMENT.md](../dev/DEVELOPMENT.md) | Compilar, probar, ejecutar, empaquetar, contribuir |
| [dev/CHANGELOG.md](../dev/CHANGELOG.md) | Cambios visibles para el usuario |

## Descargo de responsabilidad

[Cursor](https://cursor.com/home) es un producto de Anysphere, Inc. CursorPace es un proyecto independiente y no está asociado ni respaldado por Cursor o Anysphere.

## Licencia

Copyright 2026 Waldemar Scudeller Jr.

Este software está bajo la Licencia MIT. Consulte el archivo [LICENSE](../LICENSE) para obtener más detalles.

Las traducciones de esta página se producen automáticamente. Prefiere esta página en inglés cuando una traducción no coincida con la aplicación.
