---
id: STATE-XRAYUI-WORK-ITEMS
title: XrayUI Work Item Registry
document_type: architecture
status: active
version: 1.0.0
project: XrayUI
owner: XrayUI maintainers
audience:
  - developer
  - agent
scope: _System/architecture
created_at: 2026-09-06T23:40:00+08:00
updated_at: 2026-09-07T01:48:52+08:00
tags:
  - work-items
  - state
---

# XrayUI Work Item Registry

## WI-001：vHarness 首次融合

- [x] 生成并批准 Migration Plan `20260906T231452-0fb1c04b`。
- [x] 执行 apply 与 verify，59/59 文件验证通过。
- [x] 运行 XrayUI 单元测试与 win-x64 Native AOT 发布验证。

## WI-002：Host Repository 适配优化

- [x] 完成项目库审计。
- [x] 创建并批准正式优化方案。
- [x] 创建并验证实施前快照。
- [x] 融合 XrayUI Agent、架构和状态文档。
- [x] 修正受管文档 scope 与版本管理边界。
- [x] 增加 Host Repository 验证 profile 和回归测试。
- [x] 排除 `_wip` 恢复制品进入 MSBuild 默认项目项。
- [x] 完成 vHarness 与 XrayUI 验收。

## WI-003：Git 元数据

- [x] 已确认并恢复同一 XrayUI 项目的 `.git` 元数据；恢复前 `main` HEAD 与 `origin/main` 均为 `62ba18c`，对象连通性检查通过。
- [x] 创建并批准本地 Git 基线方案，优化 `.gitignore` 并完成正反例检查。
- [x] 将升级后的 Framework Infrastructure、Developer Resources、Project Extension 和既有 `_wip` 编译排除规则纳入本地版本控制。
- [x] Host Architecture、Host Documentation、vHarness 34 项测试、XrayUI 95 项 Release 测试和 win-x64 Native AOT 发布通过。
- [x] 创建本地提交；明确未执行 `git push` 或其他远端写入。
- 状态：verified；恢复快照位于 `_wip/change-snapshots/20260907-local-git-baseline`，远端上传保持 defer。

## WI-004：可选 `_Dist`

- [ ] Decision Item：未来如需仓库内统一交付目录，另案评估 adopt。
- 状态：defer；当前继续使用 GitHub Actions、GitHub Releases 与 `dotnet publish`。

## WI-005：SOP 提示工程集

- [x] 分析 Agent 平台适配矩阵和现有 SOP 覆盖。
- [x] 创建并批准正式方案。
- [x] 创建并验证实施前快照。
- [x] 补齐常规任务实施与验证、系统性 debug、独立复核 SOP。
- [x] 生成包含 12 个场景的提示工程集。
- [x] 更新 manuals 与 SOP 索引。
- [x] 验证全部 SOP 引用、文档元数据和 vHarness 测试。

## WI-006：Vibe Coding 与会话生命周期优化

- [x] 二次分析开发者提供的附件并区分参考内容与执行指令。
- [x] 创建、补充并批准正式方案；按开发者要求排除 vHarness 升级提示及对应 SOP。
- [x] 创建并验证实施前快照。
- [x] 新增主动暂停、意外中断、复盘沉淀、只读状态核查、风险相称验证、工作区审计与清理 SOP。
- [x] 分离同一 Agent 后续会话交接与跨 Agent 工具移交。
- [x] 将提示工程集重写为 20 条非程序员可原样复制的口语化入口。
- [x] 建立 `_System` 复盘记录与 `_Dev` 提炼、脱敏、解耦的通用经验记录。
- [x] 完成引用、编码、逻辑 ID、Host 验证和 vHarness 34 项测试。

## WI-007：完整会话复盘与经验沉淀

- [x] 从方案、状态、实际文件、命令和验证结果还原完整会话事实。
- [x] 创建并批准正式复盘方案。
- [x] 创建并验证复盘写入前快照。
- [x] 追加项目复盘事实，保留既有记录并避免重复结论。
- [x] 追加迁移语义验收、验证器边界、路径最小化、方案一致性和安全重写通用经验。
- [x] merge 架构验证、正式方案和复盘 SOP 的稳定约束。
- [x] 完成引用、编码、逻辑 ID、Host 验证和 vHarness 测试。

## WI-008：同一 Agent 会话交接

