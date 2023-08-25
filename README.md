# HOI Error Tools

## 介绍

> 本项目是一个用于分析HOI4代码错误的工具.

> 使用`C#`编写, 基于`.NET 6`, 使用`WPF`作为GUI框架, 采用 MVVM 模式.

## 功能

- [x] 分析代码文件, 生产错误信息.
- [ ] 多语言支持
- [ ] 一键修复部分错误
- [x] 抑制错误

## 技术栈

- 日志框架: `NLog`
- 测试框架: `NUnit`
- Json库: `Newtonsoft.Json`
- DI框架: `Microsoft.Extensions.DependencyInjection`
- MVVM工具包: `CommunityToolkit.Mvvm`
- HOI4解析库: 经过定向优化的`CWTools_Plus` (原项目: `CWTools`)
- WPF 主题: `MaterialDesignInXamlToolkit`