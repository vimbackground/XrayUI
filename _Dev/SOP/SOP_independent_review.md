---
id: SOP-XRAYUI-INDEPENDENT-REVIEW
title: 独立复核
document_type: sop
status: active
version: 1.0.0
project: XrayUI
owner: XrayUI maintainers
audience:
  - developer
  - agent
scope: _Dev/SOP
created_at: 2026-09-07T00:35:00+08:00
updated_at: 2026-09-07T00:35:00+08:00
tags:
  - review
  - verification
  - risk
source_of_truth: true
---

# 独立复核

## 1. 目的与适用场景

用于对方案、实现、迁移、修复或发布结果进行独立证据审查。复核默认只读，目标是发现正确性、安全性、回归、边界和验证缺口，而不是代替实施者继续改代码。

## 2. 前置材料

1. 读取 `Agent.md`、系统架构、当前状态和 Work Item Registry。
2. 获取原始需求、获批方案、变更文件、验证命令与结果；材料缺失时明确标记审查限制。
3. 确认复核范围、风险等级和是否要求重新运行验证。
4. 若当前平台、权限或模型能力不可见，按 `SOP_agent_platform_adaptation.md` 重新确认。

## 3. 复核流程

1. 需求一致性：确认实现覆盖目标且未越过非目标或批准范围。
2. 变更审查：检查调用关系、状态变化、错误路径、并发、资源释放、敏感数据和平台差异。
3. 架构边界：区分 Framework Infrastructure、Developer Resources、Project Content 与 Project Extension，避免跨边界混放。
4. 验证充分性：检查测试是否能捕获目标回归，命令是否实际执行，失败、警告和跳过是否被如实报告。
5. 恢复能力：高风险变更应具有已验证快照、回滚步骤和明确停止条件。
6. 需要时独立复跑关键验证；不得把实施者的口头结论当作验证证据。

## 4. Findings 分级

- Critical：可能造成数据损坏、安全事故、不可恢复发布或核心功能不可用。
- High：主要功能错误、显著回归或验证契约失效。
- Medium：边界场景错误、维护风险或重要测试缺口。
- Low：局部可读性、文档准确性或非阻断改进。

每条 Finding 必须包含文件/位置、可观察证据、影响和建议 action；无法确认的内容标记为推断。

## 5. 结论

- 先列 Findings，按严重度排序；无 Findings 时明确说明，并列出残余风险或未验证范围。
- 给出 `pass`、`pass_with_follow_up` 或 `block` 结论及理由。
- 复核不自动授权修复；需要修改时进入 `SOP_task_execution_and_verification.md` 或 `SOP_change_planning.md`。
- 面向非程序员开发者时，先明确回答“能不能继续使用或发布、最需要担心什么、建议下一步是什么”，再提供 Findings 和技术证据。
