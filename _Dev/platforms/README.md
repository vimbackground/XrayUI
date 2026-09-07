---
id: TEMPLATE-DIR-VHARNESS-DEV-PLATFORMS
title: Agent Platform Profiles
document_type: readme
status: active
version: 1.0.0
project: vHarness
owner: vHarness maintainers
audience:
  - developer
  - agent
scope: _Dev/platforms
agent_access: read_write_with_approval
created_at: 2026-09-06T17:54:48+08:00
updated_at: 2026-09-06T17:54:48+08:00
tags:
  - platform
  - capability
---

# Agent Platform Profiles

本目录记录 Codex、Antigravity、Claude Code、Cursor 等平台的能力适配框架。平台配置只描述可核验的工具、沙箱、上下文和模型层级，不复制根治理规则。

## 通用配置字段

- `platform_id` 与客户端；
- 平台确认信号和无法确认时的回退；
- 文件、Shell、网络、连接器与审批能力；
- 推荐的 fast、balanced、deep reasoning、independent review 层级；
- 已知限制、更新时间和核验来源。

具体型号在实际任务开始时确认，避免静态文档过期。
