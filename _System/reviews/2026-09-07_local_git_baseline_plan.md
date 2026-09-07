---
id: PLAN-XRAYUI-LOCAL-GIT-BASELINE-20260907
title: XrayUI 本地 Git 基线恢复与忽略规则优化方案
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
updated_at: 2026-09-07T01:43:58+08:00
tags:
  - git
  - audit
  - local-baseline
  - gitignore
---

# XrayUI 本地 Git 基线恢复与忽略规则优化方案

## 1. 事实基线

- 平台为 Windows，当前目录经 Git 确认为项目根，当前工作区具有项目内写权限。
- `.git` 已恢复且可用；当前分支为 `main`，HEAD 与 `origin/main` 均为 `62ba18c`（`v1.1.0`），Git 对象连通性检查通过。
- `origin` 指向 XrayUI 项目仓库，`upstream` 指向上游项目；本任务不访问或修改远端。
- 排除 CRLF/LF 差异后，既有 tracked 内容只有 `.gitignore` 与 `XrayUI-dev.csproj` 存在有效差异。
- vHarness 升级成果当前形成 78 个 untracked 文件：`Agent.md`、`XrayUI.code-workspace`、`_Dev/`、`_System/` 与 `plugins/`。
- `_wip/`、根目录和测试项目的 `bin/obj` 已被忽略；没有发现被当前规则误忽略的 tracked 文件。
- 当前状态与 Work Item Registry 仍把 `.git` 恢复列为 defer，已经与物理事实不一致。

## 2. 目标

- 以当前升级后的工作树为准，建立可审查、可回滚的本地 Git 基线。
- 优化 `.gitignore` 的项目根边界、敏感数据、构建产物、测试产物和 vHarness 本地恢复制品规则。
- 验证 Project Content、Framework Infrastructure、Developer Resources 和 Project Extension 均按预期纳入或排除。
- 完成项目原有验证与 vHarness 验证后，仅创建本地提交，不推送 GitHub 或其他远端。
- 更新 current state 与 Work Item Registry，使其反映 Git 已恢复及本地提交结果。

## 3. 非目标

- 不执行 `push`、远端分支创建、Pull Request、Release 或其他远端写入。
- 不执行 `pull`、rebase、merge 或改变当前远端历史。
- 不删除 `_wip/`、构建输出或其他现有文件。
- 不创建 `Tasks/Projects` 或 `in/review/out/_wip` 结构。
- 不修改 Project Content 业务逻辑；`XrayUI-dev.csproj` 仅保留已经验证过的 `_wip` 编译排除规则。

## 4. Decision Item

| Decision Item | action | 理由 |
|---|---|---|
| 当前升级后工作树作为本地 Git 基线 | adopt | 开发者明确要求以升级后的项目库为准；Git HEAD 与 `origin/main` 一致，工作树差异可明确审查。 |
| vHarness 治理与开发资源纳入版本控制 | adopt | `Agent.md`、`_System/`、`_Dev/` 与 `plugins/` 是当前 Host Repository 的必要组成。 |
| `_wip/` 与本地恢复制品 | preserve | 保留物理文件并继续忽略，不进入本地提交。 |
| `.gitignore` 规则优化 | merge | 保留现有业务敏感数据和构建规则，收紧仅属于项目根的本地目录规则，并补齐常见本地秘密与测试报告规则。 |
| `.gitattributes` 与全仓换行规范化 | defer | 当前大量状态噪音来自 Windows 换行检查，但有效内容差异可被识别；本任务不引入可能造成全仓重写的规范化提交。 |
| 本地 Git 提交 | adopt | 在全部验证通过后创建本地提交，形成明确恢复点。 |
| GitHub 或其他远端上传 | defer | 开发者明确要求不要上传；不得执行任何 `git push`。 |

所有 Decision Item 均保留明确 action，不通过删除字段规避未决事项。

## 5. 变更清单

### [NEW]

- `_System/reviews/2026-09-07_local_git_baseline_plan.md`：记录本次审计、批准边界、验证和本地提交结果。

### [MODIFY]

