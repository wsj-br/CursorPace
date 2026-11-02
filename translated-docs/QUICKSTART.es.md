# Inicio rápido

**Leer en otros idiomas:**

<small id="lang-list">[English](../QUICKSTART.md) · [Português (Brasil)](./QUICKSTART.pt-BR.md) · [Deutsch](./QUICKSTART.de.md) · [Español](./QUICKSTART.es.md) · [Français](./QUICKSTART.fr.md) · [हिन्दी](./QUICKSTART.hi.md) · [日本語](./QUICKSTART.ja.md) · [한국어](./QUICKSTART.ko.md) · [简体中文](./QUICKSTART.zh-Hans.md) · [繁體中文](./QUICKSTART.zh-Hant.md)</small>

Guía para el usuario final de CursorPace. Para compilar desde el código fuente, consulta [dev/DEVELOPMENT.md](../dev/DEVELOPMENT.md).

## Instalación

Descarga una compilación desde la página de Lanzamientos de este repositorio.

**Windows.** Ejecuta `CursorPace-*-win-x64-setup.exe`. Si SmartScreen advierte que la aplicación no está firmada, elige **Más información** y luego **Ejecutar de todos modos**. **Iniciar sesión** necesita el [Microsoft Edge WebView2 Runtime](https://go.microsoft.com/fwlink/p/?LinkId=2124703). El instalador ofrece la página de descarga cuando falta.

**Linux.** Para la mayoría de las distribuciones, `chmod +x CursorPace-*-linux-x64.AppImage` (o el archivo `linux-arm64`) y ejecútalo. En Debian o Ubuntu, `sudo apt install ./CursorPace-*-linux-x64.deb` (o `linux-arm64`) instala GTK y WebKit. El inicio de sesión de Google puede estar bloqueado en WebKit; usa GitHub, correo electrónico o inicia sesión en Windows.

**macOS.** Abre `CursorPace-*-osx-arm64.dmg` (Apple Silicon) o `*-osx-x64.dmg` (Intel) y arrastra `CursorPace.app` a Aplicaciones. Si Gatekeeper bloquea la aplicación sin firmar, haz clic derecho en **Abrir** una vez y luego elige **Abrir de todos modos** en **Configuración del Sistema → Privacidad y Seguridad**.

## Primer inicio

**Iniciar en la bandeja de notificaciones** está activado por defecto, por lo que el primer inicio puede mostrar solo el icono de la bandeja. Abre la ventana desde ese icono, o inicia con `--show`.

1. Elige **Iniciar sesión**.
2. Completa el inicio de sesión de Google, GitHub o de dos factores en la ventana incrustada. Se cierra cuando Cursor acepta la sesión. Si ya ves tu cuenta, elige **Continuar**.
3. El ciclo de facturación y el uso aparecen en el gráfico.

Puedes iniciar sesión más tarde desde **Configuración** → **Cuenta de Cursor**. Después de una actualización exitosa, ese encabezado dice **Cuenta de Cursor (conectada)**. **Actualizar** en la barra de título, o **Actualizar ahora** en Configuración, obtiene los datos inmediatamente.

**Cerrar sesión** borra la sesión de Cursor. En Windows y macOS, un inicio de sesión guardado de Google o GitHub puede permanecer, por lo que el próximo inicio de sesión puede omitir esa contraseña. En Linux, **Cerrar sesión** borra todo el perfil del navegador. Las muestras de uso permanecen en el disco.

Con **Actualizar uso automáticamente** activado, las actualizaciones se realizan a la 1, 2, 4, 6 o 12 horas desde la medianoche local. Al iniciar, una actualización se ejecuta solo después de que la última actualización exitosa tenga al menos 20 minutos de antigüedad y se haya perdido una franja horaria programada o el intervalo completo ya haya transcurrido.

## Ventana principal

<p align="center">
  <img src="../Assets/cursor_pace_screenshot.png" alt="CursorPace" width="600">
</p>

Tres tarjetas se encuentran encima del gráfico:

- **Inicio del ciclo** y **Próxima renovación**. En el ciclo actual, el tiempo restante se encuentra junto a la fecha de renovación.
- Un pronóstico para cada modelo. Dentro del ciclo, este es el tiempo de agotamiento. Por encima de la línea de uso esperado discontinua, es cuando esa línea alcanza la lectura. Sobre o por debajo de la línea, es el porcentaje proyectado en la renovación. En el ciclo actual, una fecha incluye la espera a su lado.
- **Última medición**: la muestra más reciente de este ciclo, con Cursor %, Otros %, Uso esperado %, y qué tan lejos está cada serie de ese ritmo.

Anterior y Siguiente en el encabezado del mes permiten navegar por los ciclos almacenados. Bajo ese encabezado, `Cursor dd-MMM HH:mm` es la última actualización. Un punto rojo significa que la cuenta ha cerrado sesión o que la actualización falló. Haz clic para abrir **Cuenta de Cursor**. Cuando se configura un servidor de sincronización, **Servidor de sincronización** y su última hora de éxito también aparecen allí. Un punto rojo significa que el último intento falló. Haz clic para abrir **Servidor de sincronización**.

| Rango | Lo que muestra |
| --- | --- |
| **1D** / **2D** | Las últimas 24 o 48 horas |
| **7D** / **1W** | Los últimos siete días |
| **2W** | Los últimos 14 días |
| **1M** | El ciclo completo mostrado, una muestra por día local |

En el ciclo en vivo, un rango corto termina en la hora actual. En un ciclo anterior, termina en la renovación de ese ciclo. El inicio permanece en el inicio del ciclo cuando la ventana de otra manera comenzaría antes.

Arrastra por el gráfico para hacer zoom. El clic derecho regresa a **1M**. Elegir un botón de rango, incluido el ya seleccionado, sale del zoom y muestra ese rango. **Cada muestra hasta** (**2D**, **4D**, **7D** o **14D**, predeterminado **14D**) establece hasta dónde el gráfico traza cada muestra y etiqueta la hora local. Los rangos más largos mantienen la última muestra de cada día y etiquetan el día del mes. El eje vertical ajusta los valores en pantalla, hasta el 120%.

Mueve el puntero para leer Cursor, Otros modelos y Uso esperado en ese momento. Después de la última muestra, Cursor y Otros muestran la estimación, marcada `estimated`. Durante un zoom, la lectura también muestra cada valor al inicio del zoom y el cambio desde allí.

| Control | Acción |
| --- | --- |
| **Actualizar** | Obtener el uso de Cursor ahora. Oculto en la página de Configuración |
| **Configuración** | Abrir Configuración en esta ventana |
| **Atrás** | Volver al gráfico |
| **Minimizar** / **Maximizar** | Controles de ventana estándar. La aplicación permanece en la bandeja |
| Cerrar | Ocultar la ventana. La aplicación permanece en la bandeja |

La ventana recuerda su tamaño, posición y estado maximizado. Las etiquetas informativas se pueden seleccionar y copiar.

## Esperado y estimado

- **Uso esperado** en el gráfico es la línea discontinua desde el 0% al inicio del ciclo hasta el 100% en la próxima renovación. Al pasar el ratón se usa esta línea.
- **Cursor** y **Otros modelos** son las muestras medidas.
- **Esperado** en un CSV de ciclo sigue cada muestra, luego ajusta la cuota restante al 100% en la renovación.
- **Estimado** es la proyección de puntos redondos desde la última muestra hacia la renovación. Después de la última muestra, al pasar el ratón se usa esta línea.

Modelos de Cursor y Otros modelos permanecen independientes.

## Configuración

La Configuración reemplaza el gráfico. **Atrás**, o el encabezado **Configuración**, regresa a él. La última pestaña se abre de nuevo la próxima vez, incluso después de un reinicio.

**Servidor de sincronización** es opcional. [Servidor de sincronización de CursorPace](https://github.com/wsj-br/CursorPace-SyncServer) comparte muestras de uso y límites del ciclo de facturación entre máquinas en una red local, para que una Mac, una PC con Windows y una máquina Linux en el mismo entorno de desarrollo muestren el mismo gráfico. Instala y ejecuta el servidor desde ese repositorio. El tema, el idioma, la ubicación de la ventana y las opciones de inicio permanecen en cada equipo.

| Pestaña | Lo que puedes configurar |
| --- | --- |
| Inicio y pantalla | **Iniciar al iniciar sesión**, **Iniciar en la bandeja de notificaciones**, tema (Sistema, Claro u Oscuro), **Idioma** (las configuraciones regionales en `locales/ui-languages.json`, o inglés cuando ese archivo falta), y **Cada muestra hasta** |
| Cuenta de Cursor | **Iniciar sesión** (oculto mientras está Conectado), **Actualizar ahora**, **Cerrar sesión**, y actualización automática cada 1, 2, 4, 6 o 12 horas |
| Servidor de sincronización | **Sincronizar con el servidor**, URL del servidor, token de API, nombre de la máquina (el nombre de host por defecto) y **Volver a sincronizar ahora**. El enlace abre el repositorio del servidor de sincronización |
| Exportar y copia de seguridad | **Exportar ciclo CSV** para el ciclo en pantalla, **Exportar uso** para cada muestra almacenada mientras se ha iniciado sesión, y **Copia de seguridad** / **Restaurar** de la configuración y las muestras como un solo archivo zip. **Abrir carpeta** abre el destino |
| Acerca de | Versión, fecha y hora de compilación UTC, derechos de autor, Licencia MIT y el repositorio de GitHub |

La copia de seguridad y la restauración mantienen el inicio de sesión de Cursor en esta máquina.

## Bandeja

Haz clic en el icono, o elige **Abrir**, para mostrar la ventana. **Salir** cierra la aplicación. La información sobre herramientas muestra el porcentaje de hoy y, una vez que haya suficientes datos, el porcentaje proyectado en la renovación.

En macOS, un clic abre el menú de la bandeja, y el icono del Dock se oculta mientras la ventana está oculta o minimizada. Muestra la ventana desde la bandeja, con `--show`, o volviendo a iniciar la aplicación.

Si falta el icono, abre el desbordamiento de notificaciones. En GNOME, puede que se requiera la extensión AppIndicator.

Un segundo inicio muestra la ventana que ya se está ejecutando. Un segundo inicio de `--background` la deja oculta. `--show` fuerza la apertura de la ventana. **Iniciar al iniciar sesión** sigue a **Iniciar en la bandeja de notificaciones**.

## Datos

```text
Windows: %LocalAppData%\CursorPace\
Linux:   ~/.local/share/CursorPace/
macOS:   ~/Library/Application Support/CursorPace/
```

`settings.json` contiene las preferencias, la ubicación de la ventana y los límites del ciclo. `usage-samples.json` contiene las muestras. El inicio de sesión de Cursor reside en `WebView2` en Windows, `WebView` en Linux y macOS, o `WebView-AppImage` al ejecutar una AppImage de Linux. Un archivo que no se puede analizar se copia a `*.corrupt.json` y se reemplaza por uno en blanco. Un archivo bloqueado o ilegible se deja tal cual.

Elimina la carpeta para empezar de nuevo. En Windows, desinstalar la aplicación la elimina. En Linux y macOS, elimínala tú mismo.

## Renovación

Una obtención que informa un nuevo inicio de ciclo archiva el ciclo anterior y conserva sus muestras. Anterior y Siguiente abren esos ciclos. La aplicación también se actualiza cuando la fecha local alcanza la renovación almacenada. La ventana puede permanecer oculta. El proceso debe estar en ejecución para esa verificación y para las actualizaciones automáticas.

## Desinstalar

Sal de la aplicación desde el menú de la bandeja. En Windows, desinstala **CursorPace** desde **Aplicaciones instaladas**. En Linux y macOS, elimina la aplicación y, para un inicio limpio, la carpeta de datos mencionada anteriormente.

## Solución de problemas

**La aplicación no se inicia.** Finaliza cualquier proceso de `CursorPace` y vuelve a iniciarla. En Windows, comprueba el Visor de eventos. En Linux, prueba `journalctl --user -xe`. En macOS, comprueba Console.app. Si Linux consume el 100% de la CPU, o `crash.log` dice `Could not create glyphTypeface`, instala una versión posterior a la 0.3.0. Si **Iniciar al iniciar sesión** sigue apuntando a una AppImage eliminada, ejecuta la nueva AppImage una vez.

**Falla el inicio de sesión, o "No se pudo encontrar el módulo especificado".** En Windows, instala el [tiempo de ejecución de WebView2](https://go.microsoft.com/fwlink/p/?LinkId=2124703) (Evergreen x64). En Linux, instala `libwebkit2gtk-4.1-0`. Si Google bloquea el navegador incrustado, usa GitHub o el correo electrónico. Elige **Continuar** cuando tu cuenta de Cursor ya esté en pantalla.

**El uso no se actualiza.** Confirma **Cuenta de Cursor (conectada)** y las actualizaciones automáticas, o elige **Actualizar**. Comprueba la fecha, hora y zona horaria del sistema. Después de un límite de tasa de Cursor, la aplicación espera el siguiente intervalo.

**Falta el icono de la bandeja o la barra de tareas.** Abre el desbordamiento de notificaciones. En Linux, reinicia la aplicación para que pueda escribir `~/.local/share/applications/CursorPace.desktop`. Si el icono de la barra de tareas sigue siendo un engranaje genérico, cierra la sesión una vez, o en X11 presiona Alt+F2, escribe `r` y presiona Enter.

**La configuración no se guarda, o el porcentaje de hoy parece incorrecto.** Confirma que puedes escribir en la carpeta de datos. Si `settings.corrupt.json` existe, elimínalo y `settings.json` para restablecer. Luego, verifica la hora y elige **Actualizar**.

**Iniciar al iniciar sesión no hace nada.** Confirma que la configuración está activada. Windows almacena `CursorPace` en la clave Run del usuario actual y añade `--background` cuando el inicio en la bandeja está activado. Una aplicación firmada de macOS aparece en **Abrir al iniciar sesión**. Una compilación sin firmar usa `~/Library/LaunchAgents/com.cursorpace.app.plist` e inicia el `.app` con `/usr/bin/open`. Linux usa `~/.config/autostart/cursor-pace.desktop`. Una entrada de AppImage debe apuntar al archivo `.AppImage`. Desactiva y activa la configuración, o inicia la nueva AppImage una vez, para reescribirla.

**Dos iconos en la bandeja, o cierre de sesión después de un reinicio de Linux.** Salga de la bandeja y abra la aplicación una vez. En Linux, inicie sesión de nuevo en una compilación actual para que la sesión se guarde en `cookies.sqlite`.

Las traducciones de esta página se producen automáticamente. Prefiere esta página en inglés cuando una traducción no coincida con la aplicación.
