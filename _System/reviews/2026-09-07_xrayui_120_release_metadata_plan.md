---
id: PLAN-XRAYUI-120-RELEASE-METADATA-20260907
title: XrayUI 1.2.0 版本与 GitHub Release 更新说明方案
document_type: plan
status: verified
version: 1.0.0
project: XrayUI
owner: XrayUI maintainers
audience:
  - developer
  - agent
scope: _System/reviews
created_at: 2026-09-07T00:00:00+08:00
updated_at: 2026-09-07T03:45:00+08:00
tags:
  - xrayui
  - release
  - versioning
  - changelog
---

# XrayUI 1.2.0 版本与 GitHub Release 更新说明方案

## 1. 事实基线

- 主应用默认版本、文件版本和程序集版本当前均为 `1.1.0`；GitHub Release 工作流读取主项目版本作为非标签构建的版本回退，并另有 `1.1.0` 兜底值。
- Rust 更新器包版本当前为 `1.1.0`，应与随应用一同发布的版本线保持一致。
- GitHub Release 工作流会按目标版本从根目录 `CHANGELOG.md` 提取对应的二级标题章节，并将其加入 Releases 下载说明之后；不存在匹配章节时才使用通用兜底文案。
- 应用内更新弹窗读取的是网站托管的独立 `changelog.json`，该资源不在当前仓库和本次授权范围内；本方案只保证 GitHub Releases 的更新说明。

## 2. 目标与非目标

### 目标

1. 将应用、文件、程序集和 Rust 更新器版本统一为 `1.2.0`。
2. 将 GitHub Release 工作流的版本兜底同步为 `1.2.0`，确保手动触发时与项目默认版本一致。
3. 在 `CHANGELOG.md` 顶部增加 `1.2.0` 章节，覆盖本版本已完成的多代理可靠性、主/辅代理交互、运行状态信息、完全退出与托盘恢复、便携数据保护和启动稳定性改进。
4. 验证工作流的现有正则可以精确提取 `1.2.0` 更新说明，不影响旧版本条目和下载链接生成。

### 非目标

- 不创建 Git tag、不推送代码、不触发 GitHub Actions、不创建 GitHub Release。
- 不修改网站托管的应用内更新说明源，也不宣称已同步应用内弹窗内容。
- 不改变自动更新协议、发布资产名称、更新器复制行为或用户 `Data`。

## 3. 预期 GitHub Release 说明

```text
## [1.2.0] - 2026-09-07

### 代理体验与可靠性
- 主代理快速切换更稳定；识别到本软件残留核心占用端口时会自动恢复。
- 多代理界面使用“主代理 / 辅代理”及清晰状态，提供实时运行信息。

### 数据安全与稳定性
- 覆盖升级不会带入或覆盖用户 Data；异常配置可安全启动并保留备份。
- 修复多节点便携配置的启动稳定性问题，增加完全退出和托盘图标恢复。
```

## 4. 变更清单

### [MODIFY]

- `XrayUI-dev.csproj`：将默认 `Version`、`FileVersion` 与 `AssemblyVersion` 更新为 `1.2.0` / `1.2.0.0`。
- `updater-rs/Cargo.toml`：将更新器包版本更新为 `1.2.0`，与发布版本线一致。
- `.github/workflows/release.yml`：将非标签构建的显式版本兜底更新为 `1.2.0`；保留从项目文件读取版本、提取 changelog、构建资产和创建 Release 的既有流程。
- `CHANGELOG.md`：新增面向 GitHub Releases 的 `1.2.0` 用户更新说明，标题格式与工作流提取正则匹配。
- `_System/architecture/task_breakdown.md`、`_System/memory/current_state.md`：记录方案批准、版本同步、验证结果和未执行的 GitHub 发布。
- 本方案：获批后转为 `in_progress`，验收完成后转为 `verified`。

### [NEW]

- 无。

### [DELETE]

- 无。

## 5. 实施与验收

1. 获批后创建并验证实施前快照，更新方案和 Work Item 状态。
2. 同步四处版本元数据，再新增 changelog 章节；不触发任何远程写入。
3. 用与 GitHub 工作流相同的提取规则验证恰好命中 `1.2.0` 条目；确认旧 `1.1.0` 条目仍存在。
4. 运行 Release 构建和 99 项单元测试；检查程序集版本为 `1.2.0.0`，运行 Host Architecture Validation、Host Documentation Validation 和 `git diff --check`。

## 6. 风险、回滚与决策

- 风险：版本元数据遗漏会导致 GitHub tag、发布资产或更新器显示不一致。控制方式是对主项目、工作流兜底和 Rust 包逐项搜索并验证。
- 风险：changelog 标题不匹配会使 GitHub Release 退回通用文案。控制方式是使用工作流的实际正则做本地提取验证。
- 风险：将 GitHub Releases 与应用内网站更新源混淆。控制方式是明确外部网站内容为 defer，不做未经授权的外部修改。
- 回滚：通过本轮快照恢复四处版本/说明文件和治理记录；不涉及远程 Release，因此无需远程回滚。
- Decision Item：adopt `1.2.0` 作为下一次 GitHub Release 的应用和更新器版本，并以 `CHANGELOG.md` 作为 GitHub Releases 更新说明来源；应用内网站更新说明保持 defer。不包含 Git tag、提交、推送或发布。

## 7. 实施记录与验收结果

- 已完成：主应用默认版本、文件版本和程序集版本分别更新为 `1.2.0`、`1.2.0.0` 和 `1.2.0.0`；Release 构建产物的程序集版本实测为 `1.2.0.0`。
- 已完成：Rust 更新器包版本和 GitHub Release 工作流的非标签兜底版本均更新为 `1.2.0`。
- 已完成：`CHANGELOG.md` 顶部新增 `1.2.0` 用户更新说明，覆盖本版本的代理可靠性、主/辅代理交互、运行信息、数据保护、启动稳定性、完全退出和托盘恢复。
- 已验证：用工作流同一正则规则本地提取，精确命中 `## [1.2.0] - 2026-09-07`；既有 `1.1.0` 条目仍保留。
- 自动验证：Release 构建无警告无错误，99/99 单元测试通过；win-x64 Native AOT 发布成功，最终可执行文件的文件版本实测为 `1.2.0.0` 且发布目录不含 `Data/`；实施前快照完整性、Host Architecture Validation、Host Documentation Validation 和 `git diff --check` 通过。
- 未执行：未创建 Git tag、未提交、未推送、未触发 GitHub Actions 或 GitHub Release；网站托管的应用内更新说明未修改。
