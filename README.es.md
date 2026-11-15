# BCN Wallet para Windows

[English](README.md) · [Українська](README.uk.md) · **Español** · [Português](README.pt.md) · [Türkçe](README.tr.md) · [Bahasa Indonesia](README.id.md) · [Tiếng Việt](README.vi.md) · [中文](README.zh.md)

Un monedero de escritorio para Bytecoin (BCN). Funciona **con o sin descargar la cadena de bloques**, guarda sus claves en su ordenador, tiene temas oscuro y claro y una interfaz en ocho idiomas. Se ofrece como instalador y como zip portátil.

Es un proyecto independiente de la comunidad de Bytecoin, sin relación con los fundadores de Bytecoin ni con bcndev.

![Resumen](docs/screenshots/overview-dark.png)

## Descarga

En [bytecoin.community](https://bytecoin.community/#downloads):

- `BCNWallet-<versión>-setup.exe`: se instala solo para su usuario (sin permisos de administrador) en `%LOCALAPPDATA%\Programs\BCN Wallet`, crea accesos directos en el menú Inicio y en el escritorio y una entrada en Configuración → Aplicaciones para desinstalarlo. Los monederos y los ajustes se guardan en `%APPDATA%\BCN Wallet`; la instalación, la actualización y la desinstalación nunca los tocan.
- `BCNWallet-<versión>-win-portable.zip`: descomprímalo en cualquier lugar, también en una memoria USB, y ejecute `BCNWallet.exe`. Todo (ajustes, archivos de monedero, caché y, en modo nodo completo, la cadena de bloques) queda en la carpeta `data` junto al programa. **Esa carpeta contiene sus monederos:** se pierden si la borra o la sustituye, si descomprime una actualización encima o si abre el programa directamente desde el zip. Anote antes las 24 palabras, no guarde la carpeta en Descargas, en carpetas temporales ni en carpetas sincronizadas con la nube, y haga copia de ella con el monedero cerrado.

Ambos incluyen `walletd.exe` y `bytecoind.exe` 3.5.1 de la [versión oficial de Bytecoin](https://github.com/bcndev/bytecoin/releases/tag/v3.5.1), sin cambios. `SHA256SUMS` contiene las sumas de comprobación de ambos archivos. No hace falta instalar nada más: .NET Framework 4.8 viene con Windows 10 y 11.

## Dos formas de conectarse

| | Sin la cadena de bloques (por defecto) | Nodo completo |
| --- | --- | --- |
| Cómo | walletd habla con un bytecoind remoto: [node.bytecoin.community](https://node.bytecoin.community/) o cualquier nodo que indique | el monedero inicia su propio `bytecoind` y walletd habla con él en `127.0.0.1` |
| Disco | unos 100 MB | unos 45 GB |
| Listo | en minutos | tras la primera sincronización: de muchas horas a unos días |
| Privacidad | las claves, el archivo del monedero y el escaneo se quedan en su PC; el nodo ve qué bloques descarga y qué transacciones envía | nada sale de su PC salvo el tráfico P2P normal |

Lo elige en la pantalla de inicio y puede cambiarlo en cualquier momento en Ajustes → Conexión. Si ya responde un `bytecoind` en `127.0.0.1:8081` (por ejemplo su propio nodo), el monedero lo usa en lugar de iniciar otro. Si el monedero oficial de Bytecoin ya descargó la cadena en `%APPDATA%\bytecoin`, el modo nodo completo ofrece esa carpeta y solo se descargan los bloques que faltan. Antes de cambiar, el monedero comprueba el espacio libre y si la unidad es SSD o HDD, y le indica cuánto puede tardar la primera sincronización (en un HDD, una semana o más).

### Nodos

Ajustes → Conexión muestra los nodos públicos con JSON-RPC abierto: el nodo de la comunidad `node.bytecoin.community` y los dos nodos oficiales de bcndev `node01/node02.bytecoin.org:8091`. Puede añadir los suyos (`host:puerto`, `http://…`, `https://…`). «Comprobar nodos» muestra para cada uno el tiempo de respuesta, la altura y si está en la misma cadena que los demás (se compara el hash de un bloque por debajo de la cima más baja; un nodo con otro hash se marca como «otra cadena: no usar»).

**Automático** (por defecto) elige al azar entre los nodos sanos que no son mucho más lentos que el más rápido, para que los monederos se repartan entre varios nodos en lugar de cargar uno solo, y cambia a otro nodo si el actual deja de responder durante 90 segundos.

### Una cadena de bloques que ya tiene

El modo nodo completo necesita una *carpeta de datos* de bytecoind, la que contiene la subcarpeta `blockchain`. El monedero oficial la guarda en `%APPDATA%\bytecoin`, y este monedero ofrece esa carpeta automáticamente. También puede copiar la carpeta de otro PC (detenga antes su bytecoind) y elegirla en Ajustes → Conexión. Guárdela en un SSD con NTFS, fuera de carpetas sincronizadas por OneDrive o Dropbox.

«Verificar la cadena de bloques» compara los hashes de bloques de su nodo en alturas repartidas por toda la cadena (los primeros bloques, cada 500 000 bloques y cerca de la cima) con todos los nodos públicos sanos. Si coinciden, su base de datos está en la misma cadena que la red; después bytecoind valida él mismo cada bloque nuevo. bytecoind 3.5.1 no vuelve a validar bloque a bloque una base copiada, así que para una independencia total descargue la cadena desde cero.

## Funciones

- Crear un monedero (frase mnemónica BIP39 de 24 palabras, comprobada volviendo a escribir 3 palabras), restaurarlo desde las palabras con una fecha opcional de «creado después de», abrir cualquier archivo `.wallet` e importar las claves de un monedero antiguo (direcciones que empiezan por `2`).
- Los demás tipos de monedero del monedero oficial: un monedero Amethyst de hardware (Ledger/Trezor con la aplicación de Bytecoin; experimental, aún sin probar con un dispositivo) y un monedero legacy con claves aleatorias.
- Saldo, progreso de sincronización y últimas transacciones; historial completo con búsqueda y filtros; detalles de cada transacción con enlace al [explorador](https://explorer.bytecoin.community).
- Envío con niveles de comisión (económico / normal / rápido, según la comisión recomendada por el nodo), nivel de anonimato 0–50 (tamaño del anillo − 1, 6 por defecto como en walletd), «enviar todo», ID de pago opcional para exchanges y pantalla de confirmación con la comisión exacta.
- Prueba de pago (sendproof): créela para una transacción saliente o compruebe una prueba que le hayan dado; útil en operaciones P2P y con exchanges.
- Recibir: dirección con código QR (opcionalmente con importe) y direcciones adicionales con etiquetas en monederos Amethyst.
- Libreta de direcciones (guardada localmente).
- Mostrar las 24 palabras (o las claves de un monedero antiguo) y copiarlas enteras: el portapapeles se borra a los 60 segundos; cambiar la contraseña; exportar una copia de solo lectura.
- Página de la comunidad: enlace a [BCN Miner](https://github.com/bytecoinBCN-community/bcn-miner) con «copiar mi dirección para minar» en un clic, el pool de la comunidad, el sitio, el explorador, el monedero web, Telegram y GitHub.
- Icono en la bandeja con avisos de pagos recibidos, inicio con Windows y opción de ocultar los importes.

![Pantalla de inicio](docs/screenshots/start-dark.png)

## Seguridad

- walletd cifra el archivo del monedero con su contraseña. Las contraseñas y la frase mnemónica se pasan a walletd por su entrada estándar (UTF-8), nunca por la línea de comandos, y la interfaz nunca las escribe en disco. Mientras el monedero está abierto, su contraseña permanece en la memoria del programa para poder reconectar tras cambiar los ajustes del nodo; «Cerrar monedero» la borra.
- walletd escucha solo en `127.0.0.1`, en un puerto libre aleatorio y con una contraseña HTTP aleatoria nueva en cada inicio.
- walletd y bytecoind se ejecutan dentro de un Job Object: si el monedero se cierra o falla, se detienen con él.
- La interfaz nunca envía sus claves a ningún sitio. En el modo sin cadena de bloques, el nodo remoto recibe las mismas peticiones que de cualquier monedero de Bytecoin; la privacidad de las transacciones (firmas de anillo, direcciones de un solo uso) la proporciona el propio Bytecoin.
- Si faltan walletd o bytecoind (compilaciones desde el código fuente), el monedero ofrece descargar el archivo oficial y solo lo instala si su SHA-256 coincide con el valor fijado en el código.

**Guarde las 24 palabras en papel.** La contraseña protege el archivo; las palabras son el dinero.

## Idiomas

Українська, English, Español, Português, Türkçe, Bahasa Indonesia, Tiếng Việt, 中文: el mismo conjunto que [bytecoin.community](https://bytecoin.community). Ajustes → Apariencia → Idioma, o los botones de la pantalla de inicio. «Sistema» sigue el idioma de Windows y, si no está disponible, usa inglés.

Las traducciones están en `src/lang/<código>.txt` como líneas `clave = texto` (`{0}` es un marcador y `\n` un salto de línea). Para corregir una traducción sin recompilar, ponga `<código>.txt` en una carpeta `lang` junto a `BCNWallet.exe`. El ucraniano y el inglés se escribieron a mano; los demás idiomas se tradujeron con ayuda de máquina, y las revisiones de hablantes nativos son bienvenidas.

![Tema claro](docs/screenshots/overview-light.png)

## Compilación

Solo se necesita Windows 10/11, sin herramientas de compilación:
```
powershell -ExecutionPolicy Bypass -File .\build.ps1
```
`dist\` recibe `BCNWallet.exe`, el README y la licencia. Ejecute `get-node.ps1` una vez para poner walletd y bytecoind en `node\` (se copian a `dist\node`), o deje que el monedero los descargue en el primer inicio.

```
powershell -ExecutionPolicy Bypass -File .\build.ps1 -Release
```
crea además `release\BCNWallet-<versión>-setup.exe`, `release\BCNWallet-<versión>-win-portable.zip` y `release\SHA256SUMS`.

El código es C# 5 para el compilador que viene con .NET Framework (`csc.exe`), WPF con la interfaz construida en código.

## Archivos

- `src/App.cs`: arranque, instancia única, UTF-8 para la entrada estándar de los demonios, tema e idioma.
- `src/Wallet.cs`: inicio y parada de walletd/bytecoind, llamadas JSON-RPC (`get_status`, `get_balance`, `get_transfers`, `create_transaction`, `send_transaction`, `get_wallet_records`, `create_addresses`), exportación, cambio de contraseña.
- `src/Node.cs`: procesos de los demonios, Job Object, descarga de los binarios oficiales con comprobación SHA-256.
- `src/Rpc.cs`: cliente JSON-RPC, formato y lectura de importes en BCN.
- `src/MainWindow.cs`, `src/Start.cs`, `src/Pages.cs`, `src/Pages2.cs`: ventana, pantalla de inicio, Resumen, Enviar, Recibir, Historial, Libreta, Comunidad, Ajustes.
- `src/Qr.cs`: codificador de códigos QR.
- `src/Theme.cs`, `src/Styles.xaml`, `src/Ui.cs`: el sistema de diseño del lienzo *Bytecoin Desktop Suite*, compartido con BCN Miner.
- `setup/Setup.cs`: el instalador y desinstalador.
- `get-node.ps1`: descarga de walletd y bytecoind con comprobación SHA-256.

## Agradecimientos

Los flujos (cómo se inicia walletd, cómo se pasan los secretos, qué métodos RPC se usan) siguen el [bytecoin-gui](https://github.com/bcndev/bytecoin-gui) oficial de bcndev, al que este monedero sustituye con una interfaz nueva en el estilo de bytecoin.community.

## Licencia

BCN Wallet se distribuye bajo la [GNU LGPL v3.0](LICENSE) (que complementa la [GNU GPL v3.0](COPYING)). `walletd.exe` y `bytecoind.exe` son © los desarrolladores de Bytecoin, LGPL-3.0, código fuente en <https://github.com/bcndev/bytecoin>.
