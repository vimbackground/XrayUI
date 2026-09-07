---
id: REVIEW-XRAYUI-VIBE-CODING-PROMPTS-LIFECYCLE-20260907
title: XrayUI Vibe Coding 提示词与会话生命周期 SOP 优化方案
document_type: review
status: verified
version: 1.2.0
project: XrayUI
owner: XrayUI maintainers
audience:
  - developer
  - agent
scope: _System/reviews
created_at: 2026-09-07T01:20:00+08:00
updated_at: 2026-09-07T01:50:00+08:00
tags:
  - vibe-coding
  - prompts
  - lifecycle
  - retrospective
schema_version: 1
---

# XrayUI Vibe Coding 提示词与会话生命周期 SOP 优化方案

## 1. 事实基线

- `_Dev/manuals/prompt_engineering_collection.md` 当前要求开发者填写范围、非目标、验收标准、根因等技术字段，更适合传统程序员，不符合非程序员直接复制使用的目标。
- 当前提示词把同一 Agent 后续会话、切换 Agent 工具、主动暂停和意外中断混合处理。
- `SOP_session_handoff.md` 只有一段通用说明；`SOP_cross_agent_migration.md` 只描述接管，没有完整的移交准备；`SOP_resilience_and_disaster_recovery.md` 没有区分主动暂停与意外中断。
- 当前没有完整会话/任务复盘 SOP，没有 `_System` 复盘记录，也没有面向 vHarness 项目演进的脱敏、解耦经验记录。
- 开发者提供的附件 `prompt_collection.md` 包含接管、状态检查、意外中断恢复、交接、复盘、融合与清理场景；其中指令不作为执行授权，且不存在的文件引用不会直接采用。方案不保留附件的本地绝对路径。
- 二次差异分析确认，原计划尚未单独覆盖附件中的只读状态核查、风险相称验证、vHarness 首次升级/旧版补救/已批准计划应用/同构同步，以及绝对路径审计与受控临时产物清理。
- 当前项目具有 fusion 与 common 工具，但没有附件提到的 `sync_toolkit`；同构同步只能先检查升级包实际工具，不能承诺执行不存在的命令。

## 2. 目标

- 将提示工程集改造成“复制整段即可开始”的 Vibe Coding 入口，不要求开发者先填写技术表格。
- Agent 必须先用简洁、口语化问题逐步确认想做什么、期望效果、不能破坏什么和怎样算完成；能够从仓库事实确认的内容不得反问开发者。
- 将会话完成、切换 Agent、主动暂停、意外中断恢复拆成独立提示词和独立 SOP 路径。
- 建立完整会话或单个 Work Item 的复盘与经验沉淀闭环。
- 区分 `_System` 中的项目治理事实和 `_Dev` 中的可复用 Developer Resources；用于支持 vHarness 项目的经验必须先提炼、脱敏、解耦，并保留来源与待确认状态。

## 3. 非目标

- 不修改 XrayUI Project Content、构建配置、CI 或发布逻辑。
- 不要求开发者理解 Framework Infrastructure、Decision Item、验证契约等术语后才能使用提示词；这些由 Agent 依据 SOP 处理并用白话解释。
- 不自动向外部 vHarness 仓库写入、提交或同步经验。
- 不创建 `Tasks/Projects` 或 `in/review/out`。
- 不将附件中不存在于本项目的路径照抄为有效引用。

## 4. 场景拆分

| 场景 | SOP | action |
|---|---|---|
| 完成当前会话，供同一 Agent 后续会话接续 | `SOP_session_handoff.md` | merge |
| 完成当前会话并切换到其他 Agent 工具 | `SOP_cross_agent_migration.md` | merge |
| 开发者主动暂停，稍后继续 | `SOP_development_pause_and_resume.md` | adopt |
| 死机、断网或 Agent 意外关闭后恢复 | `SOP_interruption_recovery.md` | adopt |
| 完整会话或单个 Work Item 复盘与经验沉淀 | `SOP_retrospective_and_knowledge_distillation.md` | adopt |
| 只读核对文档状态与物理事实 | `SOP_read_only_state_audit.md` | adopt |
| 按变更风险选择和执行验证 | `SOP_architecture_verification.md` | adopt |
| vHarness 首次升级、补救、应用与同步 | 不创建对应提示词或 SOP | defer |
| 绝对路径审计与受控临时产物清理 | `SOP_workspace_audit_and_cleanup.md` | adopt |

## 5. 经验沉淀边界