- `.gitignore`：保留现有敏感数据、.NET、Rust、IDE、包缓存和发布制品规则；将明确属于仓库根的本地目录规则锚定到根；补齐必要的秘密文件及覆盖率报告规则，并保留可提交示例文件例外。
- `_System/memory/current_state.md`：把“.git 缺失”改为 Git 已恢复、验证和本地提交的真实状态。
- `_System/architecture/task_breakdown.md`：完成 WI-003，并登记本地基线、验证结果及禁止远端上传的边界。

### [DELETE]

- 无。

### 纳入本地版本控制但不改写内容

- `Agent.md`
- `XrayUI.code-workspace`
- `_Dev/`
- `_System/`
- `plugins/`
- `XrayUI-dev.csproj` 中已经完成的 `_wip` 编译排除改动

## 6. 实施阶段

1. 创建并验证本次变更前 `_wip` 快照；另外记录当前 HEAD、分支和远端配置作为回滚证据。
2. 按批准范围修改 `.gitignore`，用 `git check-ignore` 对敏感数据、构建输出、`_wip`、Harness 文件和 Project Content 做正反例验证。
3. 审核全部有效 tracked 差异和 untracked 清单，确认不存在秘密、本地绝对路径、生成物或非预期大文件。
4. 运行 Host Architecture、Host Documentation、vHarness 测试及 XrayUI Release 单元测试；由于项目文件进入基线，再执行 README 的 win-x64 Native AOT publish。若发现 Rust updater 有有效内容变化，再运行 Cargo 验证。
5. 更新 current state、Work Item Registry 和本方案状态，记录实际验证结果及回滚路径。
6. 执行 `git diff --check`，暂存经审计的升级后项目内容，复核 staged 清单与差异统计。
7. 创建本地提交，再次确认工作树状态、提交 ID 与分支领先情况；明确验证没有执行 push。

任一验证失败立即停止，不创建提交；报告失败命令、已完成动作和快照路径。

## 7. 验收标准

- `git fsck --no-reflogs --connectivity-only` 通过。
- `.gitignore` 正反例检查证明 `_wip`、秘密和生成物被忽略，`Agent.md`、`_Dev/`、`_System/`、`plugins/` 与 Project Content 不被误忽略。
- Host Architecture Validation 与 Host Documentation Validation 通过。
- vHarness common、fusion 和 Python plugin 测试全部通过。
- `dotnet test XrayUI.Tests/XrayUI.Tests.csproj -c Release` 通过。
- README 规定的 win-x64 Native AOT publish 通过。
- `git diff --cached --check` 通过，staged 清单不含 `_wip`、`bin/obj/target`、秘密或其他本地运行数据。
- 本地提交创建成功；`main` 相对 `origin/main` 仅本地领先，未执行 push。
- current state、Work Item Registry 与实际 Git 状态一致。

## 8. 风险

- 把升级后全部内容一次纳入基线可能形成较大的初始提交；通过完整清单、大小和 staged diff 审核控制。
- `core.autocrlf=true` 会制造状态噪音；以 `--ignore-cr-at-eol` 的有效差异审查和 staged diff 为准，不在本任务中做全仓换行重写。
- 忽略规则过宽可能隐藏应提交内容；采用根锚定和正反例验证。
- 忽略规则不足可能纳入秘密或生成物；在暂存前后分别检查 ignored/untracked/staged 清单。
- 本地提交后若直接 push 会上传全部基线；本任务明确禁止 push，并以分支相对远端领先状态作为终态。

## 9. 回滚

- 写入前使用 `_System/tools/common/create_change_snapshot.py` 创建并验证 `_wip/change-snapshots/` 快照。
- 提交前可依据快照恢复受管文件；Git index 在暂存前保持不变。
- 提交后保留提交前 HEAD 与新提交 ID；如未来需要撤销，由开发者另行批准 `git revert` 或其他 Git 操作，不在本任务自动执行破坏性重置。

## 10. 批准状态

- 当前状态：已批准、已实施并通过提交前验收。
- 快照：`_wip/change-snapshots/20260907-local-git-baseline`，完整性验证通过。
- 验证：Host Architecture、Host Documentation、vHarness common 21 项、fusion 10 项、Python plugin 3 项、XrayUI 95 项 Release 测试及 win-x64 Native AOT 发布均通过。
- 安全审计：新增文件最大约 15 KB，未发现凭据特征；`_wip`、构建输出和本地秘密保持忽略。
- Git 边界：仅创建本地提交，不执行 push；远端上传保持 defer。
