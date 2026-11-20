# BCN Wallet 1.1.0

## New: iOS · Android · Web

A new page in the menu explains how to move the wallet to [Amethyst Web Wallet](https://web.bytecoin.community) — in a browser, or installed as an app on iPhone and Android — and does it:

- **Save .amwbackup file:** the 24 words, creation date, address count and the state of the synced wallet at block *top − 10* (balance, unspent outputs, history, first payment), encrypted with a separate backup password (AES-256-GCM, PBKDF2-HMAC-SHA256 600 000 iterations). The phone restores from it in seconds instead of scanning from the creation date.
- **Restore from .amwbackup file** (start screen, or the page's button): the way back, from a phone or the web wallet to this PC.
- Install steps for iPhone/iPad (Safari → Add to Home Screen), Android (Chrome → Install app) and desktop browsers, with a QR code that opens the web wallet on the phone.

Format: `docs/amwbackup-format.md` of Amethyst Web Wallet, version 2. Checked against the format's test vector and decrypted independently with browser Web Crypto.

Only amethyst wallets (24 words, `bcnZ…`) can be saved this way. Legacy, Ledger and view-only wallets are refused, and so is a wallet whose words have a BIP39 passphrase (walletd does not report it, so the file would restore a different wallet).

The web wallet reads version 2 files from its build with service worker v18 on; older deployed builds read only version 1.
