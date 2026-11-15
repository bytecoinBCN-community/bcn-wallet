# BCN Wallet para Windows

[English](README.md) · [Українська](README.uk.md) · **Español** · [Português](README.pt.md) · [Türkçe](README.tr.md) · [Bahasa Indonesia](README.id.md) · [Tiếng Việt](README.vi.md) · [中文](README.zh.md)

Monedero de escritorio para Bytecoin (BCN). Funciona **con o sin descargar la blockchain**, guarda las claves en su ordenador, tiene tema oscuro y claro e interfaz en ocho idiomas. Se distribuye como instalador y como zip portátil.

Proyecto independiente de la comunidad Bytecoin, sin relación con los fundadores de Bytecoin ni con bcndev.

![Resumen](docs/screenshots/overview-dark.png)

## Descarga

En [Releases](../../releases):

- `BCNWallet-<versión>-setup.exe` — se instala solo para su usuario (sin permisos de administrador) en `%LOCALAPPDATA%\Programs\BCN Wallet`, con accesos directos y desinstalación desde Configuración → Aplicaciones. Monederos y ajustes quedan en `%APPDATA%\BCN Wallet` y nunca se tocan.
- `BCNWallet-<versión>-win-portable.zip` — descomprima en cualquier lugar (también un USB) y ejecute `BCNWallet.exe`. Todo queda en la carpeta `data` junto al programa.

Ambos incluyen `walletd.exe` y `bytecoind.exe` 3.5.1 de la [versión oficial de Bytecoin](https://github.com/bcndev/bytecoin/releases/tag/v3.5.1), sin cambios. Las sumas están en `SHA256SUMS`. Solo necesita .NET Framework 4.8, incluido en Windows 10 y 11.

## Dos formas de conectarse

- **Sin la blockchain (por defecto):** walletd usa un bytecoind remoto — [node.bytecoin.community](https://node.bytecoin.community/) u otro nodo que indique. Unos 100 MB, listo en minutos. Las claves y el escaneo se quedan en su PC.
- **Nodo completo:** el monedero ejecuta su propio `bytecoind` y descarga la blockchain (unos 45 GB; la primera sincronización tarda de horas a días). Si ya hay un bytecoind en `127.0.0.1:8081`, se usa ese.

Se elige en la pantalla de inicio y se cambia en Ajustes → Conexión.

## Funciones

Crear (24 palabras con verificación), restaurar con fecha opcional, abrir archivos `.wallet`, importar claves de monederos antiguos; saldo, historial con búsqueda, envío con niveles de comisión y Payment ID, recepción con código QR y direcciones extra, libreta de direcciones, mostrar las 24 palabras, cambiar contraseña, copia de solo lectura; página de comunidad con [BCN Miner](https://github.com/bytecoinBCN-community/bcn-miner), pool, sitio, explorador, monedero web y Telegram.

## Seguridad

Las contraseñas y la frase pasan a walletd por la entrada estándar (UTF-8), nunca por la línea de comandos ni al disco. walletd escucha solo en `127.0.0.1` con un puerto y una contraseña HTTP aleatorios. **Anote las 24 palabras en papel.**

## Compilación

```
powershell -ExecutionPolicy Bypass -File .\build.ps1 -Release
```
Detalles en el [README en inglés](README.md#building).

## Licencia

[GNU LGPL v3.0](LICENSE). `walletd.exe` y `bytecoind.exe` — © The Bytecoin developers, LGPL-3.0.
