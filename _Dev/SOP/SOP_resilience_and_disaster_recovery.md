---
id: TEMPLATE-SOP-VHARNESS-DISASTER-RECOVERY
title: vHarness 容错与灾难恢复
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
  - resilience
  - recovery
  - rollback
source_of_truth: true
---

# vHarness 容错与灾难恢复

## 基本原则

重大变更开始前建立可验证快照；每个阶段形成独立检查点；恢复时先诊断、再预演、后恢复。不得把未验证的复制品当作备份，也不得在恢复过程中覆盖 `_Dev/box/`。

主动暂停且现场状态已知时使用 `SOP_development_pause_and_resume.md`；死机、断网或 Agent 意外关闭导致现场未知时使用 `SOP_interruption_recovery.md`。本 SOP 提供两者共同依赖的快照、预演、恢复和回滚规则。

## 标准流程

1. 记录当前平台、根目录、方案编号与 Git 状态。
2. 运行 `python _System/tools/common/create_change_snapshot.py --dir .` 创建快照。
3. 运行 `python _System/tools/common/verify_change_snapshot.py --snapshot <path>` 验证快照。
4. 分阶段实施，每阶段运行测试和验证器并提交检查点。
5. 发生中断后，先比对工作树、快照清单和最近提交，形成正式恢复方案。
6. 恢复前运行 `restore_change_snapshot.py --snapshot <path>` 预演；经授权后追加 `--apply`。
7. 恢复完成后再次验证快照或运行项目全量门禁。

快照默认写入 `_wip/change-snapshots/`，不进入发布物。恢复工具只恢复快照中记录的文件，不删除快照之外的文件；冲突文件在应用前备份到 `_wip/restore-backup-*`。

## 禁止事项

- 不得跳过预演直接恢复。
- 不得将项目根、用户主目录或未解析变量作为递归删除目标。
- 不得在没有完整性校验的情况下清理最后一份恢复副本。
- `_Dev/box/` 始终遵循开发者专用、Agent 默认只读规则。
