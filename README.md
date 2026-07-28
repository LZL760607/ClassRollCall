
# 🎯 ClassRollCall — 课堂点名系统

基于 .NET 8 + WPF + WPF-UI 的现代化课堂随机点名工具。支持**加权随机点名**、**桌面悬浮窗**、**权重可视化调节**、**动画结果弹窗**、**系统托盘后台常驻**。

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![WPF](https://img.shields.io/badge/WPF-8.0-5C2D91)
![License](https://img.shields.io/badge/license-MIT-green)

---

## ✨ 核心功能

### 加权随机点名
- 每人初始权重 1.0，总权重恒等于学生人数
- 被点名后权重按**衰减系数**降低，差值按比例分配给其他人
- 未被点过的学生概率逐渐升高，避免「总点那几个人」

### 桌面悬浮窗
- 常驻桌面右下角，小巧不干扰
- **长按 1.5 秒**快速点名，环状进度条反馈
- **短按**弹出面板设置点名人数
- 支持拖拽移动、鼠标悬停半透明

### 点名结果动画
- 横线展开 → 纵向展开 → 名字从右侧逐个滑入
- 卡片宽度自适应最长名字
- 多人时自动分为多卡片均分屏幕
- 点击任意位置取消倒计时，显示手动关闭按钮
- 基础 3 秒展示 + 每多 1 人延长 1 秒

### 权重管理
- 可视化滑块调节权重（上限为总人数）
- 🔒 锁定/ 🔓 解锁一键切换，红绿颜色提示
- 总权重自动归一化
- 支持一键重置所有权重

### 学生管理
- 增删学生，即时保存
- 支持 TXT 批量导入（一行一个姓名）
- 名单持久化存储在本地 JSON 文件

### 系统托盘
- 关闭主窗口最小化到托盘
- 右键菜单：打开主界面、设置、重启、退出
- 程序退出时自动保存所有数据

---

## 🖥 技术栈

| 技术 | 说明 |
|------|------|
| .NET 8 | 运行时 |
| WPF | UI 框架 |
| [WPF-UI](https://github.com/lepoco/wpfui) 3.0.4 | Fluent Design 主题与控件 |
| [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) 8.2.2 | MVVM 架构 |
| [Microsoft.Extensions.Hosting](https://github.com/dotnet/runtime) 8.0.0 | 依赖注入 |

---

## 📁 项目结构

```
ClassRollCall/
├── App.xaml / App.xaml.cs                # 程序入口、DI 容器、系统托盘
├── InvertBoolConverter.cs                # 布尔取反值转换器
├── Models/
│   ├── Student.cs                        # 学生实体
│   └── StudentInfo.cs                    # 学生信息（支持属性变更通知）
├── ViewModels/
│   └── MainViewModel.cs                  # MVVM 骨架
├── Services/
│   ├── IConfigurationService.cs           # 配置服务接口
│   ├── ConfigurationService.cs           # JSON 读写持久化
│   └── StudentService.cs                 # 学生管理 + 加权随机算法
├── Views/
│   ├── MainWindow.xaml / .cs             # 主窗口（侧边栏导航）
│   ├── SplashScreen.xaml / .cs           # 启动动画
│   ├── DesktopWidget.xaml / .cs          # 桌面悬浮窗
│   ├── ResultPopupWindow.xaml / .cs      # 点名结果动画弹窗
│   ├── WeightManageWindow.xaml / .cs     # 权重管理窗口
│   ├── StyledDialog.xaml / .cs           # Fluent 风格对话框
│   └── Pages/
│       ├── HomePage.xaml / .cs           # 点名主页
│       ├── StudentManagePage.xaml / .cs  # 学生管理页
│       └── SettingsPage.xaml / .cs       # 系统设置页
└── Helpers/
    └── PathHelper.cs                     # 路径工具
```

---

## 🚀 快速开始

### 环境要求

- Windows 10 / 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022（推荐）

### 克隆并运行

```bash
git clone https://github.com/你的用户名/ClassRollCall.git
cd ClassRollCall
dotnet run
```

或在 Visual Studio 中打开 `ClassRollCall.sln`，按 `F5` 运行。

### 安装 NuGet 依赖

```powershell
Install-Package WPF-UI -Version 3.0.4
Install-Package CommunityToolkit.Mvvm -Version 8.2.2
Install-Package Microsoft.Extensions.Hosting -Version 8.0.0
```

---

## 🎲 算法说明

### 加权随机公式

```
P(i) = W(i) / ΣW(j)

点名后：
  被点者  W' = W × decay（默认 0.70，可在设置中调节）
  其他人  W' = W + diff × (W / Σ_others)
  总权重  保持恒定 = 学生人数
```

### 概率示例（5 人，衰减 = 0.70）

| 轮次 | 张三 | 李四 | 王五 | 赵六 | 孙七 | 总权重 |
|------|------|------|------|------|------|--------|
| 初始 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 | **5.00** |
| 张三被点 | 0.70 | 1.08 | 1.08 | 1.08 | 1.08 | **5.02** → 归一化 |

---

## 📦 配置存储

配置文件位于：

```
C:\Users\<用户名>\AppData\Local\ClassRollCall\appsettings.json
```

包含学生名单、点名人数、权重衰减系数、自动权重开关等。

---

## 🔮 可扩展方向

- [ ] Excel 导入 / 导出（NPOI）
- [ ] 语音播报点名结果（System.Speech）
- [ ] 点名历史记录
- [ ] 多班级切换
- [ ] 全屏大字模式（教室投影）
- [ ] 缺勤统计与导出

欢迎提交 Issue 和 Pull Request！

---

## 📄 许可

MIT License © 2025

---

**如果这个项目对你有帮助，请给一个 ⭐ Star！**
```
