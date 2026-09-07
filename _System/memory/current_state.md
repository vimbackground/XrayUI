---
id: STATE-XRAYUI-CURRENT
title: XrayUI 当前状态
document_type: state
status: active
version: 1.0.0
project: XrayUI
owner: XrayUI maintainers
audience:
  - developer
  - agent
scope: _System/memory
created_at: 2026-09-06T23:40:00+08:00
updated_at: 2026-09-07T03:45:00+08:00
recorded_at: 2026-09-07T03:45:00+08:00
current_phase: private_local_git_public_github_isolation_final_remote_gate
next_action: await_release_trigger_decision_before_public_force_push
tags:
  - handoff
  - state
---

# XrayUI 当前状态

## 当前阶段

1.2.0 版本与 GitHub Release 更新说明同步已完成并验证：主应用、文件、程序集、Rust 更新器和 GitHub Release 工作流兜底版本均为 1.2.0；Release 构建程序集和最终 win-x64 Native AOT 可执行文件的文件版本均实测为 1.2.0.0，发布目录不含 `Data/`。`CHANGELOG.md` 顶部已增加可被工作流精确提取的 1.2.0 更新说明；Release 构建、99/99 单元测试、Native AOT 发布、快照完整性、Host Architecture、Host Documentation 和 `git diff --check` 均通过。未创建 Git tag、未提交、未推送、未触发 GitHub Actions 或线上 Release；网站托管的应用内更新说明保持 defer。实施前快照位于 `_wip/change-snapshots/20260907-xrayui-120-release-metadata`。

构建交付目录与工作区保留优化已完成并验证：本地编译输出现在位于 `_Dist/build/`，用户可直接从 `_Dist/publish/win-x64/` 取得 x64 发布版；发布配置和 GitHub 发布工作流已同步新路径。Release 构建、99/99 单元测试、win-x64 Native AOT 发布和原多节点配置隔离启动回归均通过，最终发布目录不含 `Data/`。旧主项目和测试项目构建缓存、以及全部旧 `publish-check` 诊断副本已删除；变更快照和迁移回滚资料保留。后续快照自动排除可再生输出，避免再次占用大量空间。实施前快照 `_wip/change-snapshots/20260907-xrayui-build-output-workspace-retention` 完整性通过。

残留 Xray 核心端口恢复与列表按钮细调已经完成自动验收：在正常 2 秒端口等待失败后，只清理同时占用目标端口且路径精确匹配当前便携版捆绑核心的 Xray 进程；恢复启动继续使用已有服务器集合，不改变任何辅代理已连接或未连接状态。列表按钮保持 16px 节点同级字体但收紧内边距，并按主/辅代理状态显示绿或灰色。实施前快照 `_wip/change-snapshots/20260907-xrayui-orphaned-xray-port-recovery` 已验证；Release 构建、99/99 单元测试、win-x64 Native AOT 发布、原多节点配置隔离启动回归、Host Architecture、Host Documentation 与 `git diff --check` 均通过。尚待桌面复现关闭后立即启用主代理，确认自动恢复、辅代理状态保持和按钮视觉效果。

WI-010「多代理可靠性与交互改进」的代码实施和自动验收已完成，等待可交互 Windows 桌面的图形烟雾测试后再结束 Work Item。

补充回归结论：开发者提供的原便携 `servers.json` 在修复前可稳定触发 Native AOT 启动退出（任意两条节点记录即可复现，退出码 `-1073741189`）；`settings.json` 与 `xray_config.json` 不是触发条件。已移除服务器列表模板内的跨作用域 `ElementName` 绑定，改由条目运行时状态控制辅代理 UI。修复后，完整原 `Data` 配置在全新 Native AOT 发布副本中持续运行 15 秒，无退出或应用崩溃事件。

已批准并开始 WI-010 的 UE 布局扩展：控制面板单行双端布局、服务器列表单按钮与两行信息布局、服务器详情无滚动卡片。实施前快照为 `_wip/change-snapshots/20260907-xrayui-ue-layout-refinement`，完整性验证通过。

UE 布局扩展的自动验收已完成：控制行左侧为路由/全局代理/TUN、右侧为完全退出；列表左侧为名称紧邻延迟及 IP/端口/协议两行，右侧为单一主或辅代理按钮和独立端口状态；详情卡片顶对齐且无滚动容器。Release 构建、97/97 单元测试和原多节点配置 Native AOT 启动回归通过。仍待开发者完成桌面视觉烟雾测试后结束 WI-010。

