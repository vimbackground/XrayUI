---
id: SOP-XRAYUI-RETROSPECTIVE-KNOWLEDGE-DISTILLATION
title: 会话与 Work Item 复盘及经验沉淀
document_type: sop
status: active
version: 1.1.0
project: XrayUI
owner: XrayUI maintainers
audience:
  - developer
  - agent
scope: _Dev/SOP
created_at: 2026-09-07T02:00:00+08:00
updated_at: 2026-09-07T03:15:00+08:00
tags:
  - retrospective
  - knowledge
  - improvement
source_of_truth: true
---

# 会话与 Work Item 复盘及经验沉淀

## 复盘范围

可复盘完整会话或单个 Work Item。开发者无需整理技术资料；Agent 应从方案、状态、文件、命令和验证结果中还原事实，只向开发者询问无法从仓库确认的目标感受或取舍。

## 复盘流程

1. 明确复盘对象和时间边界，读取 Agent、current state、Work Item Registry、相关方案、验证结果和既有 retrospective log。
2. 区分事实、推断和开发者反馈，提炼：完成了什么、哪里顺利、哪里失败、根因、如何修正、哪些做法值得复用、哪些约束需要改变。
   - 同时采集未产生文件修改的失败补丁、失败验证、范围增补，以及被后续决定取代的 Decision Item；这些也是可验证过程事实。
3. 只分析本次边界内的新事实，不重复复制既有结论。
4. 将项目级事实追加到 `_System/memory/retrospective_log.md`，并按需要更新 current state、Work Item Registry、ADR、架构或 reviews；使用链接而不是复制完整内容。
5. 判断经验是否稳定、可重复：
   - 稳定流程 merge 到对应 `_Dev/SOP/`；
   - 面向开发者的调用方式 merge 到 `_Dev/manuals/prompt_engineering_collection.md`；
   - 解释性材料 relocate 到 `_Dev/manuals/` 或 `_Dev/References/`。
6. 可用于 vHarness 项目开发和升级的通用经验，必须先去除项目名称、真实路径、账号、密钥、业务数据和偶然实现细节，再解耦为跨项目描述，记录到 `_Dev/References/agent_assisted_development_experience.md`。
7. 对外同步或写入其他 vHarness 仓库始终需要开发者另行批准；本流程只形成候选经验。

## 质量门禁

- 每条结论包含来源、适用条件、反例或限制和建议 action。
- 单次偶然事件默认只进入 retrospective log，不立即升级为 SOP。
- 不为相同语义创建第二套术语、状态文件或 SOP。
- 涉及批量修改多个治理文件时，先按 `SOP_change_planning.md` 建立正式方案。
- 方案或范围多轮修订时，复盘必须检查最终目标、验收、文件清单和 Decision Item 是否一致，不能只引用最后一段聊天确认。

## 输出

Agent 用白话先向开发者总结三件事：这次学到了什么、已经沉淀到哪里、还有什么需要确认；随后列出实际更新文件和验证结果。
