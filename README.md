# 🎯 ClassRollCall — 课堂点名系统

基于 .NET 8 + WPF + WPF-UI 的现代化课堂随机点名工具，支持加权随机、高度公平模式、桌面悬浮窗、权重管理等。

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![WPF](https://img.shields.io/badge/WPF-8.0-5C2D91?logo=dotnet)
![License](https://img.shields.io/badge/license-MIT-green)

---

## ✨ 功能特性

- **加权随机点名** — 被点过的学生权重降低，没被点过的权重升高，确保公平
- **桌面悬浮窗** — 常驻桌面，长按 1.5 秒快速点名，单击弹出人数设置面板
- **权重管理** — 可视化滑块调节权重，一键锁定/解锁，支持总权重归一化
- **点名结果动画** — 横线展开 → 纵向展开 → 名字从右侧逐个滑入
- **学生管理** — 增删改、TXT 导入、Excel/Word 提示（可自行安装 NPOI/DocX）
- **系统托盘** — 关闭窗口最小化到托盘，右键菜单快速操作
- **配置持久化** — JSON 文件保存学生名单、权重、偏好设置
- **深色主题** — WPF-UI Fluent Design 风格



## 🚀 快速开始

### 环境要求

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022（推荐）

### 克隆 & 运行

```bash
git clone https://github.com/LZL760607/ClassRollCall.git
cd ClassRollCall
dotnet run --project WpfApp1