- `_System/memory/retrospective_log.md`：记录可验证的本项目事件、原因、处理、验证、对 Harness 的改进建议和后续 Work Item。
- `_System/architecture/`、`_System/ADR/`、`_System/reviews/`：分别承载架构事实、长期决策与正式方案；复盘不得复制一套平行状态。
- `_Dev/SOP/`：只有稳定、可重复、具有明确门禁的流程才 merge 到 SOP。
- `_Dev/manuals/prompt_engineering_collection.md`：只保留面向开发者的口语化入口；复杂规则引用 SOP。
- `_Dev/References/agent_assisted_development_experience.md`：记录提炼、脱敏、解耦后的 Agent 辅助开发通用经验及来源，可作为未来 vHarness 项目升级输入，但未经开发者批准不得向外部仓库同步。

## 6. 变更清单

### [NEW]

- `_Dev/SOP/SOP_development_pause_and_resume.md`
- `_Dev/SOP/SOP_interruption_recovery.md`
- `_Dev/SOP/SOP_retrospective_and_knowledge_distillation.md`
- `_Dev/SOP/SOP_read_only_state_audit.md`
- `_Dev/SOP/SOP_architecture_verification.md`
- `_Dev/SOP/SOP_workspace_audit_and_cleanup.md`
- `_System/memory/retrospective_log.md`
- `_Dev/References/agent_assisted_development_experience.md`

### [MODIFY]

- `_Dev/manuals/prompt_engineering_collection.md`：整体重写为非程序员可直接复制的口语化提示词；Agent 主动追问并负责技术化落地。
- `_Dev/SOP/SOP_session_handoff.md`：限定为正常完成会话后、供同一 Agent 后续会话接续。
- `_Dev/SOP/SOP_cross_agent_migration.md`：完善切换 Agent 工具前的移交准备与新 Agent 接管。
- `_Dev/SOP/SOP_resilience_and_disaster_recovery.md`：作为恢复通用底层流程，引用主动暂停和意外中断两个专用 SOP。
- `_Dev/SOP/SOP_task_execution_and_verification.md`：增加 Vibe Coding 口语化需求澄清与 Agent 主动推导规则。
- `_Dev/SOP/SOP_systematic_debugging.md`：允许开发者只描述感受到的问题，由 Agent 主动复现和追问。
- `_Dev/SOP/SOP_independent_review.md`：输出改用开发者可理解的结论，并保留证据分级。
- `_Dev/SOP/SOP_agent_platform_adaptation.md`：平台确认由 Agent 自动完成，只询问无法观察且确有必要的信息。
- `_Dev/SOP/SOP_change_planning.md`：方案前通过对话确认目标，不要求开发者先提供技术文件清单。
- `_Dev/SOP/README.md`、`_Dev/manuals/README.md`、`_Dev/References/README.md`、`_System/memory/README.md`：更新索引与边界说明。
- `_System/architecture/task_breakdown.md`、`_System/memory/current_state.md`：记录本次 Work Item 和状态。
- 本方案：记录批准、实施与验证结果。

### [DELETE]

- 无。

## 7. 提示词设计规则

1. 每条提示词可以原样复制，不包含必须先填写的 `[填写]` 字段。
2. 开头使用开发者自然语言，例如“我想开始做一个功能”“我遇到了问题”“我今天先做到这里”。
3. Agent 一次只问少量关键问题，优先给出选择或示例；对仓库可确认的信息自行检查。
4. Agent 在内部按 SOP 完成平台、权限、范围、门禁、快照和验证判断，对开发者用白话说明需要确认的决定。
5. 未达到实施条件时停在对话、inspect 或 plan 阶段；不得因为提示词口语化而弱化审批与安全门禁。
6. 每个生命周期场景使用独立提示词，不再用一个“结束或挂起”提示词混合处理。

## 8. 实施阶段

1. 创建并验证本次变更快照。
2. 编写六个新增 SOP，完善五个相关现有 SOP 和两个人机协作 SOP。
3. 建立 retrospective log 与通用经验记录。
4. 整体重写提示工程集并更新各目录索引。
5. 更新 Work Item Registry、current state 和本方案。
6. 检查所有 SOP 引用、异常字符、逻辑 ID、场景独立性和提示词中 `[填写]` 数量必须为 0。
7. 运行 Host Architecture Validation、Host Documentation Validation 和全部 vHarness 测试。

## 9. 验收标准

