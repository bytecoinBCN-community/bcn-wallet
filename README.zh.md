# BCN Wallet（Windows 版）

[English](README.md) · [Українська](README.uk.md) · [Español](README.es.md) · [Português](README.pt.md) · [Türkçe](README.tr.md) · [Bahasa Indonesia](README.id.md) · [Tiếng Việt](README.vi.md) · **中文**

Bytecoin（BCN）桌面钱包。**可以下载区块链，也可以不下载**；密钥保存在您的电脑上；提供深色和浅色主题以及八种语言界面。提供安装程序和便携版 zip。

这是 Bytecoin 社区的独立项目，与 Bytecoin 创始人或 bcndev 无关。

![概览](docs/screenshots/overview-dark.png)

## 下载

在 [Releases](../../releases) 页面：

- `BCNWallet-<版本>-setup.exe` —— 仅为当前用户安装（无需管理员权限）到 `%LOCALAPPDATA%\Programs\BCN Wallet`，创建快捷方式，可在“设置 → 应用”中卸载。钱包和设置保存在 `%APPDATA%\BCN Wallet`，安装、更新和卸载都不会改动它们。
- `BCNWallet-<版本>-win-portable.zip` —— 解压到任意位置（包括 U 盘），运行 `BCNWallet.exe`。所有内容都保存在程序旁边的 `data` 文件夹中。

两者都包含来自 [Bytecoin 官方发布](https://github.com/bcndev/bytecoin/releases/tag/v3.5.1) 的未修改的 `walletd.exe` 和 `bytecoind.exe` 3.5.1。校验和见 `SHA256SUMS`。只需要 Windows 10 和 11 自带的 .NET Framework 4.8。

## 两种连接方式

- **不下载区块链（默认）：** walletd 使用远程 bytecoind —— [node.bytecoin.community](https://node.bytecoin.community/) 或您填写的其他节点。约 100 MB，几分钟即可使用。密钥和扫描都留在您的电脑上。
- **完整节点：** 钱包运行自己的 `bytecoind` 并下载区块链（约 45 GB；首次同步需要数小时到数天）。如果 `127.0.0.1:8081` 上已有 bytecoind，则直接使用它。

在开始页面选择；可在“设置 → 连接”中更改。

## 功能

创建钱包（24 个单词并验证）、按可选日期恢复、打开 `.wallet` 文件、导入旧钱包密钥；余额、可搜索的历史记录、按手续费等级和 Payment ID 发送、带二维码和附加地址的接收、地址簿、显示 24 个单词、修改密码、仅查看副本；社区页面提供 [BCN Miner](https://github.com/bytecoinBCN-community/bcn-miner)、矿池、网站、区块浏览器、网页钱包和 Telegram 链接。

## 安全

密码和助记词通过标准输入（UTF-8）传给 walletd，绝不出现在命令行中，也不会写入磁盘。walletd 只在 `127.0.0.1` 上监听，使用随机端口和随机 HTTP 密码。**请把 24 个单词抄在纸上。**

## 构建

```
powershell -ExecutionPolicy Bypass -File .\build.ps1 -Release
```
详情见 [英文 README](README.md#building)。

## 许可证

[GNU LGPL v3.0](LICENSE)。`walletd.exe` 和 `bytecoind.exe` —— © The Bytecoin developers，LGPL-3.0。
