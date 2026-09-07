---
id: SOP-XRAYUI-TASK-EXECUTION-VERIFICATION
title: 常规任务实施与验证
document_type: sop
status: active
version: 1.1.0
project: XrayUI
owner: XrayUI maintainers
audience:
  - developer
  - agent
scope: _Dev/SOP
created_at: 2026-09-07T00:35:00+08:00
updated_at: 2026-09-07T00:40:00+08:00
tags:
  - implementation
  - verification
  - work-items
source_of_truth: true
---

# 常规任务实施与验证

## 1. 目的与适用场景

用于 XrayUI 的低风险机械修改、常规功能实现、测试补充和局部重构。结构调整、跨模块或批量修改、发布、迁移、核心功能与高风险工作必须先转入 `SOP_change_planning.md`。

## 2. 前置确认

1. 读取 `Agent.md`、`_System/memory/current_state.md` 和 `_System/architecture/task_breakdown.md`。
2. 按 `SOP_agent_platform_adaptation.md` 确认平台、项目根、读写权限、Shell、网络和沙箱边界。
3. 先让开发者用日常语言说明想要的效果。Agent 主动查看仓库，并用少量、口语化问题确认目标、不能破坏的行为和完成标准；技术文件范围由 Agent 推导并说明。
4. 检查目标文件现状、相关测试和工作区状态；不得覆盖未知用户修改。
5. 判断是否触发正式方案或快照门禁；触发时必须先停在 approve 阶段。

## 3. 标准流程

1. 建立事实基线：定位入口、依赖、调用方和现有验证命令。
2. 将工作拆成最小可验证阶段，每阶段只处理同一职责。
3. 优先复用现有模式、规范术语、命名和工具，不为局部任务建立另一套词表。
4. 实施后先运行最贴近变更的定向验证，再运行受影响范围的回归验证。
5. 涉及 XrayUI C# 逻辑时至少运行相关测试；常规完整测试命令为 `dotnet test XrayUI.Tests/XrayUI.Tests.csproj -c Release`。
6. 涉及发布链时执行 README 规定的 `dotnet publish`；涉及 `updater-rs/` 时执行相应 Cargo 验证。
7. 涉及 Framework Infrastructure 或 Developer Resources 时运行 Host Architecture Validation、Host Documentation Validation 和相关 vHarness 测试。
8. 更新 Work Item Registry；需要跨会话继续时按 `SOP_session_handoff.md` 记录交接。

### Git 本地基线与提交门禁

1. 只有开发者明确要求更新本地 Git 时才暂存或提交；本地提交不包含 push 授权。
2. 暂存前检查有效 tracked 差异、untracked 与 ignored 清单、候选文件大小、秘密特征和本地绝对路径；行尾转换造成状态噪音时，使用忽略行尾差异的视图辅助判断，不自动引入全仓规范化。
3. 暂存后再次运行 `git diff --cached --check`，复核 staged 文件数量、统计和禁入路径，确保恢复目录、构建输出、秘密和运行数据没有进入提交。
4. 提交后确认工作树、提交 ID和相对现有远端引用的 ahead/behind；没有明确授权时不得 fetch、pull、push、merge 或 rebase。
5. Git 检查报告不可达对象但返回成功时，先区分当前历史损坏与可清理旧对象；没有精确清理授权不得 prune。

## 4. 停止条件

- 发现任务范围需要结构、批量、发布、迁移或高风险变更，但尚无获批正式方案。
- 目标文件存在无法归因的修改，继续会覆盖或混合用户工作。
- 验证失败、输入状态漂移、权限不足或依赖来源不可用。
- 连续两轮修改未命中目标或实现偏离已确认架构。

命令本身的语法、转义或编排错误不等于项目验证失败，但也不能记为通过。先确认它没有产生写入或部分状态，再修正命令并完整重跑；项目测试、门禁或内容验证实际失败时仍按上述停止条件处理。

停止时必须报告：已完成内容、失败命令或证据、受影响文件、可恢复路径和需要开发者裁决的 Decision Item。

## 5. 验证与交付

- 只把实际执行且通过的命令标记为通过；警告与跳过项单独说明。
- 最终交付包括结果、变更文件、验证证据、已知限制和仍处于 defer 的 Decision Item。
- 不以“代码已写完”替代验证完成，不把生成目录或本地快照计入 Project Content 改造。
- 对开发者先说明“做成了什么、现在怎么用、是否达到预期”，再列出必要的技术证据；不要要求开发者阅读命令日志才能判断结果。
