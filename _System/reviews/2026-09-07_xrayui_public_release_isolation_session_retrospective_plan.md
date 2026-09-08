---
id: PLAN-XRAYUI-PUBLIC-RELEASE-ISOLATION-RETROSPECTIVE-20260907
title: XrayUI 公开发布隔离会话复盘方案
document_type: plan
status: draft
version: 1.0.0
project: XrayUI
owner: XrayUI maintainers
audience:
  - developer
  - agent
scope: _System/reviews
created_at: 2026-09-07T18:00:00+08:00
updated_at: 2026-09-07T18:00:00+08:00
tags:
  - xrayui
  - retrospective
  - git
  - github
  - release
---

# XrayUI 公开发布隔离会话复盘方案

## 1. 事实基线

- 本次会话完成了本地私有 Git 与公开 GitHub 的分离：公开历史不含 `_Dev/`、`_System/`、`_wip/`、`Agent.md` 或 `XrayUI.code-workspace`；公开 `main` 已推送。
- 历史净化过程中曾因将 `git-filter-repo` 的文件筛选参数误作仓库定位参数而影响本地共享 Git 元数据。远端未在该事故阶段写入；已从实施前快照恢复完整工作区，并以本地恢复提交保留私有工作状态。
- 公开 Release 首次失败的根因已由开发者提供的 GitHub Actions 日志确认：PowerShell 将 XML `Version` 元素对象写入 output，而不是其文本值，导致 `dotnet publish` 收到错误的版本参数。工作流已修正为读取 `InnerText.Trim()` 并以加引号的环境变量参数传递。
- GitHub Actions run `34107575904` 已成功完成，GitHub Release `v1.2.0` 已于本次会话发布。
- 现有复盘记录已包含早期 Git 恢复、快照、文档门禁与本地基线经验；本次只追加新事实，不重复已有 EXP-008 至 EXP-011。

## 2. 目标与非目标

### 目标

1. 还原并记录本次公开历史隔离、事故恢复、Release 失败诊断与最终发布的项目事实。
2. 将稳定且可复用的“隔离副本执行历史重写”“发布变量的类型安全传递”“远端写入前后核验”流程合并到现有 SOP。
3. 为开发者提示词集补充一个可复制的“私有本地开发资料与公开 GitHub 发布隔离”入口。
4. 将可支持 vHarness 项目的经验脱敏、解耦后追加至 `_Dev/References/agent_assisted_development_experience.md`；不向外部项目写入。

### 非目标

- 不改动应用功能、发布包、GitHub Release、远端分支、标签或工作流触发条件。
- 不删除快照、bundle、恢复备份或其他 `_wip/` 制品。
- 不把本次事故自动提升为跨项目强制 SOP；仅把已验证、条件明确的流程约束写入相应位置。

## 3. 变更清单

### [MODIFY]

- `_System/memory/retrospective_log.md`：追加项目事实、验证、限制和后续 action。
- `_System/memory/current_state.md`：更新公开发布已完成、当前私有/公开分支关系、仍待桌面验收事项与下一步。
- `_System/architecture/task_breakdown.md`：将 WI-013 的已完成事项更新为 verified，并保留私有历史重建这一限制。
- `_Dev/SOP/SOP_interruption_recovery.md`、`_Dev/SOP/SOP_resilience_and_disaster_recovery.md`、`_Dev/SOP/SOP_task_execution_and_verification.md`：仅合并稳定的隔离副本、快照恢复和远端写入核验约束。
- `_Dev/manuals/prompt_engineering_collection.md`：新增面向开发者的公开发布隔离提示词，并维护编号与使用原则一致性。
- `_Dev/References/agent_assisted_development_experience.md`：追加脱敏、跨项目可复用的经验候选。

### [NEW]

- 无。

### [DELETE]

- 无。

## 4. 实施阶段

1. 建立复盘事实清单，区分已验证事实、开发者提供的日志、推断及未验证限制；与既有 EXP 去重。
2. 获批后创建并验证复盘写入前快照。
3. 追加项目级复盘、更新状态与 Work Item；只记录可复核的提交、工作流、Release、验证与恢复事实。
4. 仅将跨项目稳定流程 merge 到既有 SOP，新增提示词入口，追加脱敏经验候选；不建立重复词表或外部同步。
5. 执行文档、架构、引用、逻辑 ID、编码、`git diff --check` 与快照完整性验证；复核不包含本地绝对路径或私有运行数据。

## 5. 验收

- 复盘明确本次完成内容、失败路径、根因、修正和限制，且与 Git 分支、快照、公开 Actions/Release 事实一致。
- SOP 只吸收稳定流程，不把单次具体工具误用固化为普适命令。
- 新提示词可被非技术开发者直接复制，明确不会自动推送或发布。
- 通用经验不含项目名、账号、绝对路径、提交 ID、工作流 URL、节点或用户数据。
- Host Architecture Validation、Host Documentation Validation、适用 vHarness 测试、内部引用与 `git diff --check` 通过。

## 6. 风险、回滚与 Decision Item

- 风险：把单次故障误当作通用规则，导致 SOP 过度复杂。控制方式：仅 merge 条件明确、可重复验证的阶段门禁；工具特定事故保留在项目复盘。
- 风险：复盘文档泄露本地路径、远端账号或运行数据。控制方式：使用仓库相对路径、角色描述和脱敏结论，写后扫描。
- 风险：状态记录覆盖仍待桌面图形烟雾测试的事实。控制方式：WI-010 保持 in_progress，明确区分自动验收与交互验收。
- 回滚：使用复盘写入前快照恢复上述治理文档；不触碰已发布的公开历史与 Release。
- Decision Item：无。外部 vHarness 同步继续 defer，需开发者另行批准。
