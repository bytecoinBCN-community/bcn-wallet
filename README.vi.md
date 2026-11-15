# BCN Wallet cho Windows

[English](README.md) · [Українська](README.uk.md) · [Español](README.es.md) · [Português](README.pt.md) · [Türkçe](README.tr.md) · [Bahasa Indonesia](README.id.md) · **Tiếng Việt** · [中文](README.zh.md)

Ví máy tính cho Bytecoin (BCN). Hoạt động **có hoặc không tải blockchain**, giữ khóa trên máy của bạn, có giao diện tối và sáng và tám ngôn ngữ. Có bản cài đặt và bản zip di động.

Dự án độc lập của cộng đồng Bytecoin, không liên quan đến những người sáng lập Bytecoin hay bcndev.

![Tổng quan](docs/screenshots/overview-dark.png)

## Tải về

Tại trang [Releases](../../releases):

- `BCNWallet-<phiên bản>-setup.exe` — chỉ cài cho người dùng của bạn (không cần quyền quản trị) vào `%LOCALAPPDATA%\Programs\BCN Wallet`, có lối tắt và gỡ cài đặt qua Cài đặt → Ứng dụng. Ví và cài đặt nằm trong `%APPDATA%\BCN Wallet` và không bao giờ bị động đến.
- `BCNWallet-<phiên bản>-win-portable.zip` — giải nén ở bất kỳ đâu (kể cả USB) và chạy `BCNWallet.exe`. Mọi thứ nằm trong thư mục `data` cạnh chương trình.

Cả hai đều có `walletd.exe` và `bytecoind.exe` 3.5.1 từ [bản phát hành chính thức của Bytecoin](https://github.com/bcndev/bytecoin/releases/tag/v3.5.1), không sửa đổi. Tổng kiểm tra nằm trong `SHA256SUMS`. Chỉ cần .NET Framework 4.8 có sẵn trong Windows 10 và 11.

## Hai cách kết nối

- **Không tải blockchain (mặc định):** walletd dùng bytecoind từ xa — [node.bytecoin.community](https://node.bytecoin.community/) hoặc nút khác bạn nhập. Khoảng 100 MB, sẵn sàng sau vài phút. Khóa và việc quét vẫn ở trên máy bạn.
- **Nút đầy đủ:** ví chạy `bytecoind` riêng và tải blockchain (khoảng 45 GB; lần đồng bộ đầu mất từ vài giờ đến vài ngày). Nếu đã có bytecoind tại `127.0.0.1:8081`, ví sẽ dùng nút đó.

Chọn ở màn hình bắt đầu; đổi trong Cài đặt → Kết nối.

## Tính năng

Tạo ví (24 từ có kiểm tra), khôi phục với ngày tùy chọn, mở tệp `.wallet`, nhập khóa ví cũ; số dư, lịch sử có tìm kiếm, gửi với các mức phí và Payment ID, nhận với mã QR và địa chỉ phụ, sổ địa chỉ, hiện 24 từ, đổi mật khẩu, bản sao chỉ xem; trang cộng đồng với [BCN Miner](https://github.com/bytecoinBCN-community/bcn-miner), pool, trang web, trình khám phá, ví web và Telegram.

## Bảo mật

Mật khẩu và cụm từ được chuyển cho walletd qua đầu vào chuẩn (UTF-8), không bao giờ qua dòng lệnh hay ghi ra đĩa. walletd chỉ lắng nghe trên `127.0.0.1` với cổng và mật khẩu HTTP ngẫu nhiên. **Hãy chép 24 từ ra giấy.**

## Biên dịch

```
powershell -ExecutionPolicy Bypass -File .\build.ps1 -Release
```
Chi tiết trong [README tiếng Anh](README.md#building).

## Giấy phép

[GNU LGPL v3.0](LICENSE). `walletd.exe` và `bytecoind.exe` — © The Bytecoin developers, LGPL-3.0.
