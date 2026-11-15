# BCN Wallet for Windows

**English** · [Українська](README.uk.md) · [Español](README.es.md) · [Português](README.pt.md) · [Türkçe](README.tr.md) · [Bahasa Indonesia](README.id.md) · [Tiếng Việt](README.vi.md) · [中文](README.zh.md)

A desktop wallet for Bytecoin (BCN). It works **with or without downloading the blockchain**, keeps your keys on your computer, has dark and light themes and an interface in eight languages. It comes as an installer and as a portable zip.

This is an independent Bytecoin community project, not affiliated with the Bytecoin founders or bcndev.

![Overview](docs/screenshots/overview-dark.png)

## Download

From [bytecoin.community](https://bytecoin.community/#downloads):

- `BCNWallet-<version>-setup.exe` — installs for your user only (no administrator rights) into `%LOCALAPPDATA%\Programs\BCN Wallet`, adds Start menu and desktop shortcuts and an entry in Settings → Apps for uninstalling. Wallets and settings are kept in `%APPDATA%\BCN Wallet`; install, update and uninstall never touch them.
- `BCNWallet-<version>-win-portable.zip` — unpack anywhere, a USB stick included, and run `BCNWallet.exe`. Everything (settings, wallet files, cache, and the blockchain in full-node mode) stays in the `data` folder next to it. **That folder holds your wallets:** deleting or replacing it, unpacking an update on top of it, or starting the program straight from the zip loses them. Write down the 24 words first, keep the folder out of Downloads, temporary and cloud-synced folders, and back it up while the wallet is closed.

Both contain `walletd.exe` and `bytecoind.exe` 3.5.1 from the [official Bytecoin release](https://github.com/bcndev/bytecoin/releases/tag/v3.5.1), unchanged. `SHA256SUMS` lists the checksums of both files. Nothing else has to be installed: .NET Framework 4.8 ships with Windows 10 and 11.

## Two ways to connect

| | Without the blockchain (default) | Full node |
| --- | --- | --- |
| How | walletd talks to a remote bytecoind: [node.bytecoin.community](https://node.bytecoin.community/) or any node you enter | the wallet starts its own `bytecoind` and walletd talks to it on `127.0.0.1` |
| Disk | about 100 MB | about 45 GB |
| Ready | in minutes | after the first sync: many hours to a few days |
| Privacy | keys, the wallet file and the scanning stay on your PC; the node sees which blocks you download and the transactions you broadcast | nothing leaves your PC except normal P2P traffic |

You choose on the start screen and can switch any time in Settings → Connection. If a `bytecoind` already answers on `127.0.0.1:8081` (for example your own node), the wallet uses it instead of starting a second one. If the official Bytecoin wallet has already downloaded the blockchain into `%APPDATA%\bytecoin`, full-node mode offers that folder, so only the missing blocks are fetched. Before switching, the wallet checks free space, whether the drive is an SSD or an HDD, and tells you how long the first sync may take (on an HDD a week or more).

### Nodes

Settings → Connection lists the public nodes with an open JSON-RPC: the community node `node.bytecoin.community` and the two official bcndev nodes `node01/node02.bytecoin.org:8091`. You can add your own (`host:port`, `http://…`, `https://…`). "Check nodes" shows for each one the response time, the height and whether it is on the same chain as the others (the hash of one block below the lowest top is compared; a node with another hash is marked "another chain — do not use").

**Automatic** (the default) picks at random among healthy nodes that are not much slower than the fastest — so wallets spread over several nodes instead of all hitting one — and switches to another node when the current one stops answering for 90 seconds.

### A blockchain you already have

Full-node mode needs a bytecoind *data folder* — the folder that contains the `blockchain` subfolder. The official wallet keeps it in `%APPDATA%\bytecoin`, and the wallet offers that folder automatically. You can also copy the folder from another PC (stop its bytecoind first) and choose it in Settings → Connection. Keep it on an SSD with NTFS, outside folders synced by OneDrive/Dropbox.

"Verify the blockchain" compares your node's block hashes at heights spread over the whole chain (the first blocks, every 500 000 blocks, and near the top) with all healthy public nodes. Matching hashes mean your database is on the same chain as the network; after that bytecoind validates every new block itself. A copied database is not re-validated block by block by bytecoind 3.5.1, so for full independence download the chain from scratch.

## What it does

- Create a wallet (24-word BIP39 mnemonic, checked by retyping 3 words), restore from the words with an optional "created after" date, open any `.wallet` file, import keys of an old wallet (addresses starting with `2`).
- The other wallet types of the official wallet: a hardware Amethyst wallet (Ledger/Trezor with the Bytecoin app; experimental, not yet tested with a device) and a legacy wallet with random keys.
- Balance, synchronization progress, recent transactions; full history with search and filters; transaction details with a link to the [explorer](https://explorer.bytecoin.community).
- Send with fee levels (economy / normal / fast, based on the node's recommended fee), anonymity level 0–50 (ring size − 1, default 6 as in walletd), "send all", optional payment ID for exchanges, confirmation screen with the exact fee.
- Proof of payment (sendproof): create one for an outgoing transaction, or check a proof someone gave you — useful for P2P deals and exchanges.
- Receive: address with a QR code (optionally with an amount), extra addresses with labels for Amethyst wallets.
- Address book (stored locally).
- Show the 24 words (or the keys of an old wallet) and copy them whole — the clipboard is cleared after 60 seconds; change the password, export a view-only copy.
- Community page: link to [BCN Miner](https://github.com/bytecoinBCN-community/bcn-miner) with one-click "copy my address for mining", the community pool, site, explorer, web wallet, Telegram and GitHub.
- Tray icon with notifications about incoming payments, start with Windows, hide amounts.

![Start screen](docs/screenshots/start-dark.png)

## Security

- The wallet file is encrypted by walletd with your password. Passwords and the mnemonic are passed to walletd through its standard input (UTF-8), never on the command line, and are never written to disk by the GUI. While a wallet is open, its password stays in the program's memory so it can reconnect after you change node settings; "Close wallet" clears it.
- walletd listens only on `127.0.0.1`, on a free random port, with a new random HTTP password every start.
- walletd and bytecoind run in a Job Object: if the wallet closes or crashes, they stop with it.
- The GUI never sends your keys anywhere. In mode without the blockchain the remote node gets the same requests as from any Bytecoin wallet; transaction privacy (ring signatures, one-time addresses) is provided by Bytecoin itself.
- When walletd/bytecoind are missing (builds from source), the wallet offers to download the official archive and installs it only if its SHA-256 matches the value pinned in the code.

**Back up the 24 words on paper.** The password protects the file; the words are the money.

## Languages

Українська, English, Español, Português, Türkçe, Bahasa Indonesia, Tiếng Việt, 中文 — the same set as [bytecoin.community](https://bytecoin.community). Settings → Appearance → Language, or the buttons on the start screen. "System" follows the Windows UI language and falls back to English.

Translations live in `src/lang/<code>.txt` as `key = text` lines (`{0}` is a placeholder, `\n` a line break). To fix a translation without rebuilding, put `<code>.txt` into a `lang` folder next to `BCNWallet.exe`. Ukrainian and English were written by hand; the others are machine-assisted, and reviews by native speakers are welcome.

![Light theme](docs/screenshots/overview-light.png)

## Building

Only Windows 10/11 is needed, no build tools:
```
powershell -ExecutionPolicy Bypass -File .\build.ps1
```
`dist\` receives `BCNWallet.exe`, README and LICENSE. Run `get-node.ps1` once to put walletd and bytecoind into `node\` (they are copied to `dist\node`), or let the wallet download them on first start.

```
powershell -ExecutionPolicy Bypass -File .\build.ps1 -Release
```
also makes `release\BCNWallet-<version>-setup.exe`, `release\BCNWallet-<version>-win-portable.zip` and `release\SHA256SUMS`.

The code is C# 5 for the compiler that ships with .NET Framework (`csc.exe`), WPF with the UI built in code.

## Files

- `src/App.cs` — startup, single instance, UTF-8 for the daemons' stdin, theme and language.
- `src/Wallet.cs` — walletd/bytecoind start and stop, JSON-RPC calls (`get_status`, `get_balance`, `get_transfers`, `create_transaction`, `send_transaction`, `get_wallet_records`, `create_addresses`), export, password change.
- `src/Node.cs` — daemon processes, Job Object, download of the official binaries with SHA-256 check.
- `src/Rpc.cs` — JSON-RPC client, BCN amount formatting and parsing.
- `src/MainWindow.cs`, `src/Start.cs`, `src/Pages.cs`, `src/Pages2.cs` — window, start screen, Overview, Send, Receive, History, Address book, Community, Settings.
- `src/Qr.cs` — QR code encoder.
- `src/Theme.cs`, `src/Styles.xaml`, `src/Ui.cs` — the design system from the *Bytecoin Desktop Suite* canvas, shared with BCN Miner.
- `setup/Setup.cs` — the installer and uninstaller.
- `get-node.ps1` — download of walletd and bytecoind with SHA-256 check.

## Thanks

The flows (how walletd is started, how secrets are passed, which RPC methods are used) follow the official [bytecoin-gui](https://github.com/bcndev/bytecoin-gui) by bcndev, which this wallet replaces with a new interface in the style of bytecoin.community.

## License

BCN Wallet is licensed under the [GNU LGPL v3.0](LICENSE) (which supplements the [GNU GPL v3.0](COPYING)). `walletd.exe` and `bytecoind.exe` are © The Bytecoin developers, LGPL-3.0, source at <https://github.com/bcndev/bytecoin>.
