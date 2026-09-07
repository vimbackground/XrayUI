---
id: SOP-XRAYUI-DEVELOPMENT-PAUSE-RESUME
title: 开发主动暂停与继续
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
  - pause
  - resume
  - state
source_of_truth: true
---

# 开发主动暂停与继续

## 适用场景

开发者已知即将暂停，例如下班、临时处理其他事务或主动切换项目。此时现场仍可正常访问，不按意外中断处理。

## 暂停流程

1. Agent 停止开始新的修改，完成或安全终止当前最小操作。
2. 核对实际文件、正在运行的命令和最后一次验证结果；不得把未验证内容标记为完成。
3. 若存在不易重建的半成品，放入对应 `_wip/` 并记录用途；不得移动 `_Dev/box/` 内容。
4. 更新 `_System/memory/current_state.md`：暂停原因、准确进度、已修改文件、已通过/失败/未运行的验证、快照或回滚路径、继续时的第一步。
5. 更新 `_System/architecture/task_breakdown.md` 中对应 Work Item，不创建新的顶层业务容器。
6. 用白话向开发者说明“已经安全停在哪里”和“下次只需说什么”。

## 继续流程

1. 读取 Agent、current state、Work Item Registry 和相关方案。
2. 核对记录与物理文件是否一致；只检查与暂停任务相关的 `_wip` 内容。
3. 平台或权限变化时执行平台确认；状态有差异时先转入 `SOP_read_only_state_audit.md`。
4. 向开发者简短复述目标、暂停点和建议下一步；得到确认后继续。

## 停止条件

发现未知修改、状态记录与文件不一致、验证结果不可确认或恢复动作可能覆盖内容时，不继续实现，先报告 Decision Item。
