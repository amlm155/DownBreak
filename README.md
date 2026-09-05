# DownBreak

**第一人称生存沙盒原型** · Unity 6 · 自研引擎框架 + 6 大独立子系统

[![Unity](https://img.shields.io/badge/Unity-6000.5.1f1-000?logo=unity)](https://unity.com/)
[![URP](https://img.shields.io/badge/URP-17.5-blue)](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/manual/index.html)
[![C#](https://img.shields.io/badge/C%23-9.0-239120?logo=c-sharp)](https://docs.microsoft.com/dotnet/csharp/)

> 项目仍在持续开发中。本仓库展示了我在 Unity 上**从零搭建的引擎框架层**与**多个独立可复用的游戏子系统**,全部源码开放,无包装、无 demo 缩水。

---

## 🎮 项目概述

DownBreak 是一款**第一人称生存沙盒**的游戏原型。玩家在程序化生成的废土世界中:
- **第一人称移动 + 攀爬** —— 自定义 MotionCharacterController,支持抓墙、平台跟随、受击稳定
- **技能系统** —— 仿 Unreal GAS 实现的 AbilitySystem,带属性/标签/Buff
- **基地建造** —— 程序化建筑系统,支持蓝图、虚拟网格、群组编辑
- **物品管理** —— 网格背包系统,支持拆分/合并/拖拽
- **对话与任务** —— MVVM 对话系统 + 自实现的 GraphView 编辑器

所有核心系统均**从零自研**,未使用 Unity 内置 CharacterController / Behavior Tree 等黑盒组件,体现从底层搭建复杂 Unity 工程的能力。

---

## 🛠️ 技术栈

| 层 | 选型 |
|---|---|
| 引擎 | Unity `6000.5.1f1`(Unity 6) |
| 渲染 | URP 17.5 + Shader Graph |
| 主语言 | C# 9.0 |
| 异步 | UniTask |
| 动效 | DOTween Pro |
| 自研框架 | MM-FSM / MM-MVVM / MM-EventBus / MM-Saver / Unity-CLI-Bridge |
| 网络协议 | Protobuf(自研代码生成器) |
| 架构模式 | MVC · MVVM · 事件总线 · 对象池 · 容器化 IO |

---

## 💪 核心子系统(全部自研)

### 1. 自研引擎框架 —— `Assets/MieMieFrameTools/`

可独立复用的 Unity 引擎框架,所有子系统可单独引用:

| 子模块 | 路径 | 能力 |
|---|---|---|
| Mono 管理 | `Frame/A_FrameBase/MonoManager` | MonoBehaviour 生命周期统一注册,替代 `FindObjectOfType` |
| 资源加载 | `Frame/B_Assets/MmAssetsMethod` | 随包 / 热更 双模式,支持远端 URL |
| 对象池 | `Frame/C_Pool/` | `GameObjPool` / `ObjectPool` / `IPoolable` 接口,带 Reporter |
| 输入系统 | `Frame/G_InputSystem/` | 2D/3D 输入缓冲,改键支持(`InputManager.ChangeKey`) |
| 定时器 | `Tools/UniTask/UniTimerManager` | 基于 UniTask 的时间轮调度 |
| 编辑器扩展 | `Editor/` | FSM/MVVC 代码生成、Protobuf 下载/生成、文件夹管理、模块目录 |

### 2. 角色控制器 —— `Assets/Scripts/1_System/3C/Character/Mcc/`

`MotionCharacterController` —— 基于物理求解器的角色控制器,不含 Unity 内置 CharacterController:

- **`LedgeSolver`** — 抓墙 / 攀爬边沿检测与求解
- **`PlatformSolver`** — 移动平台跟随 / 相对位移同步
- **`HitStabilityEvaluator`** — 受击后姿态稳定性判定
- **`IMover` 接口** — 与具体运动实现解耦,可换 2D / 3D / 网格式

### 3. GAS 技能系统 —— `Assets/Scripts/1_System/GAS/`

仿 Unreal GAS 在 Unity 上完整实现:

- **`AbilitySystem`** — 技能冷却 / 激活 / 取消 / 实例化
- **`StatSystem`** — 属性系统(血量/耐力/饥饿/... 派生公式)
- **`TagSystem`** — 标签状态(燃烧/中毒/眩晕)及其组合
- **`EffectSystem`** — 持续效果 / Buff / Debuff / 周期触发
- **`GasDemoScene`** — 单场景可独立演示全链路

### 4. 程序化建筑 —— `Assets/Scripts/1_System/Builder/`

类似 Rust / Subnautica 的基地建造:

- **`CubeSystem`** — 立方块接口与基础行为(`ICubeBehaviour` / `CubeBehaviour`)
- **`BuilderSystem`** — 蓝图 / 虚拟网格组 / 群组 / 自定义构建器
- **`Mm_ProceduralBuilding`** — 程序化生成器(枚举驱动 + 网格分布)
- **`Editor/EnumGen`** — 自定义 Inspector 与代码生成
- **`MmScene`** 示例场景 + **`ProBuildScene`** 程序化演示

### 5. 网格背包 —— `Assets/Scripts/1_System/Inventory/GridInventory/`

- 3 种容器实现:`GridContainer` / `RandomContainer` / `PlayerContainer`
- 拖拽 / 拆分 / 合并 / 搜索 / 序列化
- 完整 Sample 场景(基础容器 / 随机容器 / 玩家容器)

### 6. 对话 + 任务 —— `Assets/Scripts/1_System/DialogAndMission/`

- **`DialogSystem`** — MVVM 对话系统,含 `DialogueGraphEditorWindow` 自实现可视化编辑器
- **`QuestSystem`** — 任务系统,支持 `QuestSaveData` / `QuestSaveService` 持久化
- **`Narrative/GraphViewFrame`** —— 自实现的图编辑器框架(节点/边/检视器/缩放/对齐)
- **MVVM 三件套** —— Model / ViewModel(`SperakTypes`)/ View 解耦

---

## ▶️ 如何打开

1. 安装 **Unity Hub**:https://unity.com/download
2. 安装 **Unity 6000.5.1f1**(版本必须严格匹配,否则 Package Manager 会报依赖错误)
3. Unity Hub → **Projects** → **Open** → 选择本目录 `DownBreak/`
4. 首次打开会重建 `Library/`(几分钟到十几分钟),按提示 **Auto-resolve** 即可
5. 打开 `Assets/Scene/Scenes/GameScene/GameScene.unity` 试玩主场景

各子系统独立 Demo 场景位置:
- 角色控制 → `Assets/Scripts/1_System/3C/Character/Mcc/Scene/MccSampleScene.unity`
- GAS → `Assets/Scripts/1_System/GAS/Demo/GasDemoScene.unity`
- 程序化建筑 → `Assets/Scripts/1_System/Builder/_Scripts/Mm_ProceduralBuilding/Scene/ProBuildScene.unity`
- 对话 → `Assets/Scripts/1_System/DialogAndMission/Narrative/DialogSystem/Demo/DemoScene.unity`
- 背包 → `Assets/Scripts/1_System/Inventory/GridInventory/Samples/`

---

## ⚠️ 关于第三方美术资源

`Assets/Arts/ExternalArts/` 下是**第三方商业美术资源包**(Polygon Apocalypse 末日城市包 / 办公室低模包 / LowPolyWeapons 等):

- 本仓库**仅保留 `.meta` 引用文件**(Unity 用 GUID 找回贴图引用所必需)
- **贴图本体未包含**(因资源版权与体积考虑)
- 因此 clone 后打开 Unity,ExternalArts 下的部分材质可能显示为**粉色(贴图缺失)**
- **自研的所有逻辑、场景、动画完全可用**,不受影响
- 这是 Unity 行业惯例——商业资源包通常不进版本控制

---

## 📂 目录结构

```
DownBreak/
├── Assets/
│   ├── Scripts/1_System/      ← 核心游戏系统(自研)
│   │   ├── 3C/Character/      ← 角色控制器
│   │   ├── GAS/               ← 技能系统
│   │   ├── Builder/           ← 程序化建筑
│   │   ├── Inventory/         ← 网格背包
│   │   └── DialogAndMission/  ← 对话与任务
│   ├── MieMieFrameTools/      ← 自研引擎框架库(可独立引用)
│   ├── Arts/InteranlArts/     ← 自制美术资源
│   ├── Arts/ExternalArts/     ← 第三方美术(仅 .meta,贴图未入库)
│   └── Scene/Scenes/          ← 主场景(Start / Game / Test)
├── Packages/                  ← UPM 包依赖
├── ProjectSettings/           ← Unity 工程配置
└── .gitignore                 ← 已排除 Library / Temp / Logs / .codex / .opencode / 构建产物
```

---

## ✍️ 关于作者

**边雨帅** · Unity / C# 客户端开发

- GitHub:[@amlm155](https://github.com/amlm155)
- 博客:[amlm155.github.io](https://amlm155.github.io/)
- 邮箱:3431673637@qq.com