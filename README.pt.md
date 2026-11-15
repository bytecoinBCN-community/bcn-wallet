# BCN Wallet para Windows

[English](README.md) · [Українська](README.uk.md) · [Español](README.es.md) · **Português** · [Türkçe](README.tr.md) · [Bahasa Indonesia](README.id.md) · [Tiếng Việt](README.vi.md) · [中文](README.zh.md)

Carteira de desktop para Bytecoin (BCN). Funciona **com ou sem baixar a blockchain**, mantém as chaves no seu computador, tem tema escuro e claro e interface em oito idiomas. Distribuída como instalador e como zip portátil.

Projeto independente da comunidade Bytecoin, sem vínculo com os fundadores do Bytecoin ou com a bcndev.

![Visão geral](docs/screenshots/overview-dark.png)

## Download

Em [Releases](../../releases):

- `BCNWallet-<versão>-setup.exe` — instala só para o seu usuário (sem direitos de administrador) em `%LOCALAPPDATA%\Programs\BCN Wallet`, com atalhos e desinstalação em Configurações → Aplicativos. Carteiras e configurações ficam em `%APPDATA%\BCN Wallet` e nunca são tocadas.
- `BCNWallet-<versão>-win-portable.zip` — descompacte em qualquer lugar (até num pendrive) e rode `BCNWallet.exe`. Tudo fica na pasta `data` ao lado do programa.

Ambos trazem `walletd.exe` e `bytecoind.exe` 3.5.1 da [versão oficial do Bytecoin](https://github.com/bcndev/bytecoin/releases/tag/v3.5.1), sem alterações. As somas estão em `SHA256SUMS`. Basta o .NET Framework 4.8, incluído no Windows 10 e 11.

## Duas formas de conexão

- **Sem a blockchain (padrão):** o walletd usa um bytecoind remoto — [node.bytecoin.community](https://node.bytecoin.community/) ou outro nó indicado. Cerca de 100 MB, pronto em minutos. As chaves e a varredura ficam no seu PC.
- **Nó completo:** a carteira executa o próprio `bytecoind` e baixa a blockchain (cerca de 45 GB; a primeira sincronização leva de horas a dias). Se já houver um bytecoind em `127.0.0.1:8081`, ele é usado.

Escolha na tela inicial; mude em Configurações → Conexão.

## Recursos

Criar (24 palavras com verificação), restaurar com data opcional, abrir arquivos `.wallet`, importar chaves de carteiras antigas; saldo, histórico com busca, envio com níveis de taxa e Payment ID, recebimento com QR code e endereços extras, contatos, mostrar as 24 palavras, trocar a senha, cópia somente leitura; página da comunidade com [BCN Miner](https://github.com/bytecoinBCN-community/bcn-miner), pool, site, explorador, carteira web e Telegram.

## Segurança

Senhas e a frase vão para o walletd pela entrada padrão (UTF-8), nunca pela linha de comando nem para o disco. O walletd escuta só em `127.0.0.1`, com porta e senha HTTP aleatórias. **Anote as 24 palavras em papel.**

## Compilação

```
powershell -ExecutionPolicy Bypass -File .\build.ps1 -Release
```
Detalhes no [README em inglês](README.md#building).

## Licença

[GNU LGPL v3.0](LICENSE). `walletd.exe` e `bytecoind.exe` — © The Bytecoin developers, LGPL-3.0.
