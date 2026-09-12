<h1 align="center">🚀 XrayUI-Portable</h1>

<p align="center">
  <b>为 Windows 打造的极简、轻快、全绿色免安装的科学冲浪客户端。</b><br/>
优势：1.解压即用，免安装，可迁移；2.可同时连接多个服务器节点，通过不同端口分流同时访问；3. 软件速度极快，系统资源占用极低；4. 端口自动设置，避免冲突。

---

## 🌟 为什么选择 XrayUI-Portable？

市面上的代理工具很多，但往往存在“换电脑配置全丢”、“一次只能连一个国家节点”、“内存占用大”、“界面不够美观”等痛点。**XrayUI-Portable** 正是为此而生：

> **项目关系说明**：本项目是基于 [vimbackground/XrayUI](https://github.com/vimbackground/XrayUI) 继续开发的独立便携版，项目名称、发布包和功能演进均与上游仓库区分管理，并不代表上游项目的官方发行版。本项目重点强化了全目录便携数据、多节点并行分流、端口冲突处理、TUN 模式、运行状态监控和 Native AOT 发布体验。

### 1. 🎒 真正的全绿色免安装（配置永不丢失）
- 所有订阅、节点列表、自定义规则及日志，**全部存放在软件同级的 `Data/` 文件夹中**；
- 换电脑、重装系统或放在 U 盘里？**整包复制过去直接双击就能用**，所有节点和设置完好如初，再也不用繁琐重配！

### 2. 🔀 独创：多节点同时上网（多节点分流模式）
- **告别来回切换连接**：以往看剧、游戏、查资料如果在不同地区，必须反复断开重连；
- **一台电脑同时连多个节点**：主节点连香港用于网页查资料，同时可为日本节点分配独立端口（如 `:10809`）看流媒体，为新加坡节点分配 `:10810` 用于游戏。第三方软件只需指定对应端口，即可实现定向分流！
- **渐进式极简设计**：平时不开启该模式时，界面与普通客户端一样纯净简洁，没有任何复杂选项打扰。

### 3. 🖱️ 双击极速操作 & 智能防冲突
- **双击即切**：双击已连接的节点立即停止；双击未连接的节点秒连并自动断开其他节点；
- **智能避开端口占用**：首次启动或日常运行中，如果遇到端口被其他软件占用，系统会自动预检并一键分配空闲端口，小白用户也能无感上手。

### 4. 🎨 原生 Windows 现代视觉（秒开、极省内存）
- 采用微软最新的 **WinUI 3** 原生界面，完美支持 Windows 11 / 10 风格；
- 支持 **深色 / 浅色模式** 与 **云母 (Mica) / 亚克力 (Acrylic) 毛玻璃半透明** 材质；
- 采用 **Native AOT** 机器码原生编译，软件体积仅约 16MB，双击瞬间秒开，内存占用极低。

### 5. ⌨️ 超实用的全局快捷键（老板键）
- **一键隐藏/唤出窗口**（老板键，下班或摸鱼必备）；
- **一键开关代理** / **一键切换分流与全局模式**；
- 允许自由自定义快捷键组合。

### 6. 🌐 全能协议支持与智能分流
- 全面支持主流协议：**VLESS, VMess, Trojan, Shadowsocks, Hysteria2, WireGuard** 及 **链式代理 (Proxy Chain)**；
- 内置 **智能分流模式**（国内网站直连不绕路，国外网站高速代理），并支持 **TUN 虚拟网卡模式** 与 **AI 常用服务解锁检测**；
- Gemini 检测到当前节点受限时，会自动按节点列表切换到下一个节点尝试，减少手动切换。

### 7. 🖥️ 运行时信息与独立控制
- 在代理动态信息窗口中，可查看主代理和已启用辅助代理的运行状态、端口及日志；
- 实时查看代理上传/下载速度及累计流量，数据每秒刷新；
- 可直接关闭当前选中的主代理或辅助代理，不必返回主界面操作。

---

## 📥 下载与安装

前往 [👉 Releases 页面](https://github.com/vimbackground/XrayUI/releases/latest) 下载最新版本：

| 版本类型 | 适用人群 | 下载链接 |
| :--- | :--- | :--- |
| **x64 常规版 (强烈推荐 🌟)** | 绝大多数 Windows 电脑（Intel / AMD 处理器） | [下载 XrayUI-win-x64.zip](https://github.com/vimbackground/XrayUI/releases/latest) |
| **ARM64 常规版** | Surface Pro X、骁龙本等 ARM 架构 Windows 电脑 | [下载 XrayUI-win-arm64.zip](https://github.com/vimbackground/XrayUI/releases/latest) |
| **独立完整版 (wasdk)** | 仅当普通版提示缺少 Windows 运行库无法启动时使用 | [下载包含运行时的完整版](https://github.com/vimbackground/XrayUI/releases/latest) |

> 💡 **使用方法**：下载 `.zip` 压缩包后，**解压到任意文件夹**，双击运行 `XrayUI-Portable.exe` 即可使用！

---

## 🧭 常见问题 (FAQ)

<details>
<summary><b>Q1: 怎么开启“多节点分流（多个节点同时连接）”？</b></summary>

1. 点击右下角 **【软件设置】（齿轮图标）**；
2. 找到 **“多节点分流模式”** 并打开开关，点击保存；
3. 返回主界面，在需要作为独立端口的节点上 **右键 -> 设置独立分流端口**（或开启独立端口）；
4. 此时该节点会显示 `[🎧 专用端口 :端口号]`，在其他软件中配置该端口作为代理即可同时上网！
</details>

<details>
<summary><b>Q2: 怎么将所有配置迁移到新电脑？</b></summary>

直接将包含 `XrayUI-Portable.exe` 和 `Data/` 文件夹的整个目录复制到新电脑上，打开即可继续使用，无需重新导入订阅。
</details>

<details>
<summary><b>Q3: 遇到端口冲突无法启动代理怎么办？</b></summary>

软件内置了冲突检测，如果当前端口被其他软件占用，会自动弹出提示并推荐一个空闲端口，点击确认即可一键切换并启动。
</details>

<details>
<summary><b>Q4: Gemini 检测失败后会发生什么？</b></summary>

当当前主节点正在运行且 Gemini 解锁检测结果为受限时，软件会自动尝试节点列表中的下一个节点。每个节点在本轮检测中最多尝试一次；如果没有可用节点，请手动选择其他节点。
</details>

<details>
<summary><b>Q5: 如何单独关闭某个正在运行的代理？</b></summary>

打开 **代理动态信息** 窗口，在“查看”下拉框中选择主代理或辅助代理，然后点击 **关闭当前代理**。主代理和辅助代理可以分别控制。
</details>

<details>
<summary><b>Q6: 网速监测统计的是什么流量？</b></summary>

打开 **代理动态信息** 窗口即可查看上传速度、下载速度及累计流量。统计对象是经过 Xray 代理入站的流量，不包含其他程序直接使用网卡产生的流量。
</details>

---

## 🛠️ 开发者指南 (编译源码)

如果你希望自行编译本项目：

```powershell
# 使用 .NET 10 SDK 进行 Native AOT 编译
dotnet publish XrayUI-dev.csproj -c Release -r win-x64 -p:Platform=x64 -p:SelfContained=true -p:PublishAot=true -p:PublishTrimmed=true -p:WindowsAppSDKSelfContained=false -p:BuildingForCI=true
```

---

## 🙏 致谢 (Acknowledgements)

- 上游项目：[vimbackground/XrayUI](https://github.com/vimbackground/XrayUI)（感谢其开源项目基础与持续维护）
- 原项目：[PhoenixNil/XrayUI-dev](https://github.com/PhoenixNil/XrayUI-dev)（感谢原作者构建的优秀 WinUI 3 客户端框架）
- 核心引擎：[Xray-core](https://github.com/XTLS/Xray-core)
- 驱动支持：[Wintun](https://www.wintun.net/)
- 社区交流：[Linux.do 社区](https://linux.do)

---

## 📄 开源协议

本项目基于 [Apache License 2.0](LICENSE) 开源协议。