- [x] 停止开始新的开发工作。
- [x] 核对四份 verified 方案、关键文件、快照和 Migration Run 回滚资料。
- [x] 区分已验证成果、未验证成果、半成品和未开始 Decision Item。
- [x] 重新运行 Host Architecture Validation、Host Documentation Validation 和最新快照完整性检查。
- [x] 将准确交接信息、时钟不一致、恢复路径和下次唯一第一步写入 current state。

## WI-009：Git 恢复与本地基线会话复盘

- [x] 从方案、状态、实际文件、命令、Git 历史和验证结果还原新事实。
- [x] 创建、预验证并批准正式复盘方案。
- [x] 创建并验证复盘写入前快照。
- [x] 追加项目级复盘事实，区分换行噪音、治理验证失败、命令编排错误和仓库完整性限制。
- [x] merge 正式方案预验证、Git 双阶段提交门禁和 Git 元数据恢复审计到 3 份既有 SOP。
- [x] 增加第 21 个可复制提示词，并追加 EXP-008 至 EXP-011 脱敏、解耦通用经验。
- [x] 完成 Host 验证、vHarness 34 项测试、21 条提示词编号、11 个唯一 EXP、13 个 SOP 引用、格式和快照检查。
- 状态：verified；外部 vHarness 同步与全仓换行规范化保持 defer。

## WI-010：多代理可靠性与交互改进

- [x] 已确认并批准正式方案；实施前快照完整性验证通过。
- [x] 修复主代理切换的端口释放竞态，并补充端口释放及超时测试。
- [x] 统一主代理、辅助代理的列表开关、双击和右键菜单。
- [x] 提供按代理查看的动态信息窗口。
- [x] 增加完全退出与 Explorer 通知区重建后的托盘图标自恢复。
- [x] 完成 Release、Native AOT、Host 验证和独立复核。
- [x] 复现并修复原便携 `servers.json` 在多条列表项加载时导致的 Native AOT 启动退出；完整原配置隔离回归启动通过。
- [x] 已完成自动验收：代理控制行、服务器列表和服务器详情卡片的 UE 布局优化；原多节点配置 Native AOT 启动回归通过。
- [x] 已完成残留 Xray 核心端口恢复与列表操作按钮细调的自动验收：严格按目标端口和当前捆绑核心路径清理残留进程，恢复重启保持辅代理状态；99/99 测试、Release、Native AOT 与原多节点配置启动回归通过。实施前快照位于 `_wip/change-snapshots/20260907-xrayui-orphaned-xray-port-recovery`。
- [ ] Decision Item：在桌面复现“主代理关闭后立刻启用”的端口占用，确认自动恢复、辅代理状态保持及紧凑状态色按钮的实际效果。
- [ ] Decision Item：开发者桌面确认双端控制行、列表两行内容与详情卡片无滚动条的视觉效果。
- [ ] Decision Item：在可交互 Windows 桌面执行快速切换、辅助代理开关、动态信息、完全退出和 Explorer 重启的图形烟雾测试。
- 状态：in_progress；自动验收通过，等待图形烟雾测试。方案为 `2026-09-07_xrayui_multi_proxy_reliability_and_experience_plan.md`，快照位于 `_wip/change-snapshots/20260907-xrayui-multi-proxy-reliability`。

## WI-011：构建交付目录与工作区保留优化

- [x] 已确认并批准将本地构建、发布交付根目录 adopt 为 `_Dist/`；实施前快照完整性验证通过。
- [x] 已迁移主项目输出、发布工作流、发布配置和忽略规则；本地 x64 Native AOT 发布成功输出到 `_Dist/publish/win-x64/`。
- [x] 已清理旧构建缓存与全部已完成发布诊断副本，保留变更快照和迁移资料；后续快照自动排除可再生输出。
- 状态：verified；不包含 Git 提交、推送或 GitHub Release。

## WI-012：1.2.0 版本与 GitHub Release 更新说明

- [x] 已批准版本元数据与 GitHub Release 更新说明同步；实施前快照完整性验证通过。
- [x] 已同步应用、更新器、工作流兜底版本及 `CHANGELOG.md`；Release 构建、99/99 测试、win-x64 Native AOT 发布和 changelog 提取验证通过。
- 状态：verified；未创建 Git tag、提交、推送或 GitHub Release。
