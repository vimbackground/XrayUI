---
id: PLAN-XRAYUI-GIT-RECOVERY-RETROSPECTIVE-20260907
title: XrayUI Git 恢复与本地基线会话复盘方案
document_type: plan
status: verified
version: 1.0.0
project: XrayUI
owner: XrayUI maintainers
audience:
  - developer
  - agent
scope: _System/reviews
created_at: 2026-09-07T01:50:00+08:00
updated_at: 2026-09-07T01:52:36+08:00
tags:
  - retrospective
  - git
  - local-baseline
  - knowledge-distillation
---

# XrayUI Git 恢复与本地基线会话复盘方案

## 1. 复盘边界与事实基线

- 边界：从开发者说明 `.git` 已由 GitHub 同步副本复制回升级后项目开始，到本地提交 `a997c47` 创建并确认 `main` 仅领先本地 `origin/main` 记录 1 个提交为止。
- 本次只分析上一轮完整会话复盘之后的新事实，不重复复制 vHarness 首次融合、Host Repository 适配和提示工程建设的既有结论。
- `.git` 恢复后的只读检查确认项目根、分支、HEAD、远端、对象连通性、ignore 行为，以及不存在 alternates、commondir、linked worktrees 和 submodule 路径依赖。
- 排除 CRLF/LF 差异后，既有 tracked 内容只有 `.gitignore` 与 `XrayUI-dev.csproj` 存在有效差异；vHarness 成果最初表现为 78 个 untracked 文件。
- 正式方案 `_System/reviews/2026-09-07_local_git_baseline_plan.md` 获批后实施；快照 `_wip/change-snapshots/20260907-local-git-baseline` 通过完整性验证。
- 最终本地提交 `a997c47` 纳入 81 个文件，工作树干净，`main` 相对现有 `origin/main` 记录 ahead 1；未执行 push、pull、merge 或 rebase。

## 2. 已还原的过程事实

### 顺利部分

- Git 根、HEAD、remote、fsck 和路径依赖检查快速确认复制的 `.git` 可作为同一仓库元数据继续使用。
- `git diff --ignore-cr-at-eol` 将大量状态噪音收敛到两个真实 tracked 差异，避免误判为全仓业务改动。
- `.gitignore` 正反例、敏感信息、文件大小、staged 禁入路径和 `git diff --cached --check` 共同保护本地基线。
- Host Architecture、Host Documentation、vHarness 34 项测试、XrayUI 95 项 Release 测试和 win-x64 Native AOT 发布全部通过。

### 踩坑与修正

- 新方案最初写入真实本地绝对路径，触发 Architecture Validation；修正为项目根的相对描述。
- 新方案最初使用标准未注册的 `proposed` 状态，触发 Documentation Validation；读取 Standards Registry 后改为 `in_progress`，完成后改为 `verified`。
- 方案是在开发者批准后才发现上述两个治理错误，造成两次停止和再次确认；说明方案本身缺少批准前预验证。
- `git status` 在 `core.autocrlf=true` 下报告大量 modified，实际有效差异很少；说明不能直接以状态行数量判断内容变更。
- 暂存检查发现 Python 测试文件末尾多余空行；以最小格式修改修正并重新运行相关测试。
- 一条只读 PowerShell 审计命令因管道语法错误未执行；随后区分为编排命令错误而非项目验证失败，改用分步变量方式重跑。
- `git fsck` 报告 dangling tree 但退出成功；按“不可达历史对象不等于仓库损坏”处理，没有执行破坏性清理。

## 3. 目标

- 将上述项目事实及 Harness 改进线索追加到 retrospective log。
- 把稳定、可重复的 Git 恢复审计、治理方案预验证和 staged 安全门禁 merge 到现有 SOP，不新建同义 SOP。
- 在提示词集中增加一条非程序员可原样复制的“恢复 Git 并只更新本地”入口。
- 将可支持 vHarness 项目的经验提炼、脱敏、解耦后追加到通用经验文档，不写入任何外部项目。
- 更新 current state 与 Work Item Registry，并通过 Harness 验证。

## 4. 非目标

- 不修改 `.gitignore`、Project Content、构建逻辑或 Git 远端。
- 不执行 GitHub 上传或任何外部 vHarness 项目写入。
- 不新增 Git 专用 SOP；相关稳定流程 merge 到现有正式方案、任务执行和工作区审计 SOP。
- 不重新复盘已经沉淀的 Migration Run、Host Repository 和 Vibe Coding 经验。

## 5. Decision Item

