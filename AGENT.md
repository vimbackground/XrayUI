---
id: DOC-XRAYUI-AGENT-SUPREME
title: XrayUI Agent 最高治理规范
document_type: specification
status: active
version: 2.0.0
project: XrayUI
owner: XrayUI maintainers
audience:
  - developer
  - agent
scope: repository
created_at: 2026-09-06T23:40:00+08:00
updated_at: 2026-09-28T11:50:00+08:00
tags:
  - governance
  - agent-rules
  - supreme-policy
---

# XrayUI 项目 Agent 最高治理规范 (AGENT.md)

> [!IMPORTANT]
> **最高效力声明 (Supremacy Clause)**：  
> 本文档是 XrayUI 项目中所有 AI 编程助理（Agent）与自动化流程的**最高治理规范**。任何存在冲突的其他文档、SOP、操作惯例或预设提示词，一律以本文档规定为准。

---

## 一、核心交付三大铁律 (Three Golden Rules)

### 1. 默认免测高效发布原则 (Skip Unit Tests by Default)
- 在执行日常构建、版本升级与 GitHub 发布流程时，**默认不执行自动化单元测试**（禁止自动运行 `dotnet test` 等测试套件）。
- **例外条件**：仅在开发者明确下达“运行测试”、“检查回归”等指令时，方可执行单元测试。
- **核心目的**：杜绝因等待大量单元测试执行而浪费开发者等待时间，最大化交付与迭代速度。

### 2. 单一目标构建原则 (Native AOT Only)
- 本地构建与交付时，**只构建 Native AOT 绿色便携发布版**。
- **本地发布产物位置**：`_Dist/publish/win-x64/`（核心文件为免安装独立可执行文件 `XrayUI-Portable.exe`）。
- **标准发布编译命令**：
  ```powershell
  dotnet publish XrayUI-dev.csproj /p:PublishProfile=Properties/PublishProfiles/win-x64.pubxml -p:BuildingForCI=true
  ```
- **禁止项**：除非开发者特别要求调试，默认**不要**生成 `_Dist/build/` 的依赖版或重复中间产物，杜绝双重编译开销。

### 3. GitHub 单架构精简发布原则 (GitHub Releases Only x64)
- GitHub 远端发布流水线（GitHub Actions）**仅构建与发布 x64 架构版本**。
- 移除 ARM64 编译矩阵，不再生成 ARM64 制品；仅保留以下两项 x64 制品交付：
  1. `XrayUI-win-x64.zip`（常规 Native AOT 绿色便携版）
  2. `XrayUI-win-x64-wasdk.zip`（内嵌 Windows App SDK 独立便携版）
- **核心目的**：显著缩短 GitHub Actions 云端构建时间（从 15~30 分钟大幅压减），避免浪费 CI 额度与发布冗余。

---

## 二、架构与目录边界

1. **业务源码目录 (Project Content)**：
   - 核心代码：`Controls/`、`Converters/`、`Helpers/`、`Models/`、`Services/`、`Styles/`、`ViewModels/`、`Views/`、`Assets/`、`Strings/`、`Properties/`、`updater-rs/`。
   - 测试代码：`XrayUI.Tests/`（受版本控制，由开发者按需触发）。
2. **治理与开发资料 (Governance & Dev Infrastructure)**：
   - `_System/`、`_Dev/`、`AGENT.md` 为本地架构治理体系。
3. **本地构建与交付根目录**：
   - `_Dist/publish/win-x64/` 为本地最终交付物唯一定位路径。
   - `_Dist/` 严格受 `.gitignore` 保护，严禁提交到 Git。

---

## 三、公私双分支隔离与发布规范

- **私有分支 (`private/main`)**：
  - 存放完整的本地开发历史、治理规约、状态备忘及私有文档。
  - **绝不直接推送到 `origin` 远端**。
- **公开分支 (`public-main` / `main`)**：
  - 仅包含纯净的产品源码、资源、配置与公开 `CHANGELOG.md`。
  - 严禁包含 `_Dev/`、`_System/`、`_wip/` 或任何内部草案。
- **发布至 GitHub 步骤**：
  1. 将产品源码与公开配置同步至公开分支（如通过独立干净工作树或 Cherry-pick）；
  2. 审查变更清单，确认无内部文件；
  3. 打上版本 Tag（如 `v1.2.2`）；
  4. 推送 `main` 分支及 Tag 至 `origin`，触发 GitHub Actions 自动化发布。

---

## 四、Agent 行为准则

1. **以开发者需求为中心**：指令明确、交付迅速，不推诿、不添加未经要求的额外多余流程。
2. **先排查后操作**：涉及状态变更或核心代理控制逻辑时，准确定位代码根因后再修改。
3. **环境适应与透明报告**：如遇到网络波动、编译锁或工具限制，清晰向开发者说明并提供最优解法。
