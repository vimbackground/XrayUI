---
id: PLAN-XRAYUI-BUILD-OUTPUT-WORKSPACE-RETENTION-20260907
title: XrayUI 构建交付目录与本地工作区保留优化方案
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
updated_at: 2026-09-07T03:10:00+08:00
tags:
  - xrayui
  - build-output
  - workspace
  - retention
---

# XrayUI 构建交付目录与本地工作区保留优化方案

## 1. 事实基线

- 主项目当前使用 SDK 默认 `bin/` 作为编译和发布输出；发布工作流中的打包、校验和更新器复制步骤也硬编码引用该路径，因此不能直接把已有目录改名。
- `obj/` 是 .NET/WinUI 生成的中间文件和缓存；删除后可由构建完全再生。
- `XrayUI.Tests/` 是受版本控制的测试源码项目，内部 `bin/` 和 `obj/` 才是可再生缓存；主项目已经显式排除该目录，避免其进入应用编译。
- `_wip/` 是本地忽略目录，当前包含变更快照、迁移回滚资料和大量发布验证副本。其快照可用于回滚；多个 bisect、旧版 UI 验证及含隔离测试数据的发布副本仅用于已完成诊断，不应长期保留。
- 已有 Work Item 曾将 `_Dist` 保持为 defer；本方案以“开发者快速找到本地交付物”为目标，正式 adopt `_Dist` 为本地构建和发布交付根目录，不改变 GitHub Release 的对外交付方式。

## 2. 目标与非目标

### 目标

1. 将主项目后续构建输出从 `bin/` 迁移到 `_Dist/build/`，将本地 publish 输出落在便于用户定位的 `_Dist/publish/<RID>/`。
2. 让 GitHub Actions 的发布、更新器复制、AOT 可执行文件检查和压缩步骤使用新输出目录，不改变产物名称、RID 矩阵、版本号或 Release 上传行为。
3. 保留 `obj/` 和 `XrayUI.Tests/` 的正确职责；清理它们的可再生缓存而不删除源码。
4. 在验证最新发布物后，清理旧 `bin/`、主项目 `obj/`、测试项目缓存，以及 `_wip/publish-check/` 中已完成且不再需要的诊断副本；保留可回滚快照和迁移资料。
5. 移除不再需要的、含隔离用户配置的临时发布副本，避免本地测试数据长期重复保存。

### 非目标

- 不改变 `Data/`、正式源码、测试源码、Git 历史、远程仓库或 GitHub Release。
- 不删除仍关联 `in_progress` 方案的快照，不删除迁移回滚资料。
- 不把 `_Dist/` 提交到 Git，不把构建输出、测试配置或凭据带入软件发布包。
- 不把 `obj/` 改名为 `_Dist`；它继续作为工具可再生的中间目录。

## 3. 目标目录结构

```text
_Dist/
├─ build/                 # 可再生的主项目编译输出
└─ publish/
   ├─ win-x64/            # 本地可直接取用的 x64 发布版
   └─ win-arm64/          # 本地可直接取用的 ARM64 发布版

obj/                      # 可再生的主项目中间缓存
XrayUI.Tests/             # 测试源码；其 bin/、obj/ 均为可再生缓存
_wip/
├─ change-snapshots/      # 保留当前/最近回滚快照
├─ vharness-migrations/   # 保留迁移回滚资料
└─ publish-check/         # 仅保留当前验证副本，旧诊断副本清理
```

## 4. 变更清单

### [MODIFY]

- `XrayUI-dev.csproj`：设置主项目基础输出根为 `_Dist/build/`，为带 RID 的本地 publish 设置 `_Dist/publish/<RID>/`；保持 `obj/`、`Data/`、`_wip/` 和测试源码的既有隔离规则。
- `Properties/PublishProfiles/win-x64.pubxml`、`Properties/PublishProfiles/win-arm64.pubxml`：将发布配置中的显式 publish 根从旧 `bin/` 迁移到对应 RID 的 `_Dist/publish/` 路径，避免配置覆盖项目默认值。
- `.github/workflows/release.yml`：将发布后的更新器复制、AOT 可执行文件检查和压缩步骤改为新 publish 路径；保留现有产物名称与 Release 行为。
- `.gitignore`：显式忽略 `_Dist/`，并保留既有 `bin/`、`obj/`、`_wip/` 忽略规则，防止本地交付物误提交。
- `_System/tools/common/create_change_snapshot.py`：将全部可再生的 `bin/`、`obj/`、`target/` 和 `_Dist/` 输出排除在后续变更快照之外，防止本地构建产物膨胀回滚资料。
- `_System/architecture/task_breakdown.md`、`_System/memory/current_state.md`：记录 `_Dist` 的 adopt、清理范围、验证结果和保留的回滚资料。
- 本方案：获批后转为 `in_progress`，验收完成后转为 `verified`。

