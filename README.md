<img width="2172" height="724" alt="image" src="https://github.com/user-attachments/assets/ea4d4a40-76cd-48f5-abc5-ce3bc07d6f3c" />

<h1 align="center">XrayUI</h1>

<p align="center">
  基于 <a href="https://docs.microsoft.com/windows/apps/winui">WinUI 3</a> 和 .NET 10 构建的 Windows 原生 Xray GUI 客户端，轻量、敏捷、现代化。
</p>

---

## 🌟 本 Fork 版本改进说明

本项目基于原版 [PhoenixNil/XrayUI-dev](https://github.com/PhoenixNil/XrayUI-dev) 进行维护与体验优化，主要新增与改进功能包括：

- ⚡ **控制台代理状态快捷切换**：控制台右下侧直接提供 `[ 全局代理 | 不接管代理 ]` 分段切换控件，一键直达，TUN 模式下智能置灰。
- 🎯 **路由策略状态清晰指示**：设置菜单中的“路由设置”升级为单选标记（RadioMenuFlyoutItem），清晰直观展示当前生效的“全局路由”或“智能分流”。
- 🚀 **全局快捷键置顶防遮挡唤醒**：重构窗口激活与焦点夺取逻辑，采用 Win32 原生置顶重排序，彻底解决快捷键恢复窗口时被其他前台应用遮挡的问题。
- 📋 **从剪贴板一键导入**：服务器列表“添加”下拉菜单新增“从剪贴板导入”，一键自动识别并解析多协议节点链接、Base64 订阅文本或 Clash YAML 配置。
- 🎨 **极简白底纯黑小火箭图标**：全新设计极简风格的白底纯黑小火箭应用图标与托盘指示图标（带连接状态徽标指示）。

---

## ✨ 核心特性

- **多协议全支持**：支持 Shadowsocks、VMess、VLESS (含 XTLS / Reality / Flow / Encryption)、Trojan、Hysteria2、WireGuard 以及链式代理 (Chain Proxy)。
- **TUN 虚拟网卡模式**：内置 Wintun 驱动，支持全局系统级流量接管与 IPv4/IPv6 双栈路由。
- **订阅与节点管理**：支持节点链接导入、Base64 订阅拉取、Clash 配置文件转换以及定时自动更新。
- **AI 解锁状态检测**：一键检测节点对 OpenAI、Claude、Gemini 的连通与区域解锁情况。
- **智能分流与高级路由**：内置 GeoIP / GeoSite 规则库，支持国内分流、自定义路由规则及 FakeDNS。
- **自启动与自动重连**：支持 Windows 开机静默启动到托盘及自动连接上次节点。
- **个性化定制**：支持浅色/深色主题、Mica (云母) / Acrylic (亚克力) 背景材质及协议徽标颜色自定义。

---

## 🖼️ 界面预览

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