最新范围内细调已自动验收：辅代理项仅显示辅端口；端口与已连接/未连接状态分开且状态分别使用绿/灰色；右侧详情、设置与路由控制区改为内容高度连续布局以移除空白。Release 构建、97/97 单元测试和原多节点配置 Native AOT 启动回归通过，桌面视觉确认仍待完成。

字号与窗口高度细调已自动验收：服务器列表延迟、IP、端口与协议以及操作按钮放大；默认完整窗口高度从 720 收紧为 660，以减少控制行下方空白。Release 构建无警告无错误，97/97 单元测试和原多节点配置 Native AOT 启动回归通过；桌面视觉确认仍待完成。

## WI-010 实施进度

- 已实施：主代理切换串行化、最多两秒的端口释放等待、主代理或辅助代理的双击和右键路径、列表右侧操作按钮、动态信息窗口、完全退出和 Explorer 通知区重建后的托盘恢复。
- 自动验收通过：Release 编译；XrayUI 单元测试 97/97；win-x64 Native AOT publish；Host Architecture；Host Documentation；vHarness common 21/21；`git diff --check`。
- 已知未验证项：本会话没有可交互 Windows 桌面，尚未实际执行快速切换、辅助代理开关、动态信息、完全退出及 Explorer 重启后的图形烟雾测试；这不是自动测试失败。
- 后续交互调整已实施：关闭多代理模式会自动关闭全部辅助代理并隐藏辅助控件；端口和启用或关闭按钮分离；退出移动到详情下方；设置页增加右上角返回控制台；默认窗口与服务器列表空间增大。
- 严重回归已修复：发布项目排除 `Data/`，全新 Native AOT 发布目录确认没有 `Data/`；更新器防御性保留用户 Data；无法解析的 settings 或 servers JSON 会改名备份后以空配置启动；导入仅接受完整的分享节点。完全退出已按反馈恢复到控制面板的路由状态与 TUN 同一行。
- 验证限制：当前环境没有 Cargo，`updater-rs` 的修改尚未完成本机 Cargo 编译；需要在有 Rust 工具链的 Windows 环境补跑 Cargo 验证和真实覆盖更新测试。
- 回滚快照：`_wip/change-snapshots/20260907-xrayui-multi-proxy-reliability`，完整性已通过。

## 已确认事实

- XrayUI 是 WinUI 3 / .NET 10 Windows 桌面应用，包含 `XrayUI.Tests` 和独立的 `updater-rs`。
- XrayUI 单元测试 95/95 通过，win-x64 Native AOT 发布验证通过。
- vHarness 安装文件 59/59 哈希验证通过，Project Content 保持原位。
- 原始 `vharness_upgrade` 发布目录已移除；受管工具位于 `_System/tools/`。
- `.git` 已从同一项目的 GitHub 同步副本恢复；`main` 的恢复前 HEAD 与 `origin/main` 均为 `62ba18c`，对象连通性、分支和远端配置检查通过。

## 当前变更

