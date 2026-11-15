# BCN Wallet untuk Windows

[English](README.md) · [Українська](README.uk.md) · [Español](README.es.md) · [Português](README.pt.md) · [Türkçe](README.tr.md) · **Bahasa Indonesia** · [Tiếng Việt](README.vi.md) · [中文](README.zh.md)

Dompet desktop untuk Bytecoin (BCN). Bekerja **dengan atau tanpa mengunduh blockchain**, menyimpan kunci di komputer Anda, memiliki tema gelap dan terang serta antarmuka dalam delapan bahasa. Tersedia sebagai installer dan zip portabel.

Proyek independen komunitas Bytecoin, tidak terkait dengan pendiri Bytecoin atau bcndev.

![Ringkasan](docs/screenshots/overview-dark.png)

## Unduh

Di halaman [Releases](../../releases):

- `BCNWallet-<versi>-setup.exe` — dipasang hanya untuk pengguna Anda (tanpa hak administrator) di `%LOCALAPPDATA%\Programs\BCN Wallet`, dengan pintasan dan penghapusan lewat Pengaturan → Aplikasi. Dompet dan pengaturan tetap di `%APPDATA%\BCN Wallet` dan tidak pernah disentuh.
- `BCNWallet-<versi>-win-portable.zip` — ekstrak di mana saja (termasuk flashdisk) lalu jalankan `BCNWallet.exe`. Semuanya tersimpan di folder `data` di samping program.

Keduanya berisi `walletd.exe` dan `bytecoind.exe` 3.5.1 dari [rilis resmi Bytecoin](https://github.com/bcndev/bytecoin/releases/tag/v3.5.1) tanpa perubahan. Checksum ada di `SHA256SUMS`. Cukup .NET Framework 4.8 yang sudah ada di Windows 10 dan 11.

## Dua cara terhubung

- **Tanpa blockchain (bawaan):** walletd memakai bytecoind jarak jauh — [node.bytecoin.community](https://node.bytecoin.community/) atau node lain yang Anda masukkan. Sekitar 100 MB, siap dalam beberapa menit. Kunci dan pemindaian tetap di PC Anda.
- **Node penuh:** dompet menjalankan `bytecoind` sendiri dan mengunduh blockchain (sekitar 45 GB; sinkronisasi pertama butuh berjam-jam hingga berhari-hari). Jika sudah ada bytecoind di `127.0.0.1:8081`, node itu yang dipakai.

Dipilih di layar awal; diubah di Pengaturan → Koneksi.

## Fitur

Buat dompet (24 kata dengan verifikasi), pulihkan dengan tanggal opsional, buka file `.wallet`, impor kunci dompet lama; saldo, riwayat dengan pencarian, kirim dengan tingkat biaya dan Payment ID, terima dengan kode QR dan alamat tambahan, buku alamat, tampilkan 24 kata, ubah kata sandi, salinan hanya-lihat; halaman komunitas dengan [BCN Miner](https://github.com/bytecoinBCN-community/bcn-miner), pool, situs, explorer, dompet web, dan Telegram.

## Keamanan

Kata sandi dan frasa dikirim ke walletd lewat input standar (UTF-8), tidak pernah lewat baris perintah atau ke disk. walletd hanya mendengarkan di `127.0.0.1` dengan port dan kata sandi HTTP acak. **Tulis 24 kata di kertas.**

## Kompilasi

```
powershell -ExecutionPolicy Bypass -File .\build.ps1 -Release
```
Rincian di [README bahasa Inggris](README.md#building).

## Lisensi

[GNU LGPL v3.0](LICENSE). `walletd.exe` dan `bytecoind.exe` — © The Bytecoin developers, LGPL-3.0.
