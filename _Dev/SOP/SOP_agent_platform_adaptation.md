---
id: TEMPLATE-SOP-VHARNESS-AGENT-PLATFORM
title: Agent 平台确认与模型建议
document_type: sop
status: active
version: 1.0.0
project: vHarness
owner: vHarness maintainers
audience:
  - developer
  - agent
scope: _Dev/SOP
created_at: 2026-09-06T17:54:48+08:00
updated_at: 2026-09-06T17:54:48+08:00
tags:
  - platform
  - model-selection
  - preflight
source_of_truth: true
---

# Agent 平台确认与模型建议

## 启动确认

Agent 在常规开发、跨平台接管或事故恢复前必须报告：

1. 当前平台和客户端；不可见时标记为 `unknown` 并询问开发者。
2. 当前模型或可见能力层级。
3. 文件读写、Shell、网络、连接器和沙箱边界。
4. 当前目录是否为真实项目根。
5. 任务长度、风险和推荐模型层级。

探针只能报告真实可见信息，不得猜测权限、伪造平台身份或尝试绕过沙箱。

## Vibe Coding 交互

- 平台、项目根、权限和工具能由当前会话直接观察时，Agent 自动检查，不要求开发者提供技术信息。
- 只有不可观察且会影响安全或方案的事项才询问开发者；一次只问少量关键问题，并给出容易理解的选择或例子。
- 对开发者先说明“我能做什么、需要你决定什么”，技术探针和能力层级放在必要证据中。
- 未知平台先停在只读检查，不把对话便利当作写入授权。

## 模型层级建议

| 工作类型 | 推荐层级 |
|---|---|
| 检索、格式化、低风险机械修改 | fast / economical |
| 常规实现、测试和短程 Agent 工作 | balanced coding |
| 架构、审计、复杂迁移和长程任务 | deep reasoning |
| 高风险验收、故障裁决和灾难恢复 | independent strong review |

具体模型名称必须根据当前平台实际可用列表推荐。若信息可能过时，应先核验官方当前信息，不把未经核验的型号固化到规范。

## 中长程任务

- 开始前建议开发者使用适合规划和风险推理的模型。
- 实施可切换到稳定的 coding 层级，但必须保留正式方案和检查点。
- 最终验收优先使用独立复核，避免同一上下文既实施又裁决。
- 平台或模型切换后重新执行环境探针与上下文对齐。
