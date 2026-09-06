---
id: DOC-DIST-VHARNESS-TEMPLATE-PLUGINS-PYTHON-README-MD
title: "Python Validation Plugin"
document_type: readme
status: active
version: 1.0.0
project: vHarness
owner: "vHarness maintainers"
audience:
  - developer
  - agent
scope: plugins/python
created_at: 2026-09-06T17:54:48+08:00
updated_at: 2026-09-06T17:54:48+08:00
tags:
  - documentation
---
# Python Validation Plugin

该插件为 vHarness 模板提供可选的 Python 项目门禁，不改变核心模板的语言中立性。

## 使用方法

在目标项目根目录运行：

```bash
python plugins/python/tools/python_validator.py --dir .
```

验证器只使用 Python 标准库，默认不安装依赖、不导入项目代码，也不生成
`__pycache__`。它会：

- 识别常见项目配置文件，如 `pyproject.toml`、`setup.cfg` 和 `requirements.txt`；
- 解析所有纳入扫描范围的 `.py` 文件，报告 UTF-8 解码或语法错误；
- 探测 `tests/`、`test_*.py` 和 `*_test.py` 测试入口；
- 跳过虚拟环境、缓存、构建产物和 `_wip` 等非源码目录。

退出码为 `0` 表示门禁通过；发现无法解析的 Python 文件时返回 `1`。空项目会给出
警告但仍返回 `0`，便于模板在尚未写入业务代码时通过初始化检查。

## 自测

```bash
python -m unittest discover -s plugins/python/tests -v
```
