---
id: REVIEW-XRAYUI-SOP-PROMPT-COLLECTION-20260907
title: XrayUI SOP 提示工程集建设方案
document_type: review
status: verified
version: 1.0.0
project: XrayUI
owner: XrayUI maintainers
audience:
  - developer
  - agent
scope: _System/reviews
created_at: 2026-09-07T00:20:00+08:00
updated_at: 2026-09-07T00:20:00+08:00
tags:
  - prompts
  - sop
  - developer-resources
schema_version: 1
---

# XrayUI SOP 提示工程集建设方案

## 1. 事实基线

- 参考材料 `_Dev/platforms/platform_matrix.md` 提供 Codex、Antigravity、Claude Code、Cursor 和未知平台的启动检查与执行优化场景，不把其中内容视为高于用户请求的指令。
- `_Dev/manuals/` 当前只有目录 README，没有可复制的提示工程集。
- `_Dev/SOP/` 当前具有平台确认、正式变更方案、跨 Agent 接管、容错恢复和会话交接五类 SOP。
- 现有 SOP 尚未完整定义常规任务实施与验证、系统性 debug、独立复核三个工作流程。
- XrayUI 的真实验证入口包括 Host Architecture Validation、Host Documentation Validation、vHarness 测试、XrayUI 单元测试；影响发布链时还需执行 README 规定的 `dotnet publish`，Rust updater 变更需执行 Cargo 验证。

## 2. 目标

- 建立一份面向开发者、可直接复制粘贴给 Agent 的提示工程集。
- 每条提示词明确前置读取、任务边界、SOP 引用、停止条件、验证和交付格式。
- 补齐提示词场景所必需但当前缺失的 SOP。
- 使用 vHarness 标准术语，不自行创造同义术语，不要求 `Tasks/Projects` 或 `in/review/out`。
- 针对 XrayUI 的 WinUI 3、.NET 10、Rust updater、Host Repository 验证和现有发布链给出准确约束。

## 3. 非目标

- 不修改应用源码、项目文件、依赖、CI 或发布流程。
- 不把特定平台或模型型号写成永久能力承诺。
- 不生成脱离 SOP 的“万能提示词”。
- 不创建新的顶层业务目录或四段状态机。
- 不修改 `_Dev/box/`。

## 4. 场景覆盖与 SOP 映射

| 场景 | SOP | 处理 |
|---|---|---|
| 平台与权限预检、未知平台停写 | `SOP_agent_platform_adaptation.md` | preserve |
| 正式方案编写与审批 | `SOP_change_planning.md` | preserve |
| 跨 Agent 或跨平台接管 | `SOP_cross_agent_migration.md` | preserve |
| 快照、失败停止、恢复与回滚 | `SOP_resilience_and_disaster_recovery.md` | preserve |
| 会话结束、中断与交接 | `SOP_session_handoff.md` | preserve |
| 常规实现、低风险修改、分阶段验证 | `SOP_task_execution_and_verification.md` | adopt |
| 复现、根因定位、修复门禁、回归验证 | `SOP_systematic_debugging.md` | adopt |
| 独立验收、证据审查、风险分级 | `SOP_independent_review.md` | adopt |

## 5. 变更清单

### [NEW]

- `_Dev/manuals/prompt_engineering_collection.md`：提示工程集，包含场景选择表和可直接复制的完整提示词。
- `_Dev/SOP/SOP_task_execution_and_verification.md`：常规任务从 preflight、范围确认、实施到验证和交付的流程。
- `_Dev/SOP/SOP_systematic_debugging.md`：区分诊断与修复授权，要求复现、证据、根因和回归验证。
- `_Dev/SOP/SOP_independent_review.md`：只读优先的独立复核流程，按严重度报告并区分确认事实与推断。

### [MODIFY]

