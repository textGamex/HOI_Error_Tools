# HOI Error Tools

## 介绍

> 本项目是一个用于分析HOI4代码错误的工具.
>
> 使用`C#`编写, 基于`.NET 7`, 使用`WPF`作为GUI框架, 采用 MVVM 模式.

## 操作系统要求

仅支持 Windows 10 1809 及以上版本和 Windows 11.

---
## 功能

- [x] 分析代码文件, 生产错误信息.
- [ ] 多语言支持
- [ ] 一键修复部分错误
- [x] 抑制错误
---
## 错误检查范围

- State 文件
	- [x] Id
	- [x] Name
	- [x] Owner
	- [x] State Category
	- [x] Manpower
	- [x] add_core_of
	- [x] Victory Points
	- [x] Buildings
	- [x] Resources
	- [x] Provinces
	- [ ] Local Supplies
	- [ ] 不同剧本支持
- history\countries 文件夹下的国家定义文件
	- [x] Puppets
	- [ ] Ideas (部分支持)
	- [x] Capitals
	- [x] oob (仅检查是否存在)
	- [x] recruit_character
	- [x] set_popularities
	- [x] set_politics
	- [x] set_technology
	- [x] add_to_faction
	- [x] set_autonomy
- common\country_tags 文件夹 (不完全检查)
- common\buildings 文件夹 (不完全检查)
- common\idea_tags 文件夹 (不完全检查)
- common\ideologies 文件夹 (不完全检查)
---
## 技术栈

- 日志框架: `NLog`
- 测试框架: `NUnit`
- Json库: `Newtonsoft.Json`
- DI框架: `Microsoft.Extensions.DependencyInjection`
- MVVM工具包: `CommunityToolkit.Mvvm`
- HOI4解析库: 经过定向优化的`CWTools_Plus` (原项目: `CWTools`)
- WPF 主题: `MaterialDesignInXamlToolkit`