---
id: REVIEW-XRAYUI-VHARNESS-HOST-OPTIMIZATION-20260906
title: XrayUI vHarness Host Repository 适配优化方案
document_type: review
status: verified
version: 1.1.0
project: vHarness
owner: XrayUI maintainers
audience:
  - developer
  - agent
scope: _System/reviews
created_at: 2026-09-06T23:30:00+08:00
updated_at: 2026-09-06T23:55:00+08:00
tags:
  - host-repository
  - optimization
  - validation
schema_version: 1
migration_id: 20260906T231452-0fb1c04b
---

# XrayUI vHarness Host Repository 适配优化方案

## 1. 事实基线

- vHarness Migration Run `20260906T231452-0fb1c04b` 已完成，59 个 adopt 文件通过哈希验证，Project Content 保持原位。
- XrayUI 单元测试 95/95 通过，README 规定的 win-x64 Native AOT 发布成功。
- vHarness common、fusion 和 Python plugin 测试共 27/27 通过。
- `Agent.md`、`current_state.md`、`task_breakdown.md` 和 `system_architecture.md` 仍包含模板项目定位或初始化状态。
- 19 个已安装文档的 scope 仍指向 `_Dist/vharness_template`，不匹配 Host Repository 中的实际路径。
- `_Dev/box` 不存在；`.gitignore` 当前忽略 `Agent.md` 和 `_Dev/`，但未忽略 `_wip/`。
- 当前工作目录没有 `.git`，因此本方案不能确认 tracked/untracked 状态或远端历史。
- `vharness_validator.py --profile repository` 要求 `_Dist`，并扫描 `bin/obj`；绝对路径正则会把 `https://` 的 `s:/` 误判为盘符路径。
- `document_validator.py` 会扫描全部 Project Content，并要求业务与生成目录都有 README，不适用于 Host Repository。
- 首次发布验收发现 MSBuild 默认通配符会编译项目根 `_wip` 快照中的 `.cs` 副本；`.gitignore` 不能阻止该行为。

## 2. 目标

- 将 vHarness 治理内容从通用模板状态适配为 XrayUI 的真实 Host Repository 状态。
- 保留 XrayUI 现有源码布局、GitHub Actions、README 和 `dotnet publish` 交付流程。
- 让架构与文档验证器能够区分 Framework Infrastructure、Developer Resources 和 Project Content。
- 确保必要治理文件以后可以纳入版本控制，同时让本地恢复制品保持非提交状态。
- 保持 Migration Run 的回滚资料可用。

## 3. 非目标

- 不创建 `Tasks/`、`Projects/` 或 `in/review/out` 状态机。
- 不重组 XrayUI 源码、资源、测试和 CI 目录。
- 不为每个 Project Content 目录机械创建 README 或 Frontmatter。
- 不修改应用功能、依赖版本、发布参数或业务文档内容。
- 不初始化 Git、不连接远端、不提交或推送。
- 不删除 `_wip/vharness-migrations/20260906T231452-0fb1c04b`。

## 4. Decision Item 裁决

| Decision Item | action | 理由 |
|---|---|---|
| `_Dist` 是否为 XrayUI 必需目录 | defer | XrayUI 已有 GitHub Actions 与 `dotnet publish` 交付路径；本次采用 Host Repository profile，不复制 vHarness Framework Source 的 `_Dist` 发布物结构。若未来需要仓库内统一交付物，再另案 adopt。 |
| `_Dev/box` 缺失 | adopt | 它是 standards 注册的 Canonical Path 与开发者人工管理边界；创建 README 以使空目录可版本化。 |
| 模板治理文档如何处理 | merge | 保留 vHarness 规则正文，同时将 Agent、架构、状态和 Work Item Registry 融合为 XrayUI 真实上下文。 |
| 模板 scope | relocate | 将 `_Dist/vharness_template/...` scope 改为 Host Repository 中对应 Canonical Path，不移动物理文件。 |
| `Agent.md` 与 `_Dev/` 的忽略规则 | adopt | 删除对应 ignore 项，使 Framework Infrastructure 和 Developer Resources 能被版本控制。 |
| `_wip/` 的版本管理 | preserve | 保留本地恢复资料并新增 ignore 规则，不把迁移备份和运行制品默认提交。 |
| `.git` 缺失 | defer | 需要开发者确认此目录是导出副本还是需要恢复 Git 元数据；本次不执行 Git 初始化或克隆。 |

所有 Decision Item 都保留在方案中并具有明确 action；没有通过删除字段规避决策。

## 5. 变更清单

### [NEW]

- `_Dev/box/README.md`：定义开发者人工管理边界与 Agent 默认只读规则。
- `_System/tools/common/tests/test_host_repository_validation.py`：覆盖 Host Repository 核心结构、Project Content 排除、URL 与生成目录误报回归。

### [MODIFY]

