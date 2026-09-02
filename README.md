<h1 align="center">XrayUI-Portable</h1>

<p align="center">
  专为 Windows 打造的极简、现代、高性能 WinUI 3 绿色便携代理客户端。<br/>
  基于 Xray-core 原生驱动，支持 Native AOT 极速启动与全功能便携隔离。
</p>

---

## ✨ 核心特性

- 🚀 **全绿色便携架构 (Portable)**：所有订阅、节点、配置与日志数据均统一存放于程序同级 `Data/` 目录下，跨电脑直接复制即开即用，无需重新配置；
- 🧙‍♂️ **智能初次启动向导**：首次运行时自动检测本机是否存在旧版 XrayUI 配置，弹窗询问是否导入。若无需导入或初次使用，自动生成 10000 以上无冲突随机混合端口；若选择导入，支持自定义端口或一键生成 10000+ 无冲突端口；
- 🛡️ **端口冲突实时预防与原生无冲突共存**：代理启动前自动预检端口占用，若遇冲突实时弹窗推荐并一键切换至可用端口；内置独立互斥体、专属自启服务与独立 TUN 适配器，支持与原版软件及其他代理工具同时运行；
- 🖱️ **服务器列表极速双击切换**：双击已连接服务器即可立即断开停止；双击未连接服务器可一键无缝连接并断开其他节点；
- 🎛️ **三大分类控制入口**：
  - **模式调节**：支持系统代理一键接管/直连、智能分流/全局路由/自定义规则、端口设置快捷入口、DNS 设置及开机自启；
  - **个性化设置**：支持浅色/深色/跟随系统主题、Mica (云母) / Acrylic (亚克力) 背景材质、全协议徽标色彩自定义、列表显示项与多语言切换；
  - **软件设置**：专属可视化控制面板，开机自启置顶管理，支持纯净端口录入与 🎲 随机端口生成、局域网共享、5组全局快捷键实时录制、便携数据管理（配置导入导出与备份）、实时日志查看；
- ⌨️ **常用全局快捷键与乒乓键 (Toggle)**：
  - `启动 / 停止代理`（乒乓键）
  - `显示 / 隐藏控制台窗口`（乒乓键）
  - `切换全局代理`（乒乓键）
  - `切换 TUN 模式`（乒乓键）
  - `切换路由模式`（乒乓键）
- 🌐 **全协议支持**：支持 Shadowsocks, VMess, VLESS, Trojan, Hysteria2, WireGuard 及链式代理 (Chain Proxy)；
- ⚡ **TUN 模式**：基于高性能 Wintun 驱动接管整机流量；
- 🤖 **AI 解锁检测**：内置主流 AI 服务可用性状态检测。

---

## 📥 下载使用

前往 [Releases 页面](https://github.com/vimbackground/XrayUI/releases/latest) 下载最新发布的绿色免安装包 `XrayUI-Portable`，解压即用。

---

## 🛠️ 编译与开发指南

> [!NOTE]
> 编译 XrayUI-Portable 推荐使用 **.NET 10 SDK** 以及 **Visual Studio 2022**（或 **Visual Studio Build Tools**，需勾选“使用 C++ 的桌面开发”工作负载）。

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

编译产物将生成在 `bin/Release/net10.0-windows10.0.19041.0/win-x64/publish/XrayUI-Portable.exe`。

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