### [DELETE]

- 仅在新 `_Dist/publish/win-x64/` 发布、启动和回归检查通过后，删除旧主项目 `bin/`、主项目 `obj/` 与 `XrayUI.Tests/bin/`、`XrayUI.Tests/obj/` 可再生缓存。
- 仅在验证完成后，删除 `_wip/publish-check/` 下已完成的 bisect、旧版 UI 验证和旧发布副本；保留本轮最新发布验证副本直到 `_Dist` 发布启动回归通过，再一并移除该临时副本及其隔离配置。
- 不删除 `_wip/change-snapshots/` 下与当前多代理、UE、端口恢复方案有关的快照，不删除 `_wip/vharness-migrations/`。

### [NEW]

- 无新业务源码；`_Dist/` 由构建和发布命令按需生成，不作为受版本控制目录创建。

## 5. 实施与验收

1. 获批后创建并验证实施前快照，更新方案和 Work Item 状态。
2. 先迁移项目输出属性和 CI 路径；运行 Release 构建、99 项测试和 win-x64 Native AOT publish，确认 `_Dist/publish/win-x64/XrayUI-Portable.exe` 存在且发布目录不含 `Data/`。
3. 使用原多节点配置的隔离副本完成 15 秒启动回归；完成后清理其临时副本、旧构建缓存和列明的旧 `publish-check` 目录。
4. 验收清理结果：`_Dist/` 可直接定位最新本地发布物；`bin/` 不再生成；`obj/` 与测试缓存可在下次构建自动创建；快照和迁移资料完整可读；Git 状态不包含 `_Dist/`、`_wip/`、生成物或测试配置。
5. 运行 Host Architecture Validation、Host Documentation Validation、`git diff --check`，并复核工作流中没有遗留对主项目 `bin/` 发布路径的引用。

## 6. 风险、回滚与决策

- 风险：CI 某个发布步骤仍引用旧路径会导致打包失败。控制方式是同时更新全部三处路径消费者，并用本地 AOT publish 验证实际输出。
- 风险：清理误删仍需要的回滚资料。控制方式是先验证快照完整性，仅删除明确列为可再生缓存或已完成 `publish-check` 副本的目录；删除前逐项列出精确目标。
- 风险：本地发布副本可能含隔离测试配置。控制方式是发布验证后删除该副本，且最终 `_Dist` 发布目录继续确认无 `Data/`。
- 回滚：恢复本轮快照中的项目文件和工作流；已删除的缓存可重新生成，受保护快照和迁移资料不受影响。
- Decision Item：adopt `_Dist` 作为本地构建与发布交付根目录；保留 `obj/`、测试源码、当前快照和迁移资料；清理旧 `publish-check` 诊断副本与全部可再生构建缓存。后续快照排除可再生输出以控制体积。不包含 Git 提交、推送或 GitHub Release。

## 7. 实施记录与验收结果

- 已完成：主项目编译输出迁移至 `_Dist/build/`；x64 与 ARM64 发布配置、GitHub 发布工作流中的更新器复制、可执行文件检查和压缩路径均迁移至 `_Dist/publish/<RID>/`。
- 已完成：`_Dist/` 受到 Git 忽略；后续变更快照排除 `bin/`、`obj/`、`target/` 和 `_Dist/`。独立快照检查已确认生成物条目为零，快照本身完整性通过。
- 已完成：Release 构建输出位于 `_Dist/build/`；99/99 单元测试通过；win-x64 Native AOT 发布成功，`_Dist/publish/win-x64/XrayUI-Portable.exe` 已生成且发布目录不含 `Data/`。
- 已完成：原多节点配置仅复制到隔离验证副本运行 15 秒，应用未退出；验证完成后该副本及全部旧 `publish-check` 诊断副本已删除，未保留隔离用户配置。
- 已完成：删除旧主项目 `bin/`、主项目 `obj/`、测试项目 `bin/` 和测试项目 `obj/`。这些目录均为可再生缓存；`XrayUI.Tests/` 源码、变更快照和迁移回滚资料仍保留。
- 验证：实施前快照完整性通过；Host Architecture Validation、Host Documentation Validation 和 `git diff --check` 通过；发布工作流及发布配置中不再存在旧主项目 publish 路径引用。