- `Agent.md`：将核心定位改为 XrayUI Host Repository；保留 vHarness 门禁；交付路径采用现有 GitHub Actions/`dotnet publish`，不要求 `_Dist`。
- `.gitignore`：移除 `Agent.md`、`_Dev/` 忽略项；新增 `_wip/`；保持业务敏感数据和构建输出规则。
- `_System/memory/current_state.md`：记录当前真实阶段、Migration Run、验证结果、Git 元数据待确认项和下一步。
- `_System/architecture/task_breakdown.md`：改为 XrayUI Work Item Registry，登记本次适配及后续 Git 决策。
- `_System/architecture/system_architecture.md`：描述 WinUI 3 应用、服务、模型、视图、资源、测试、Rust updater、CI 与 vHarness 治理边界。
- `_System/tools/common/vharness_validator.py`：新增 `host` profile；排除标准构建输出；修复 URL 误判；保持绝对本地路径门禁。
- `_System/tools/common/document_validator.py`：新增 `host` profile，只验证 Framework Infrastructure 与 Developer Resources；不要求 Project Content 目录 README/Frontmatter；接受 Host Repository 文档与 vHarness 受管文档的合法 project 值。
- `_System/tools/common/tests/test_vharness_validator.py`：补充 URL、`bin/obj` 和真实绝对路径测试。
- `_System/tools/common/tests/test_document_validator.py`：补充 Host Repository 范围和 scope 测试。
- `XrayUI-dev.csproj`：排除 `_wip\**` 的 Compile、Content、None、Page 和 ApplicationDefinition 项，防止恢复制品进入构建。
- 下列已安装文档：仅把 `_Dist/vharness_template/...` scope 调整为实际 Canonical Path，并保留 `project: vHarness` 作为受管来源标识：
  - `_Dev/README.md`
  - `_Dev/manuals/README.md`
  - `_Dev/platforms/README.md`
  - `_Dev/platforms/platform_matrix.md`
  - `_Dev/References/README.md`
  - `_Dev/skills/README.md`
  - `_Dev/SOP/README.md`
  - `_Dev/SOP/SOP_agent_platform_adaptation.md`
  - `_Dev/SOP/SOP_resilience_and_disaster_recovery.md`
  - `_Dev/templates/README.md`
  - `_Dev/templates/developer_profile_template.md`
  - `_System/ADR/README.md`
  - `_System/architecture/README.md`
  - `_System/memory/README.md`
  - `_System/recovery/README.md`
  - `_System/reviews/README.md`
  - `plugins/README.md`
  - `plugins/python/README.md`

### [DELETE]

- 无。

## 6. 实施阶段

1. 使用现有 recovery 工具创建并验证变更快照。
2. 更新 Agent、真实状态、系统架构和 Work Item Registry。
3. 修正受管文档 scope，补齐 `_Dev/box/README.md`。
4. 调整 `.gitignore`，不触碰 Project Content 和敏感数据规则。
5. 实现 Host Repository 验证 profile 与回归测试。
6. 排除 MSBuild 对 `_wip` 恢复制品的默认收集。
7. 运行 vHarness 验证、vHarness 测试和项目原有验证命令。
8. 更新本方案状态与 `current_state.md`，记录结果和剩余 Decision Item。

## 7. 验收标准

- `python -B _System/tools/common/vharness_validator.py --dir . --profile host` 通过。
- `python -B _System/tools/common/document_validator.py --dir . --profile host` 通过。
- common、fusion、Python plugin 的全部测试通过。
- `dotnet test XrayUI.Tests/XrayUI.Tests.csproj -c Release` 通过。
- README 中的 win-x64 Native AOT `dotnet publish` 命令通过。
- `_wip` 中的 `.cs` 快照不进入 XrayUI 编译。
- `Agent.md`、`_Dev/` 不再被 `.gitignore` 忽略；`_wip/` 被忽略。
- 不存在对 `Tasks/Projects` 或 `in/review/out` 的新建动作。
- Migration Run 回滚目录及其 `plan.json`、`report.json` 保持可读。

## 8. 风险

- 验证器边界调整过宽可能漏掉 Framework Infrastructure 问题；以明确的受管根和回归测试约束。
- 批量 scope 更新可能破坏文档元数据；由 host 文档验证器和差异检查拦截。
- `.gitignore` 修改后可能暴露此前未纳入版本管理的 Developer Resources；在恢复 Git 元数据后必须先审查待提交文件。
- 当前没有 `.git`，无法在实施阶段使用 Git diff 或 Git 回滚作为证据。
- 若遗漏某类 MSBuild 默认项，`_wip` 仍可能污染编译或发布；通过对所有相关 Item 类型显式 Remove 并执行 Native AOT 发布验收控制。

## 9. 回滚

- 实施前使用 `_System/tools/common/create_change_snapshot.py` 创建快照，并用 `verify_change_snapshot.py` 验证。
- 若任一实施或验证步骤失败，立即停止，不继续后续阶段。
- 使用 `_System/tools/common/restore_change_snapshot.py` 恢复本方案涉及文件。
- Migration Run 自身仍可使用 `_System/tools/fusion/vharness_fusion.py rollback --plan _wip/vharness-migrations/20260906T231452-0fb1c04b/plan.json` 回滚整个 vHarness 安装；本方案不主动执行该操作。

## 10. 待批准

- [x] 已批准并实施本方案全部 [NEW] 与 [MODIFY] 项。
- [x] 已确认本次继续 defer `.git` 恢复/初始化，不将其纳入实施。
- [x] 已确认本次继续 defer `_Dist`，采用 Host Repository profile 与现有发布流程。
- [x] 已批准增补 `XrayUI-dev.csproj` 的 `_wip\**` 排除项。

## 11. 验证结果

- Host Architecture Validation：通过。
- Host Documentation Validation：通过，35 个受管 Markdown、35 个逻辑 ID、22 个 README 目录。
- vHarness common 测试：21/21 通过。
- vHarness fusion 测试：10/10 通过。
- Python plugin 测试：3/3 通过。
- XrayUI 测试：95/95 通过。
- win-x64 Native AOT 发布：通过。
- 首次发布因 `_wip` 快照被 MSBuild 默认通配符收集而失败；经批准增加显式排除后复验通过。
