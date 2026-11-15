# BCN Wallet（Windows 版）

[English](README.md) · [Українська](README.uk.md) · [Español](README.es.md) · [Português](README.pt.md) · [Türkçe](README.tr.md) · [Bahasa Indonesia](README.id.md) · [Tiếng Việt](README.vi.md) · **中文**

Bytecoin（BCN）桌面钱包。**可下载区块链，也可不下载**，密钥只保存在你的电脑上，有深色和浅色主题，界面支持八种语言。提供安装版和便携 zip。

这是 Bytecoin 社区的独立项目，与 Bytecoin 创始人及 bcndev 无关。

![总览](docs/screenshots/overview-dark.png)

## 下载

在 [bytecoin.community](https://bytecoin.community/#downloads)：

- `BCNWallet-<版本>-setup.exe`：仅为当前用户安装（无需管理员权限）到 `%LOCALAPPDATA%\Programs\BCN Wallet`，在开始菜单和桌面创建快捷方式，并在“设置 → 应用”中添加卸载项。钱包和设置保存在 `%APPDATA%\BCN Wallet`，安装、更新和卸载都不会动它们。
- `BCNWallet-<版本>-win-portable.zip`：解压到任意位置（包括 U 盘），运行 `BCNWallet.exe`。所有内容（设置、钱包文件、缓存，以及全节点模式下的区块链）都保存在程序旁边的 `data` 文件夹中。**这个文件夹里就是你的钱包：**如果删除或替换它、把更新解压覆盖到它上面，或者直接从 zip 中运行程序，钱包就会丢失。请先抄下 24 个单词，不要把该文件夹放在“下载”、临时文件夹或云同步文件夹中，并在钱包关闭时备份它。

两者都包含来自 [Bytecoin 官方发布](https://github.com/bcndev/bytecoin/releases/tag/v3.5.1) 的未修改的 `walletd.exe` 和 `bytecoind.exe` 3.5.1。`SHA256SUMS` 列出了两个文件的校验和。无需安装其他东西：Windows 10 和 11 自带 .NET Framework 4.8。

## 两种连接方式

| | 不下载区块链（默认） | 全节点 |
| --- | --- | --- |
| 方式 | walletd 连接远程 bytecoind：[node.bytecoin.community](https://node.bytecoin.community/) 或你填写的任意节点 | 钱包启动自己的 `bytecoind`，walletd 通过 `127.0.0.1` 与之通信 |
| 磁盘 | 约 100 MB | 约 45 GB |
| 就绪 | 几分钟 | 首次同步之后：数小时到数天 |
| 隐私 | 密钥、钱包文件和扫描都在你的电脑上；节点能看到你下载了哪些区块、广播了哪些交易 | 除正常的 P2P 流量外，没有任何数据离开你的电脑 |

在启动界面选择，之后可随时在“设置 → 连接”中切换。如果 `127.0.0.1:8081` 上已有 `bytecoind` 应答（例如你自己的节点），钱包会直接使用它，而不会再启动一个。如果 Bytecoin 官方钱包已把区块链下载到 `%APPDATA%\bytecoin`，全节点模式会提供该文件夹，只下载缺少的区块。切换前，钱包会检查剩余空间以及磁盘是 SSD 还是 HDD，并告诉你首次同步大概需要多久（HDD 上可能要一周或更久）。

### 节点

“设置 → 连接”列出开放 JSON-RPC 的公共节点：社区节点 `node.bytecoin.community` 和 bcndev 的两个官方节点 `node01/node02.bytecoin.org:8091`。你也可以添加自己的节点（`host:port`、`http://…`、`https://…`）。“检查节点”会显示每个节点的响应时间、高度，以及是否与其他节点在同一条链上（比较最低链顶以下某个区块的哈希；哈希不同的节点会被标记为“另一条链——请勿使用”）。

**自动**（默认）会在健康且不比最快节点慢太多的节点中随机选择，让钱包分散到多个节点，而不是全部压在一个节点上；当前节点 90 秒无应答时会切换到另一个节点。

### 已有的区块链

全节点模式需要 bytecoind 的*数据文件夹*，即包含 `blockchain` 子文件夹的那个文件夹。官方钱包把它放在 `%APPDATA%\bytecoin`，本钱包会自动提供该文件夹。你也可以从另一台电脑复制该文件夹（先停止那边的 bytecoind），然后在“设置 → 连接”中选择它。请把它放在 NTFS 格式的 SSD 上，不要放在 OneDrive/Dropbox 同步的文件夹中。

“验证区块链”会在整条链上分布的高度（最初的区块、每 500 000 个区块一处、以及链顶附近）把你的节点的区块哈希与所有健康的公共节点进行比较。哈希一致说明你的数据库与网络在同一条链上；此后 bytecoind 会自行验证每个新区块。bytecoind 3.5.1 不会逐块重新验证复制来的数据库，因此若要完全独立，请从头下载区块链。

## 功能

- 创建钱包（24 个单词的 BIP39 助记词，通过重新输入 3 个单词确认），用单词恢复钱包（可选“创建于某日期之后”），打开任意 `.wallet` 文件，导入旧钱包的密钥（以 `2` 开头的地址）。
- 官方钱包的其他钱包类型：硬件 Amethyst 钱包（装有 Bytecoin 应用的 Ledger/Trezor；实验性，尚未用设备测试）和随机密钥的 legacy 钱包。
- 余额、同步进度、最近交易；带搜索和筛选的完整历史；交易详情及指向[区块浏览器](https://explorer.bytecoin.community)的链接。
- 按手续费档位发送（经济 / 普通 / 快速，基于节点推荐的手续费），匿名级别 0–50（环大小 − 1，默认 6，与 walletd 相同），“全部发送”，可选的交易所 payment ID，以及显示确切手续费的确认界面。
- 付款证明（sendproof）：为转出交易创建证明，或检查别人给你的证明，适用于 P2P 交易和交易所。
- 收款：带二维码的地址（可附金额），Amethyst 钱包可添加带标签的额外地址。
- 地址簿（保存在本地）。
- 显示 24 个单词（或旧钱包的密钥）并整体复制：剪贴板 60 秒后清空；修改密码；导出只读副本。
- 社区页面：[BCN Miner](https://github.com/bytecoinBCN-community/bcn-miner) 链接及一键“复制我的挖矿地址”、社区矿池、网站、区块浏览器、网页钱包、Telegram 和 GitHub。
- 托盘图标与收款通知，开机自启，隐藏金额。

![启动界面](docs/screenshots/start-dark.png)

## 安全

- 钱包文件由 walletd 用你的密码加密。密码和助记词通过标准输入（UTF-8）传给 walletd，从不出现在命令行中，界面也从不把它们写入磁盘。钱包打开期间，密码保存在程序内存中，以便你修改节点设置后重新连接；“关闭钱包”会清除它。
- walletd 只监听 `127.0.0.1`，使用随机空闲端口，每次启动都生成新的随机 HTTP 密码。
- walletd 和 bytecoind 运行在 Job Object 中：钱包关闭或崩溃时，它们也会一起停止。
- 界面从不把你的密钥发送到任何地方。在不下载区块链的模式下，远程节点收到的请求与任何 Bytecoin 钱包发出的相同；交易隐私（环签名、一次性地址）由 Bytecoin 本身提供。
- 如果缺少 walletd/bytecoind（从源码构建时），钱包会提议下载官方压缩包，并且只有在其 SHA-256 与代码中固定的值一致时才会安装。

**请把 24 个单词抄在纸上。**密码保护的是文件，单词才是钱。

## 语言

Українська、English、Español、Português、Türkçe、Bahasa Indonesia、Tiếng Việt、中文：与 [bytecoin.community](https://bytecoin.community) 相同的一组语言。在“设置 → 外观 → 语言”或启动界面的按钮中切换。“系统”跟随 Windows 界面语言，不支持时使用英语。

翻译位于 `src/lang/<代码>.txt`，每行格式为 `键 = 文本`（`{0}` 为占位符，`\n` 为换行）。如需不重新构建就修正翻译，把 `<代码>.txt` 放进 `BCNWallet.exe` 旁边的 `lang` 文件夹即可。乌克兰语和英语为人工撰写，其他语言借助机器翻译，欢迎母语使用者校对。

![浅色主题](docs/screenshots/overview-light.png)

## 构建

只需 Windows 10/11，无需构建工具：
```
powershell -ExecutionPolicy Bypass -File .\build.ps1
```
`dist\` 中会生成 `BCNWallet.exe`、README 和许可证。运行一次 `get-node.ps1` 可把 walletd 和 bytecoind 放入 `node\`（会被复制到 `dist\node`），也可以让钱包在首次启动时自行下载。

```
powershell -ExecutionPolicy Bypass -File .\build.ps1 -Release
```
还会生成 `release\BCNWallet-<版本>-setup.exe`、`release\BCNWallet-<版本>-win-portable.zip` 和 `release\SHA256SUMS`。

代码为 C# 5，使用 .NET Framework 自带的编译器（`csc.exe`），WPF 界面完全用代码构建。

## 文件

- `src/App.cs`：启动、单实例、守护进程标准输入的 UTF-8、主题与语言。
- `src/Wallet.cs`：启动和停止 walletd/bytecoind，JSON-RPC 调用（`get_status`、`get_balance`、`get_transfers`、`create_transaction`、`send_transaction`、`get_wallet_records`、`create_addresses`），导出，修改密码。
- `src/Node.cs`：守护进程、Job Object、带 SHA-256 校验的官方程序下载。
- `src/Rpc.cs`：JSON-RPC 客户端，BCN 金额的格式化与解析。
- `src/MainWindow.cs`、`src/Start.cs`、`src/Pages.cs`、`src/Pages2.cs`：窗口、启动界面、总览、发送、收款、历史、地址簿、社区、设置。
- `src/Qr.cs`：二维码编码器。
- `src/Theme.cs`、`src/Styles.xaml`、`src/Ui.cs`：来自 *Bytecoin Desktop Suite* 画布的设计系统，与 BCN Miner 共用。
- `setup/Setup.cs`：安装与卸载程序。
- `get-node.ps1`：带 SHA-256 校验下载 walletd 和 bytecoind。

## 致谢

各项流程（walletd 如何启动、机密如何传递、使用哪些 RPC 方法）沿用 bcndev 官方的 [bytecoin-gui](https://github.com/bcndev/bytecoin-gui)，本钱包以 bytecoin.community 风格的新界面取代了它。

## 许可证

BCN Wallet 采用 [GNU LGPL v3.0](LICENSE) 授权（是对 [GNU GPL v3.0](COPYING) 的补充）。`walletd.exe` 和 `bytecoind.exe` © Bytecoin 开发者，LGPL-3.0，源码见 <https://github.com/bcndev/bytecoin>。