- 正式方案：`_System/reviews/2026-09-06_xrayui_vharness_host_optimization_plan.md`。
- 实施前快照：`_wip/change-snapshots/20260906-xrayui-vharness-host-optimization`。
- Migration Run 回滚资料：`_wip/vharness-migrations/20260906T231452-0fb1c04b`。
- Host Architecture、Host Documentation、vHarness 34 项测试、XrayUI 95 项测试和 win-x64 Native AOT 发布均已通过。
- `XrayUI-dev.csproj` 已排除 `_wip\**`，本地恢复制品不会进入应用构建。
- SOP 提示工程集已完成：`_Dev/manuals/prompt_engineering_collection.md` 包含 12 个场景，并准确引用 8 个现有 SOP。
- 新增 `SOP_task_execution_and_verification.md`、`SOP_systematic_debugging.md` 和 `SOP_independent_review.md`；本次快照位于 `_wip/change-snapshots/20260907-sop-prompt-engineering-collection`。
- 本次 Host Architecture、Host Documentation 和 vHarness 34 项测试全部通过；未修改 Project Content，因此未运行 .NET 或 Cargo 构建。
- Vibe Coding 提示词集已完成：20 条提示词可原样复制，由 Agent 主动用口语化对话确认需求；不包含 vHarness 首次升级、补救、应用或同步提示词。
- 新增 6 份会话生命周期、复盘、状态核查、验证及工作区 SOP，并分离正常会话交接、跨 Agent 工具移交、主动暂停和意外中断恢复。
- `_System/memory/retrospective_log.md` 与 `_Dev/References/agent_assisted_development_experience.md` 已建立；外部 vHarness 同步保持 defer。
- 本次快照：`_wip/change-snapshots/20260907-vibe-coding-prompts-lifecycle`；Host 验证和 vHarness 34 项测试通过。
- 完整会话复盘已完成事实还原并写入本地沉淀：新增 5 条脱敏、解耦通用经验，merge 3 份既有 SOP；正式方案为 `_System/reviews/2026-09-07_full_session_retrospective_plan.md`。
- 复盘写入前快照：`_wip/change-snapshots/20260907-full-session-retrospective`；外部 vHarness 同步未执行。
- 完整会话复盘验收通过：Host Architecture、Host Documentation、vHarness 34 项测试、内部引用、编码、逻辑 ID、EXP 编号和快照完整性均通过。
- 本地 Git 基线恢复完成：优化 `.gitignore`，将 vHarness 治理、Developer Resources 和 Project Extension 纳入版本控制，保留 `_wip` 为本地恢复制品；没有执行远端上传。
- Git 基线验收通过：Host Architecture、Host Documentation、vHarness 34 项测试、XrayUI 95 项 Release 测试和 win-x64 Native AOT 发布均通过；NuGet 漏洞数据源不可访问仅产生 NU1900 警告。
- 本次 Git 基线快照：`_wip/change-snapshots/20260907-local-git-baseline`；恢复前 HEAD 为 `62ba18c`。
- Git 恢复与本地基线会话复盘已完成事实还原和分层写入：项目事实进入 retrospective log，稳定流程 merge 到 3 份既有 SOP，提示词集增加第 21 个本地 Git 恢复入口，并新增 EXP-008 至 EXP-011 通用经验候选。
- 本次复盘写入前快照：`_wip/change-snapshots/20260907-git-recovery-retrospective`；外部 vHarness 同步未执行。
- Git 恢复复盘验收通过：Host Architecture、Host Documentation、vHarness 34 项测试、21 条提示词编号、11 个唯一 EXP、13 个 SOP 引用、格式和快照完整性均通过。

## 下一步

本地私有 Git 与公开 GitHub 隔离已完成本地恢复、净化与构建验收：私有恢复提交为 `492e971`，公开同步提交为 `a805177`，公开路径检查、99/99 测试及 win-x64 Native AOT 发布通过。旧公开历史 bundle 和实施前快照均已验证。此前一次本地工具参数误用只影响本地 Git 元数据，未触达远端；完整工作区已由快照恢复，私有旧提交对象改以新的恢复提交保留。当前唯一阻断是：推送公开 `main` 会触发 GitHub Release 工作流。等待开发者明确选择“允许同时发布 1.2.0”或“先只安排非发布的远端净化”；选择前不得 push、改远端 tag 或创建 Release。

## 2026-09-07 同一 Agent 会话交接

- 实际记录时间：`2026-09-07T01:08:50+08:00`。当前系统时钟早于部分先前文档中的 `02:xx–03:xx` 时间；保留既有历史，不把时间顺序作为完成证据，下次会话需要继续以文件和验证事实为准。
- 已完成且已验证：vHarness Migration Run、Host Repository 适配、初版 SOP 提示工程集、20 条 Vibe Coding 提示词与会话生命周期 SOP、完整会话复盘与 EXP-001 至 EXP-007 通用经验。
- 最后核对：Host Architecture Validation 通过；Host Documentation Validation 通过（50 个受管 Markdown、50 个逻辑 ID、22 个 README 目录）；完整会话复盘快照完整性通过。
- 已完成但尚未验证：无。
- 半成品：无；没有正在运行的命令或需要隔离的新文件。
- 本段是本地 Git 恢复前的历史交接记录；其中 `.git` defer 已由后续批准的本地 Git 基线方案 superseded。可选 `_Dist` 与向外部 vHarness 项目同步经验仍保持 defer。
- 恢复路径：`_wip/change-snapshots/20260907-full-session-retrospective`；更早阶段快照仍位于 `_wip/change-snapshots/`。
- Migration Run 回滚资料：`_wip/vharness-migrations/20260906T231452-0fb1c04b`。
- 下次唯一第一步：读取 `Agent.md`、本文件和 `_System/architecture/task_breakdown.md`，核对物理文件后向开发者复述状态，并等待开发者选择下一个 Work Item。