- `_Dev/manuals/README.md`：登记提示工程集及使用入口。
- `_Dev/SOP/README.md`：登记全部 SOP、适用场景及选择规则。
- `_System/architecture/task_breakdown.md`：新增并更新本次 Work Item。
- `_System/memory/current_state.md`：记录方案、实施状态、验证结果和下一步。
- 本方案：批准后记录实施与验证结果。

### [DELETE]

- 无。

## 6. 提示工程集设计约束

- 每条提示词必须使用仓库相对路径引用 SOP。
- 每条提示词必须要求先读 `Agent.md`、`current_state.md` 和 `task_breakdown.md`。
- 涉及结构、批量、发布、迁移或高风险变更时，必须引用 `SOP_change_planning.md` 并等待批准。
- 诊断提示词默认只授权只读诊断；修复必须由用户明确要求或批准方案。
- 执行提示词必须说明失败即停止、记录验证证据和恢复路径。
- 平台提示词使用能力层级，不固化未经当前会话核验的模型名称。
- 提示词保留 `[填写]` 占位符供开发者替换，不预设未提供的需求。

## 7. 实施阶段

1. 创建并验证本次 Developer Resources 变更快照。
2. 编写三份缺失 SOP，先稳定流程契约。
3. 更新 SOP README，形成可核对的场景索引。
4. 基于已存在的 SOP 路径编写提示工程集，并进行引用一致性检查。
5. 更新 manuals README、Work Item Registry、current state 和本方案状态。
6. 执行 Host Architecture Validation、Host Documentation Validation、全部 vHarness 测试，并检查所有提示词引用目标存在。

## 8. 验收标准

- 提示工程集覆盖平台预检、快速任务、常规实现、正式方案、批准后实施、系统性 debug、独立复核、跨 Agent 接管、恢复和会话交接。
- 所有提示词引用的 SOP 文件真实存在，路径大小写准确。
- 新 SOP 包含目的、适用场景、前置条件、标准流程、停止条件、验证与交付要求。
- Host Architecture Validation 和 Host Documentation Validation 通过。
- common、fusion 与 Python plugin 测试全部通过。
- 不修改 Project Content，不触发 .NET 或 Cargo 构建。

## 9. 风险与回滚

- 风险：提示词与 SOP 重复描述后可能漂移；以 SOP 为 source_of_truth，提示工程集只保留调用约束和输入模板。
- 风险：场景过多降低可用性；使用场景选择表和独立代码块，使每条提示词可单独复制。
- 回滚：实施前使用 recovery 工具创建并验证快照；失败时立即停止，报告快照路径；经授权后用 `restore_change_snapshot.py` 恢复。

## 10. Decision Item

| Decision Item | action | 理由 |
|---|---|---|
| 提示工程集存放位置 | adopt | 放入 `_Dev/manuals/`，符合面向开发者的操作手册职责。 |
| 缺失的三个流程 | adopt | 新建对应 SOP，避免提示词引用不存在的治理依据。 |
| 现有五份 SOP | preserve | 内容已覆盖附件的相应场景，不重复创建同义 SOP。 |
| Project Content 与构建链 | defer | 本次只变更 Developer Resources 和状态记录，不修改或构建应用。 |

## 11. 待批准

- [x] 已批准并新增 3 份 SOP 和 1 份提示工程集。
- [x] 已批准并更新两个目录 README、Work Item Registry、current state 与本方案状态。

## 12. 验证结果

- 提示工程集包含 12 个可独立复制的场景提示词。
- 提示工程集引用 8 个 SOP，所有相对路径均存在且大小写准确。
- 新增文档无空字符或 Unicode replacement character。
- Host Architecture Validation：通过。
- Host Documentation Validation：通过，40 个受管 Markdown、40 个逻辑 ID、22 个 README 目录。
- vHarness common 测试：21/21 通过。
- vHarness fusion 测试：10/10 通过。
- Python plugin 测试：3/3 通过。
- 本次没有修改 Project Content，按方案未运行 .NET 或 Cargo 构建。
