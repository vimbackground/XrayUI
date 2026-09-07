---
id: SOP-XRAYUI-READ-ONLY-STATE-AUDIT
title: 只读状态核查
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
  - audit
  - state
  - read-only
source_of_truth: true
---

# 只读状态核查

## 目的

在不修改文件或外部状态的前提下，核对文档声称的状态与项目真实状态，适用于接管、继续任务、意外中断、审计和开发者只想了解进度的场景。

## 流程

1. 确认平台、项目根和可读范围。
2. 读取 Agent、system architecture、current state、Work Item Registry 和相关方案。
3. 核对目标文件、测试结果、快照、Migration Run 和必要的生成物；Git 元数据存在时核对工作树，不存在时明确说明限制。
4. 分别列出“记录声称”“当前确认”“两者差异”，不把未验证结果写成已通过。
5. 按影响说明风险、阻塞、可安全进行的下一步和需要开发者决定的事项。

## 只读边界

不得修复、格式化、安装、提交、清理、启动恢复、写入状态文档或发送外部消息。若开发者随后要求修改，转入对应 SOP。

## 输出

先用一段白话说明“现在做到哪里、是否安全、接下来建议什么”，再提供必要证据和未确认项。