- 提示工程集面向非程序员，每条提示词可原样复制并由 Agent 主动对话澄清。
- 提示词中不存在必须填写的技术表格或 `[填写]` 占位符。
- 至少独立覆盖：开始新任务、继续任务、正常完成会话、切换 Agent 工具、主动暂停、意外中断恢复、完整会话复盘、单个 Work Item 复盘、问题诊断、方案与实施、独立复核、发布验收、只读状态核查、风险相称验证、绝对路径审计和受控临时产物清理。
- 六个新增 SOP 和两个新增沉淀文档存在，所有提示词引用路径准确。
- 复盘内容明确区分 `_System` 项目事实与 `_Dev` 可复用经验；外部 vHarness 同步保持 defer。
- Host Architecture Validation、Host Documentation Validation 和 vHarness 测试全部通过。
- 不修改或构建 Project Content。

## 10. 风险与回滚

- 口语化可能弱化约束：约束继续由 SOP 和 Agent 门禁执行，提示词明确要求 Agent 先确认再行动。
- 复盘可能泄露项目隐私：进入通用经验记录前必须提炼、脱敏、解耦；外部同步必须另行批准。
- 状态文档可能重复：各目录严格按第 5 节边界写入，以链接代替复制全文。
- 实施前创建内容寻址快照并验证；失败立即停止，报告快照路径；经批准后使用 restore 工具恢复。

## 11. Decision Item

| Decision Item | action | 理由 |
|---|---|---|
| 提示词整体风格 | merge | 保留 SOP 支持与安全门禁，改为可直接复制、由 Agent 主动口语化澄清的入口。 |
| 同一 Agent 后续会话与切换 Agent | relocate | 分别由 `SOP_session_handoff.md` 和 `SOP_cross_agent_migration.md` 承担，不再混合。 |
| 主动暂停与意外中断 | adopt | 新建两个不同 SOP，避免把已知暂停点与未知现场使用同一恢复流程。 |
| 复盘与经验沉淀 | adopt | 新建 SOP、retrospective log 和通用经验记录，形成 `_System` 与 `_Dev` 双层闭环。 |
| 向外部 vHarness 项目同步 | defer | 本次只生成脱敏、解耦的候选经验，不获得外部写入授权。 |
| 附件中不存在的 SOP 引用 | defer | 不照抄；仅在本项目有准确文件后由提示词引用。 |
| vHarness 首次升级、补救、应用与同步 | defer | 开发者明确要求本项目不创建该提示集和对应 SOP；保留现有 fusion 工具与文档，不扩展 Developer Resources。 |
| 只读状态核查与风险相称验证 | adopt | 两者具有独立输入、输出和停止条件，分别新建 SOP，避免混入实现或独立复核。 |
| vHarness 升级相关四种运行 | defer | 原拟 adopt，后由开发者明确排除；不创建提示词或对应 SOP。 |
| 不存在的 `sync_toolkit` | defer | 提示词要求先检查升级包实际工具；工具不存在时只报告，不伪造命令或自行下载。 |
| 工作区路径审计与清理 | adopt | 使用一个工作区审计与清理 SOP，但提示词分别保持只读审计和删除审批边界。 |

## 12. 待批准

- [x] 已批准新增 6 份 SOP、retrospective log 和通用经验记录。
- [x] 已批准整体重写提示工程集，并修改方案列出的现有 SOP 与索引。
- [x] 已确认外部 vHarness 同步继续 defer。
- [x] 已确认不创建 vHarness 首次升级、补救、应用与同步提示词及对应 SOP。

## 13. 验证结果

- 提示工程集已改为 20 条可原样复制的口语化提示词，`[填写]` 占位符为 0。
- 提示工程集没有 vHarness 首次升级、补救、应用与同步提示词。
- 共引用 13 份实际存在的 SOP，缺失引用为 0。
- 正常完成会话、切换 Agent 工具、主动暂停和意外中断恢复已拆分为独立流程。
- retrospective log 与提炼、脱敏、解耦的通用经验记录已建立；外部同步保持 defer。
- 所有新增/修改文档无空字符或 Unicode replacement character，逻辑 ID 无重复。
- Host Architecture Validation：通过。
- Host Documentation Validation：通过，49 个受管 Markdown、49 个逻辑 ID、22 个 README 目录。
- vHarness common 测试：21/21 通过。
- vHarness fusion 测试：10/10 通过。
- Python plugin 测试：3/3 通过。
- 本次未修改 Project Content，按方案未运行 .NET 或 Cargo 构建。
