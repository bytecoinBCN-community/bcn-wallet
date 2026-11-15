# Windows için BCN Wallet

[English](README.md) · [Українська](README.uk.md) · [Español](README.es.md) · [Português](README.pt.md) · **Türkçe** · [Bahasa Indonesia](README.id.md) · [Tiếng Việt](README.vi.md) · [中文](README.zh.md)

Bytecoin (BCN) için masaüstü cüzdanı. **Blok zincirini indirerek veya indirmeden** çalışır, anahtarları bilgisayarınızda tutar, koyu ve açık temalara ve sekiz dilde arayüze sahiptir. Kurulum dosyası ve taşınabilir zip olarak sunulur.

Bytecoin kurucuları veya bcndev ile bağlantısı olmayan bağımsız bir Bytecoin topluluk projesidir.

![Genel bakış](docs/screenshots/overview-dark.png)

## İndirme

[Releases](../../releases) sayfasından:

- `BCNWallet-<sürüm>-setup.exe` — yalnızca sizin kullanıcınız için (yönetici hakkı olmadan) `%LOCALAPPDATA%\Programs\BCN Wallet` içine kurar, kısayollar ekler ve Ayarlar → Uygulamalar'dan kaldırılabilir. Cüzdanlar ve ayarlar `%APPDATA%\BCN Wallet` içinde kalır ve hiç dokunulmaz.
- `BCNWallet-<sürüm>-win-portable.zip` — istediğiniz yere (USB bellek dahil) açın ve `BCNWallet.exe`'yi çalıştırın. Her şey programın yanındaki `data` klasöründe kalır.

İkisi de [resmi Bytecoin sürümündeki](https://github.com/bcndev/bytecoin/releases/tag/v3.5.1) değiştirilmemiş `walletd.exe` ve `bytecoind.exe` 3.5.1'i içerir. Sağlama toplamları `SHA256SUMS` içindedir. Yalnızca Windows 10 ve 11'de bulunan .NET Framework 4.8 gerekir.

## İki bağlantı yolu

- **Blok zinciri olmadan (varsayılan):** walletd uzak bir bytecoind kullanır — [node.bytecoin.community](https://node.bytecoin.community/) veya girdiğiniz başka bir düğüm. Yaklaşık 100 MB, dakikalar içinde hazır. Anahtarlar ve tarama bilgisayarınızda kalır.
- **Tam düğüm:** cüzdan kendi `bytecoind`'sini çalıştırır ve blok zincirini indirir (yaklaşık 45 GB; ilk eşitleme saatler ile günler arası sürer). `127.0.0.1:8081` üzerinde zaten bir bytecoind varsa o kullanılır.

Başlangıç ekranında seçilir; Ayarlar → Bağlantı'dan değiştirilir.

## Özellikler

Oluşturma (doğrulamalı 24 kelime), isteğe bağlı tarihle geri yükleme, `.wallet` dosyası açma, eski cüzdan anahtarlarını içe aktarma; bakiye, aramalı geçmiş, ücret seviyeleri ve Payment ID ile gönderme, QR kodlu alma ve ek adresler, adres defteri, 24 kelimeyi gösterme, parola değiştirme, yalnızca görüntüleme kopyası; [BCN Miner](https://github.com/bytecoinBCN-community/bcn-miner), havuz, site, gezgin, web cüzdanı ve Telegram bağlantılı topluluk sayfası.

## Güvenlik

Parolalar ve kelimeler walletd'ye standart girdiden (UTF-8) iletilir; asla komut satırına veya diske yazılmaz. walletd yalnızca `127.0.0.1` üzerinde, rastgele port ve HTTP parolasıyla dinler. **24 kelimeyi kâğıda yazın.**

## Derleme

```
powershell -ExecutionPolicy Bypass -File .\build.ps1 -Release
```
Ayrıntılar [İngilizce README](README.md#building) içinde.

## Lisans

[GNU LGPL v3.0](LICENSE). `walletd.exe` ve `bytecoind.exe` — © The Bytecoin developers, LGPL-3.0.
