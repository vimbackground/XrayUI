---
id: DOC-XRAYUI-SYSTEM-ARCHITECTURE
title: XrayUI 系统架构
document_type: architecture
status: active
version: 1.0.0
project: XrayUI
owner: XrayUI maintainers
audience:
  - developer
  - agent
scope: _System/architecture
created_at: 2026-09-06T23:40:00+08:00
updated_at: 2026-09-06T23:40:00+08:00
tags:
  - architecture
  - host-repository
---

# XrayUI 系统架构

## 1. 应用与技术栈

XrayUI 是面向 Windows 10/11 的 WinUI 3 桌面应用，主项目使用 .NET 10，支持 x86、x64 和 ARM64。发布链支持 Native AOT、自包含部署和 Windows App SDK 两种运行时携带方式。

## 2. Project Content

- `App.xaml*`、`MainWindow.xaml*`：应用入口与主窗口。
- `Views/`、`ViewModels/`、`Controls/`、`Converters/`、`Styles/`：界面与展示逻辑。
- `Services/`：配置生成、订阅、日志、更新及系统交互能力。
- `Models/`、`Helpers/`：领域模型与通用辅助逻辑。
- `Assets/`、`Strings/`、`Properties/`：引擎、规则、图标、本地化和发布配置。
- `XrayUI.Tests/`：不依赖 WinUI 运行时的解析与序列化单元测试。
- `updater-rs/`：独立 Rust updater；发布时按 RID 构建并复制为 `XrayUI.Updater.exe`。
- `.github/workflows/`：测试、构建、发布和协作自动化。

## 3. Framework Infrastructure

- `Agent.md`：XrayUI 的 Agent 协作与变更门禁。
- `_System/standards/`：vHarness 唯一机器标准源。
- `_System/architecture/`：架构与 Work Item Registry。
- `_System/memory/`：当前状态与跨会话交接。
- `_System/reviews/`：正式方案、审计与审批记录。
- `_System/recovery/`、`_System/tools/`：恢复、验证和迁移工具。
- `plugins/`：可选语言能力；当前包含 Python validator。

## 4. Developer Resources

`_Dev/` 保存 SOP、manuals、References、templates 和 platforms 资料。`_Dev/box/` 为开发者人工管理区，Agent 默认只读。

## 5. 构建、测试与交付

- 单元测试：`dotnet test XrayUI.Tests/XrayUI.Tests.csproj -c Release`。
- 发布：以 README 与 `.github/workflows/release.yml` 的 `dotnet publish` 参数为准。
- Rust updater：由 Cargo 独立构建；本地发布可由 MSBuild 目标联动，CI 可分步构建。
- `bin/`、`obj/`、`updater-rs/target/` 是生成内容，不属于受管源码。

## 6. 结构原则

- vHarness 不重组 Project Content，也不要求 `Tasks/Projects` 或 `in/review/out`。
- XrayUI 作为 Host Repository 继续采用现有 GitHub Releases 交付流程，不要求 `_Dist`。
- 框架规则、项目事实与生成内容分别验证，避免把模板约束错误施加到业务目录。