| Decision Item | action | 理由 |
|---|---|---|
| 复盘时间边界 | adopt | 仅覆盖上一轮复盘之后的 `.git` 恢复、忽略规则优化和本地基线建立。 |
| Git 元数据恢复审计流程 | merge | 合入现有工作区审计 SOP，避免创建同义 Git 恢复 SOP。 |
| 治理方案批准前预验证 | merge | 合入正式变更方案 SOP，减少无效 Frontmatter 或路径造成的批准后停止。 |
| staged 提交安全门禁 | merge | 合入常规任务实施与验证 SOP，要求提交前检查差异、禁入路径、秘密和大小。 |
| 开发者 Git 恢复提示词 | adopt | 增加自然语言入口，明确只更新本地、先审计、禁止默认 push。 |
| 通用经验候选 | adopt | 追加脱敏、解耦的 Git 元数据恢复、换行噪音和治理预验证经验。 |
| `.gitattributes` 与全仓换行规范化 | defer | 本次只记录经验，不改变既有 defer 决策。 |
| 外部 vHarness 同步 | defer | 只在本项目形成候选经验，外部写入需要开发者另行批准。 |

所有 Decision Item 均保留明确 action，不通过删除字段规避未决事项。

## 6. 变更清单

### [NEW]

- `_System/reviews/2026-09-07_git_recovery_retrospective_plan.md`：本复盘方案及验收记录。

### [MODIFY]

- `_System/memory/retrospective_log.md`：追加 Git 恢复与本地基线的项目事实、失败、根因、修正和限制。
- `_Dev/SOP/SOP_change_planning.md`：merge 治理方案批准前的元数据、路径与结构预验证。
- `_Dev/SOP/SOP_task_execution_and_verification.md`：merge Git 暂存前后的安全检查及命令失败分类要求。
- `_Dev/SOP/SOP_workspace_audit_and_cleanup.md`：merge `.git` 恢复后的仓库身份、完整性、路径依赖和工作树基线审计。
- `_Dev/manuals/prompt_engineering_collection.md`：新增“恢复 Git 并只更新本地”的可复制提示词。
- `_Dev/manuals/README.md`：同步提示词场景数量与入口说明。
- `_Dev/References/agent_assisted_development_experience.md`：追加脱敏、解耦、跨项目经验。
- `_System/memory/current_state.md`：记录本次复盘完成状态与仍为 defer 的事项。
- `_System/architecture/task_breakdown.md`：新增并完成本次复盘 Work Item。

### [DELETE]

- 无。

## 7. 实施阶段

1. 获批后创建并验证复盘写入前快照。
2. 追加 retrospective log 与通用经验，保持项目事实和跨项目经验分层。
3. merge 三份现有 SOP，并向提示词集增加一个开发者入口；同步 manuals 索引。
4. 更新 current state、Work Item Registry 和本方案状态。
5. 运行 Host Architecture、Host Documentation、vHarness 34 项测试、内部引用与 `git diff --check`。
6. 本次不修改 Project Content，因此不重复运行 .NET、Native AOT 或 Cargo；引用刚完成且与当前治理文档修改无关的验证结果。

## 8. 验收标准

- retrospective log 的新条目只覆盖本次新增边界，包含来源、适用条件、限制和 action。
- SOP 修改使用现有术语并保持单一 source_of_truth，不新增同义流程。
- 新提示词可原样复制，明确 Agent 自行审计、先计划审批、本地提交和禁止默认上传。
- 通用经验不包含项目名称、真实本地路径、账号、密钥或偶然业务细节。
- current state、Work Item Registry、方案、实际 Git 提交和验证事实一致。
- Host Architecture、Host Documentation、vHarness 34 项测试、内部引用和 `git diff --check` 全部通过。

## 9. 风险与回滚

- 风险：重复上一轮经验；通过复盘边界和 EXP 编号检查控制。
- 风险：把项目 Git 细节直接复制为框架规则；通过适用条件、限制和脱敏解耦控制。
- 风险：提示词新增后索引数量不一致；同步更新 manuals README 并执行引用检查。
- 回滚：实施前建立 `_wip/change-snapshots/` 快照；本地 Git 提交 `a997c47` 同时提供已提交基线，复盘修改不触碰该提交内容以外的 Project Content。

## 10. 批准状态

- 当前状态：已批准、已实施并通过验收。
- 快照：`_wip/change-snapshots/20260907-git-recovery-retrospective`，完整性验证通过。
- 写入：项目事实、3 份既有 SOP、第 21 条提示词、manuals 索引、EXP-008 至 EXP-011、current state 与 Work Item Registry 已完成。
- 验证：Host Architecture、Host Documentation、vHarness common 21 项、fusion 10 项、Python plugin 3 项、21 条提示词编号、11 个唯一 EXP、13 个 SOP 引用和 `git diff --check` 全部通过。
- 边界：未修改 Project Content，未重复运行 .NET、Native AOT 或 Cargo；未写入外部项目，未执行 Git 远端同步。
