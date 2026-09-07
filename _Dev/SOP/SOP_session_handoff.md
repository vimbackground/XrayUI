---
id: SOP-XRAYUI-SESSION-HANDOFF
title: 同一 Agent 后续会话交接
document_type: sop
status: active
version: 2.0.0
project: XrayUI
owner: XrayUI maintainers
audience:
  - developer
  - agent
scope: _Dev/SOP
created_at: 2026-09-06T17:54:48+08:00
updated_at: 2026-09-07T02:10:00+08:00
tags:
  - handoff
  - session
source_of_truth: true
---

# 同一 Agent 后续会话交接

## 适用场景

当前会话正常结束，开发者预计以后继续使用同一 Agent 平台或工具开启新会话。主动暂停但本次会话尚未正常收尾时使用 `SOP_development_pause_and_resume.md`；需要切换 Agent 工具时使用 `SOP_cross_agent_migration.md`。

## 当前会话收尾

1. 停止开始新工作，核对实际文件、方案、验证结果和相关 `_wip` 制品。
2. 区分已完成且已验证、已完成但未验证、半成品、阻塞和未开始内容。
3. 更新 `_System/memory/current_state.md`，记录带时区时间、变更文件、最后验证、警告、Decision Item、快照/回滚路径和下一会话唯一明确的第一步。
4. 更新 `_System/architecture/task_breakdown.md` 的 Work Item 状态。
5. 若本次产生值得复用的经验，按 `SOP_retrospective_and_knowledge_distillation.md` 复盘；不强制每次交接都制造经验条目。
6. 用白话告诉开发者：本次做完了什么、有没有没收尾的内容、下次可以直接复制哪条“继续上次任务”提示词。

## 后续会话接续

1. 新会话读取 Agent、system architecture、current state、Work Item Registry 和相关方案。
2. 核对交接记录与物理文件；Git 可用时核对工作树，不可用时说明限制。
3. 简短复述上次目标、完成情况和建议第一步，等待开发者确认后继续。

不得仅依赖聊天历史，不得伪报验证或自动扩大上次批准范围。
