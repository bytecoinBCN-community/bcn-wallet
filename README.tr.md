# Windows için BCN Wallet

[English](README.md) · [Українська](README.uk.md) · [Español](README.es.md) · [Português](README.pt.md) · **Türkçe** · [Bahasa Indonesia](README.id.md) · [Tiếng Việt](README.vi.md) · [中文](README.zh.md)

Bytecoin (BCN) için bir masaüstü cüzdanı. **Blok zinciri indirilerek ya da indirilmeden** çalışır, anahtarlarınızı bilgisayarınızda tutar, koyu ve açık temaları ve sekiz dilde arayüzü vardır. Kurulum dosyası ve taşınabilir zip olarak gelir.

Bu, Bytecoin topluluğunun bağımsız bir projesidir; Bytecoin kurucuları ve bcndev ile bağlantısı yoktur.

![Genel bakış](docs/screenshots/overview-dark.png)

## İndirme

[bytecoin.community](https://bytecoin.community/#downloads) adresinden:

- `BCNWallet-<sürüm>-setup.exe`: yalnızca sizin kullanıcınız için (yönetici hakkı olmadan) `%LOCALAPPDATA%\Programs\BCN Wallet` klasörüne kurulur, Başlat menüsüne ve masaüstüne kısayol, kaldırmak için de Ayarlar → Uygulamalar'a bir kayıt ekler. Cüzdanlar ve ayarlar `%APPDATA%\BCN Wallet` içinde tutulur; kurulum, güncelleme ve kaldırma onlara hiç dokunmaz.
- `BCNWallet-<sürüm>-win-portable.zip`: istediğiniz yere, bir USB belleğe bile açın ve `BCNWallet.exe`'yi çalıştırın. Her şey (ayarlar, cüzdan dosyaları, önbellek ve tam düğüm modunda blok zinciri) programın yanındaki `data` klasöründe kalır. **Bu klasörde cüzdanlarınız var:** klasörü siler ya da değiştirirseniz, bir güncellemeyi üzerine açarsanız veya programı doğrudan zip'ten başlatırsanız onları kaybedersiniz. Önce 24 kelimeyi yazın, klasörü İndirilenler'de, geçici klasörlerde ve buluta eşitlenen klasörlerde tutmayın, cüzdan kapalıyken yedekleyin.

İkisi de [resmi Bytecoin sürümündeki](https://github.com/bcndev/bytecoin/releases/tag/v3.5.1) `walletd.exe` ve `bytecoind.exe` 3.5.1'i değiştirilmeden içerir. `SHA256SUMS` iki dosyanın sağlama toplamlarını listeler. Başka bir şey kurmanız gerekmez: .NET Framework 4.8, Windows 10 ve 11 ile gelir.

## İki bağlantı yolu

| | Blok zinciri olmadan (varsayılan) | Tam düğüm |
| --- | --- | --- |
| Nasıl | walletd uzak bir bytecoind ile konuşur: [node.bytecoin.community](https://node.bytecoin.community/) ya da girdiğiniz herhangi bir düğüm | cüzdan kendi `bytecoind`'sini başlatır, walletd onunla `127.0.0.1` üzerinden konuşur |
| Disk | yaklaşık 100 MB | yaklaşık 45 GB |
| Hazır | dakikalar içinde | ilk eşitlemeden sonra: birçok saatten birkaç güne |
| Gizlilik | anahtarlar, cüzdan dosyası ve tarama bilgisayarınızda kalır; düğüm hangi blokları indirdiğinizi ve hangi işlemleri gönderdiğinizi görür | normal P2P trafiği dışında bilgisayarınızdan hiçbir şey çıkmaz |

Seçimi başlangıç ekranında yaparsınız, istediğiniz zaman Ayarlar → Bağlantı'dan değiştirebilirsiniz. `127.0.0.1:8081` üzerinde zaten bir `bytecoind` yanıt veriyorsa (örneğin kendi düğümünüz), cüzdan ikincisini başlatmak yerine onu kullanır. Resmi Bytecoin cüzdanı blok zincirini zaten `%APPDATA%\bytecoin` içine indirdiyse, tam düğüm modu bu klasörü önerir ve yalnızca eksik bloklar indirilir. Geçmeden önce cüzdan boş alanı ve sürücünün SSD mi HDD mi olduğunu kontrol eder, ilk eşitlemenin ne kadar sürebileceğini söyler (HDD'de bir hafta veya daha fazla).

### Düğümler

Ayarlar → Bağlantı, JSON-RPC'si açık herkese açık düğümleri listeler: topluluk düğümü `node.bytecoin.community` ve bcndev'in iki resmi düğümü `node01/node02.bytecoin.org:8091`. Kendinizinkini de ekleyebilirsiniz (`host:port`, `http://…`, `https://…`). «Düğümleri kontrol et» her biri için yanıt süresini, yüksekliği ve diğerleriyle aynı zincirde olup olmadığını gösterir (en düşük tepenin altındaki bir bloğun hash'i karşılaştırılır; başka hash'e sahip düğüm «başka zincir — kullanmayın» olarak işaretlenir).

**Otomatik** (varsayılan), en hızlısından çok yavaş olmayan sağlıklı düğümler arasından rastgele seçer; böylece cüzdanlar tek bir düğüme yüklenmek yerine birkaç düğüme dağılır. Mevcut düğüm 90 saniye yanıt vermezse başka bir düğüme geçer.

### Elinizdeki bir blok zinciri

Tam düğüm modu bir bytecoind *veri klasörüne*, yani `blockchain` alt klasörünü içeren klasöre ihtiyaç duyar. Resmi cüzdan onu `%APPDATA%\bytecoin` içinde tutar ve bu cüzdan o klasörü otomatik olarak önerir. Klasörü başka bir bilgisayardan da kopyalayabilir (önce oradaki bytecoind'yi durdurun) ve Ayarlar → Bağlantı'dan seçebilirsiniz. Onu NTFS biçimli bir SSD'de, OneDrive/Dropbox'ın eşitlediği klasörlerin dışında tutun.

«Blok zincirini doğrula», düğümünüzün blok hash'lerini zincirin tamamına yayılmış yüksekliklerde (ilk bloklar, her 500 000 blokta bir ve tepeye yakın) tüm sağlıklı herkese açık düğümlerle karşılaştırır. Hash'ler eşleşiyorsa veritabanınız ağla aynı zincirdedir; bundan sonra bytecoind her yeni bloğu kendisi doğrular. bytecoind 3.5.1 kopyalanmış bir veritabanını blok blok yeniden doğrulamaz, bu yüzden tam bağımsızlık için zinciri sıfırdan indirin.

## Neler yapar

- Cüzdan oluşturma (24 kelimelik BIP39 anımsatıcı, 3 kelime yeniden yazılarak kontrol edilir), isteğe bağlı «şu tarihten sonra oluşturuldu» tarihiyle kelimelerden geri yükleme, herhangi bir `.wallet` dosyasını açma, eski bir cüzdanın anahtarlarını içe aktarma (`2` ile başlayan adresler).
- Resmi cüzdanın diğer cüzdan türleri: donanım Amethyst cüzdanı (Bytecoin uygulamalı Ledger/Trezor; deneysel, henüz bir cihazla test edilmedi) ve rastgele anahtarlı legacy cüzdan.
- Bakiye, eşitleme ilerlemesi, son işlemler; arama ve filtrelerle tam geçmiş; [gezgine](https://explorer.bytecoin.community) bağlantılı işlem ayrıntıları.
- Ücret seviyeleriyle gönderme (ekonomik / normal / hızlı, düğümün önerdiği ücrete göre), 0–50 anonimlik seviyesi (halka boyutu − 1, walletd'deki gibi varsayılan 6), «tümünü gönder», borsalar için isteğe bağlı ödeme kimliği, tam ücretli onay ekranı.
- Ödeme kanıtı (sendproof): giden bir işlem için oluşturun ya da size verilen bir kanıtı kontrol edin; P2P anlaşmalarda ve borsalarda işe yarar.
- Alma: QR kodlu adres (isteğe bağlı tutarla), Amethyst cüzdanlarında etiketli ek adresler.
- Adres defteri (yerel olarak saklanır).
- 24 kelimeyi (ya da eski bir cüzdanın anahtarlarını) gösterme ve tamamını kopyalama: pano 60 saniye sonra temizlenir; parola değiştirme; salt görüntüleme kopyası dışa aktarma.
- Topluluk sayfası: tek tıkla «madencilik için adresimi kopyala» ile [BCN Miner](https://github.com/bytecoinBCN-community/bcn-miner) bağlantısı, topluluk havuzu, site, gezgin, web cüzdanı, Telegram ve GitHub.
- Gelen ödeme bildirimleriyle sistem tepsisi simgesi, Windows ile başlatma, tutarları gizleme.

![Başlangıç ekranı](docs/screenshots/start-dark.png)

## Güvenlik

- Cüzdan dosyası walletd tarafından parolanızla şifrelenir. Parolalar ve anımsatıcı walletd'ye standart girdisi (UTF-8) üzerinden aktarılır, asla komut satırında değil, ve arayüz bunları asla diske yazmaz. Bir cüzdan açıkken, düğüm ayarlarını değiştirdikten sonra yeniden bağlanabilmek için parolası programın belleğinde kalır; «Cüzdanı kapat» onu siler.
- walletd yalnızca `127.0.0.1` üzerinde, rastgele boş bir portta ve her başlangıçta yeni rastgele bir HTTP parolasıyla dinler.
- walletd ve bytecoind bir Job Object içinde çalışır: cüzdan kapanır ya da çökerse onlar da durur.
- Arayüz anahtarlarınızı hiçbir yere göndermez. Blok zinciri olmadan çalışan modda uzak düğüm, herhangi bir Bytecoin cüzdanından gelenlerle aynı istekleri alır; işlem gizliliğini (halka imzaları, tek kullanımlık adresler) Bytecoin'in kendisi sağlar.
- walletd/bytecoind eksikse (kaynaktan derlemelerde), cüzdan resmi arşivi indirmeyi önerir ve yalnızca SHA-256'sı kodda sabitlenmiş değerle eşleşirse kurar.

**24 kelimeyi kâğıda yazın.** Parola dosyayı korur; kelimeler ise paranın kendisidir.

## Diller

Українська, English, Español, Português, Türkçe, Bahasa Indonesia, Tiếng Việt, 中文: [bytecoin.community](https://bytecoin.community) ile aynı set. Ayarlar → Görünüm → Dil ya da başlangıç ekranındaki düğmeler. «Sistem», Windows arayüz dilini izler; yoksa İngilizceye döner.

Çeviriler `src/lang/<kod>.txt` içinde `anahtar = metin` satırları olarak durur (`{0}` bir yer tutucu, `\n` satır sonu). Yeniden derlemeden bir çeviriyi düzeltmek için `<kod>.txt` dosyasını `BCNWallet.exe`'nin yanındaki `lang` klasörüne koyun. Ukraynaca ve İngilizce elle yazıldı; diğer diller makine desteğiyle çevrildi, ana dili konuşanların düzeltmeleri memnuniyetle karşılanır.

![Açık tema](docs/screenshots/overview-light.png)

## Derleme

Yalnızca Windows 10/11 gerekir, derleme araçları gerekmez:
```
powershell -ExecutionPolicy Bypass -File .\build.ps1
```
`dist\` klasörüne `BCNWallet.exe`, README ve lisans gelir. walletd ve bytecoind'yi `node\` içine koymak için `get-node.ps1`'i bir kez çalıştırın (`dist\node` içine kopyalanırlar) ya da cüzdanın onları ilk başlangıçta indirmesine izin verin.

```
powershell -ExecutionPolicy Bypass -File .\build.ps1 -Release
```
ayrıca `release\BCNWallet-<sürüm>-setup.exe`, `release\BCNWallet-<sürüm>-win-portable.zip` ve `release\SHA256SUMS` oluşturur.

Kod, .NET Framework ile gelen derleyici (`csc.exe`) için C# 5'tir; arayüzü kodla kurulmuş WPF'dir.

## Dosyalar

- `src/App.cs`: başlatma, tek örnek, arka plan programlarının standart girdisi için UTF-8, tema ve dil.
- `src/Wallet.cs`: walletd/bytecoind başlatma ve durdurma, JSON-RPC çağrıları (`get_status`, `get_balance`, `get_transfers`, `create_transaction`, `send_transaction`, `get_wallet_records`, `create_addresses`), dışa aktarma, parola değiştirme.
- `src/Node.cs`: arka plan süreçleri, Job Object, resmi ikili dosyaların SHA-256 kontrollü indirilmesi.
- `src/Rpc.cs`: JSON-RPC istemcisi, BCN tutarlarının biçimlendirilmesi ve okunması.
- `src/MainWindow.cs`, `src/Start.cs`, `src/Pages.cs`, `src/Pages2.cs`: pencere, başlangıç ekranı, Genel bakış, Gönder, Al, Geçmiş, Adres defteri, Topluluk, Ayarlar.
- `src/Qr.cs`: QR kodu kodlayıcısı.
- `src/Theme.cs`, `src/Styles.xaml`, `src/Ui.cs`: *Bytecoin Desktop Suite* tuvalindeki tasarım sistemi, BCN Miner ile ortak.
- `setup/Setup.cs`: kurulum ve kaldırma programı.
- `get-node.ps1`: walletd ve bytecoind'nin SHA-256 kontrollü indirilmesi.

## Teşekkürler

Akışlar (walletd'nin nasıl başlatıldığı, gizli bilgilerin nasıl aktarıldığı, hangi RPC yöntemlerinin kullanıldığı) bcndev'in resmi [bytecoin-gui](https://github.com/bcndev/bytecoin-gui) projesini izler; bu cüzdan onu bytecoin.community tarzında yeni bir arayüzle değiştirir.

## Lisans

BCN Wallet, [GNU LGPL v3.0](LICENSE) (bu da [GNU GPL v3.0](COPYING)'ı tamamlar) ile lisanslanmıştır. `walletd.exe` ve `bytecoind.exe` © Bytecoin geliştiricileri, LGPL-3.0, kaynak kod <https://github.com/bcndev/bytecoin> adresinde.
