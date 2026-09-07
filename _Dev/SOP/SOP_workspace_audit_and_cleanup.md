---
id: SOP-XRAYUI-WORKSPACE-AUDIT-CLEANUP
title: 工作区审计与受控清理
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
updated_at: 2026-09-07T02:00:00+08:00
tags:
  - workspace
  - audit
  - cleanup
source_of_truth: true
---

# 工作区审计与受控清理

## 审计流程

1. 先确认开发者授权的目录范围、项目根和权限；默认不扫描项目外目录或 `_Dev/box/`。
2. 只读检查硬编码本地绝对路径、不可读文本、构建输出、缓存、临时文件、旧快照和 Migration Run。
3. 结合 XrayUI 运行方式判断路径是否为源码问题；`bin/obj/target` 中工具生成路径不等于源码违规。
4. 将候选项分为：必须保留、仍用于恢复、可重建、未知来源、建议清理。
5. 先报告精确目标、证据、影响、大小和可恢复性，不直接修改或删除。

## Git 元数据恢复审计

当项目缺失的 `.git` 由同一项目的其他同步副本恢复时，先保持只读并依次确认：

1. Git 识别出的项目根与授权工作区一致，`.git` 不是指向未知位置的间接文件。
2. 当前分支、HEAD、tag、上游跟踪和 remote 身份符合预期；检查只使用现有本地引用，不把 fetch 或 pull 当作只读前置条件。
3. 对象连通性检查通过，并确认没有无法解释的 alternates、commondir、linked worktrees、submodule 或 LFS 本地路径依赖。
4. 分别审查有效 tracked 差异、行尾转换噪音、untracked 内容和 ignored 内容；不能依据 `git status` 的行数直接认定全仓内容已改变。
5. 在任何 reset、checkout、clean、暂存或提交前先建立可验证快照，并由正式方案裁决当前工作树是 preserve、adopt、merge、relocate 还是 defer。
6. dangling 或不可达对象默认记录为限制，不自动清理；只有当前提交链或所需对象缺失时才视为完整性阻断项。

## 清理门禁

- 删除必须获得开发者针对精确目标的单独批准。
- 不得清理最后一份已验证快照、仍关联未完成 Work Item 的 `_wip` 内容、Migration Run 回滚资料或未知来源文件。
- 不得使用项目根、用户主目录、未解析变量、通配符或跨 Shell 拼接作为递归删除目标。
- 优先选择可恢复方式；执行前再次验证绝对目标仍在授权范围内。

## 输出

Agent 用白话区分“可以安全清理”“现在不要动”“需要你决定”，列出精确路径和理由。执行后说明移除了什么、是否可恢复并运行必要验证。
