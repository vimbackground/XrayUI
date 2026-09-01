<h1 align="center">XrayUI</h1>
A native Windows GUI client for the Xray core, built with <a style="text-decoration:none" href="https://docs.microsoft.com/windows/apps/winui">WinUI</a>. Designed to be a fast and lightweight proxy client.


## Features

- Support Shadowsocks, VMess, VLESS, Trojan, Hysteria2, WireGuard and Chain Proxy
- TUN mode
- Subscription import and update
- AI Unlock Status Detection
- Custom routing rules with geoip / geosite
- Auto-start on boot, auto-connect
- Theme and protocol color customization

## UI Preview
<img width="1465" height="982" alt="image" src="https://github.com/user-attachments/assets/ff288102-d874-4ecb-87dd-0a9d880cc1cf" />

---

## 📥 下载安装

前往 [Releases 页面](https://github.com/vimbackground/XrayUI/releases/latest) 下载最新发布的绿色免安装包。

---

## 🛠️ 编译与开发指南

> [!NOTE]
> 编译 XrayUI 推荐使用 **.NET 10 SDK** 以及 **Visual Studio 2022**（或 **Visual Studio Build Tools**，需勾选“使用 C++ 的桌面开发”工作负载）。

### 方式 1：使用 Visual Studio
1. 双击打开 `XrayUI-dev.slnx` 解决方案；
2. 选择目标平台（如 `x64` 或 `ARM64`）和 `Release` 配置；
3. 点击 **生成解决方案** 即可。

### 方式 2：使用命令行 (PowerShell)

#### x64 平台发布 (Native AOT 极简模式)
```powershell
dotnet publish XrayUI-dev.csproj -c Release -r win-x64 -p:Platform=x64 -p:SelfContained=true -p:PublishAot=true -p:PublishTrimmed=true -p:WindowsAppSDKSelfContained=false -p:BuildingForCI=true
```

#### ARM64 平台发布
```powershell
dotnet publish XrayUI-dev.csproj -c Release -r win-arm64 -p:Platform=ARM64 -p:SelfContained=true -p:PublishAot=true -p:PublishTrimmed=true -p:WindowsAppSDKSelfContained=false -p:BuildingForCI=true
```

---

## 🙏 致谢 (Acknowledgements)

衷心感谢以下开源项目与社区的贡献：

- 原项目：[PhoenixNil/XrayUI-dev](https://github.com/PhoenixNil/XrayUI-dev)（感谢原作者构建的优秀 WinUI 3 基础框架与客户端架构）
- 核心引擎：[Xray-core](https://github.com/XTLS/Xray-core)
- 驱动支持：[Wintun](https://www.wintun.net/)
- 社区支持：[Linux.do 社区](https://linux.do)

<p>
  <a href="https://linux.do">
    <img src="https://img.shields.io/badge/LinuxDo-community-1f6feb" alt="LinuxDo">
  </a>
</p>

---

## 📄 开源协议

本项目采用 [Apache License 2.0](LICENSE) 开源协议。
