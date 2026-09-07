---
id: SOP-XRAYUI-INTERRUPTION-RECOVERY
title: 意外中断检查与继续
document_type: sop
status: active
version: 1.0.0
project: XrayUI
owner: XrayUI maintainers
audience:
  - developer
  - agent
scope: _Dev/SOP
created_at: 2026-09-07T02:00:00+08:00
updated_at: 2026-09-07T02:00:00+08:00
tags:
  - interruption
  - recovery
  - resume
source_of_truth: true
---

# 意外中断检查与继续

## 适用场景

上次工作因死机、断网、Agent 意外关闭、命令进程中止或平台故障而结束，无法确认最后一步是否完成。与主动暂停不同，本流程先假定现场状态未知。

## 恢复诊断

1. 不立即继续写入，也不重跑可能产生外部影响的命令。
2. 确认当前平台、项目根、权限和时间，读取 Agent、current state、Work Item Registry、相关正式方案与最近交接记录。
3. 按 `SOP_read_only_state_audit.md` 比对记录与物理文件；若 Git 元数据可用，再核对工作树和最近提交，不把 Git 作为必然存在的前提。
4. 检查相关 `_wip` 快照、Migration Run、报告和仍在运行的进程；只检查任务所需范围。
5. 将内容分为：已验证成果、存在但未验证的半成品、未知来源或冲突内容、可以安全重做的步骤。
6. 涉及恢复或覆盖时，按 `SOP_resilience_and_disaster_recovery.md` 先预演并等待开发者批准。

## 恢复继续

1. 用白话说明中断前可能做到哪里、已确认什么、还不确定什么和最安全的继续方式。
2. 只有在目标、现场和下一步均已确认后才继续；必要时先重新运行最小验证。
3. 继续后更新 current state 和 Work Item Registry，记录本次中断、检查证据和恢复结果。

## 停止条件

无法区分用户修改与半成品、快照损坏、输入漂移、恢复会删除或覆盖内容、或需要新的外部授权时，立即停止并请求 Decision Item。
