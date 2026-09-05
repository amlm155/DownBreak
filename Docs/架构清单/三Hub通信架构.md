# DownBreak 通信架构重构清单（三 Hub 方案）

> 目标：统一模块间通信标准，形成"请求走接口 / 通知走事件 / 服务走注册表"的三层铁律。
> 原则：**asmdef 不重拆**（改名会断 prefab 引用，血亏），**不引入 DI 容器**，全部用现有机制收编。

---

## 一、目标架构

```
┌─────────────────────────────────────────────────────┐
│  ModuleHub（框架层 · 已存在 · 不扩大）                  │
│  Pool / Audio / AsyncTask / UniTimer / Archive / UI   │
│  可复用 进 UPM  换游戏都能用                            │
├─────────────────────────────────────────────────────┤
│  UIHub（UI 框架层 · 由 UICoreMgr 改名或保留别名）        │
│  窗口管理 Show/Hide/Stack  面板生命周期                  │
├─────────────────────────────────────────────────────┤
│  GameHub（游戏层 · 新建 · 本次核心）                    │
│  Bag / Weapon / Inventory / Status / Interact 服务     │
│  游戏特化 留主工程 随项目长                             │
└─────────────────────────────────────────────────────┘
         UI面板 = 消费者 不进任何 Hub（窗口归 UIHub 管）
```

## 二、通信铁律（决策表）

| 场景 | 机制 | 代码形态 |
|---|---|---|
| 知道对方 要数据/办事 | 接口引用 | `IBagFacade.TryPickupItem()` |
| 服务全局唯一 对方不确定 | GameHub | `GameHub.Get<IBagFacade>()` |
| 某事发生 关心者未知/多个 | mm-eventbus | `BagEvents.OnChanged.Publish(...)` |
| 共享数据 | Model 归属 | 谁拥有谁改 改完发事件 |

- 请求用调用（有返回值），通知用事件（无返回值）
- 事件绝不带请求语义 不期待回应
- 依赖只走单行道：UI → System → Gameplay → 3C → Data

---

## 三、现状盘点（已核实）

### 已符合标准的（保留不动）
| 项 | 位置 | 说明 |
|---|---|---|
| ModuleHub | `MieMieFrameTools/Scripts/Frame/A_FrameBase/ModuleHub.cs` | `IManagerBase` + `ManagerAttribute` + `GetManager<T>()` 已完备 |
| 事件类雏形 | `2_System/*/Communication/Outer/Event/*.cs` | PlayerStatEvents / GridInventoryEvents / WeaponHudEvents / UiFlowEvents / CombatFeedbackEvents 已用 mm-eventbus |
| 门面接口雏形 | `2_System/*/Communication/Outer/Interface/*.cs` | IUIBagInteract / IPlayerInteract / IDamageable / IPlayerEatPerformance |
| UI 框架层归属 | ModuleHub.GetUI<T>() → UICoreMgr | 已通过反射适配 不用动 |

### 需要收编的（散落单例）
| 类 | 现状 | 收编去向 |
|---|---|---|
| WeaponSystem : Singleton | 自管单例 | GameHub 注册 `IWeaponSystem` 门面 |
| ItemRtDataMgr : Singleton | 自管单例 | GameHub 注册 |
| UIBagInteractCore | BagPanel new 注入 | 保留接口注入 但接口进 GameHub 可查 |
| GridMainContainerManager | 静态类 | 保留（工具型） 不入 Hub |
| PlayerConfig.Instance | 配置单例 | 保留（纯配置） |
| ItemMenuPanel（BagPanel/Host 注入） / WorldItemInfoView.Instance / FloatingTextManager.Instance | UI 局部持有或窗口单例 | ItemMenu 已去全局 Instance；其余窗口类可留 |

---

## 四、分阶段任务

### 阶段 1：GameHub 落地（新增 1 个文件 + 注册 2 个系统）

**1.1 新建 `GameHub.cs`**（放 `Assets/Scripts/2_System/` 下新目录 `GameHub/`，asmdef `DownBreak.System` 内）

```csharp
/// <summary>
/// 游戏层服务注册表 与 ModuleHub(框架) 分工 只装 DownBreak 特化服务
/// </summary>
public static class GameHub
{
    private static readonly Dictionary<Type, object> serviceDict = new();

    /// <summary> 注册服务 同名覆盖警告 </summary>
    public static void Register<T>(T service) where T : class
    {
        serviceDict[typeof(T)] = service;
    }

    /// <summary> 取服务 未注册返回 null </summary>
    public static T Get<T>() where T : class
    {
        return serviceDict.TryGetValue(typeof(T), out var service) ? service as T : null;
    }

    /// <summary> 注销服务 </summary>
    public static void Unregister<T>() where T : class
    {
        serviceDict.Remove(typeof(T));
    }

    /// <summary> 清空 场景切换或框架销毁时调用 </summary>
    public static void Clear()
    {
        serviceDict.Clear();
    }
}
```

**1.2 收编 WeaponSystem**（`WeaponSystem.cs`）
- 新增门面 `IWeaponSystem`（接口只声明公开方法：TryEquipWeapon / ClearWeapon 等）
- `WeaponSystem` 改为实现 `IWeaponSystem`，在 `Awake` 里 `GameHub.Register<IWeaponSystem>(this)`
- 保留 `Instance` 过渡（老代码不破），后续逐步换 `GameHub.Get<IWeaponSystem>()`
- 引用处替换：搜索 `WeaponSystem.Instance` 全部改用 GameHub

**1.3 收编 ItemRtDataMgr**（`ItemRtDataMgr.cs`）
- 同 1.2 流程：加门面接口 → 注册 → 逐步替换

### 阶段 2：UIHub 落地（UICoreMgr 处理）

**2.1 决策：改名 or 别名（二选一，推荐改名）**

改名 `UICoreMgr` → `UIHub`：
- 改名成本已核实：全项目仅 4 文件 47 处引用（UICoreMgr.cs 本体 / ModuleHub.cs 反射字符串 / GameRuntime 两处）
- 步骤：
  1. `UICoreMgr.cs` 改名 `UIHub.cs`（含 .meta 一起改，保持 GUID 不变 → prefab 引用不断）
  2. 类名 `UICoreMgr` → `UIHub`，namespace `MmUIFrameWork.Core` 保留
  3. `ModuleHub.cs:176` 反射字符串 `MmUIFrameWork.Core.UICoreMgr` → `MmUIFrameWork.Core.UIHub`
  4. 引用处 `UICoreMgr.Instance` → `UIHub.Instance`（4 文件 47 处）
- 风险：prefab 序列化字段名若含类型名（`uiCoreMgrBehaviour` 是字段名 不影响）；`SingletonMono<UIHub>` 泛型自引用要同步改

**2.2 确认职责边界**：UIHub 只管窗口生命周期（Show/Hide/Stack/预热），游戏面板逻辑不进 UIHub，进各自面板 partial。

### 阶段 3：子 System 目录规范化（四件套结构）

每个子系统的标准文件夹结构（Communication 目录整体废弃 接口事件全部归位顶层）：

```
2_System/Weapon/              ← 任意子系统
 ├── Interface/   IWeaponSystem.cs     ← 接口文件夹
 ├── Event/       WeaponHudEvents.cs   ← 事件文件夹（mm-eventbus EventKey）
 ├── Core/                            ← 本体大文件夹（内部可再分 Data/Runtime/Modules 等）
 │    ├── WeaponSystem.cs
 │    └── Data/WeaponConfig.cs
 └── 注册：GameHub.Register<IWeaponSystem>(this)  ← 与目录无关
```

| 系统 | 门面接口 | 事件出口 | 本体(Core) | 归属 |
|---|---|---|---|---|
| 背包 | `IBagFacade` | `BagEvents`(新建) | BagModule/BagPanel | 2_System |
| 武器 | `IWeaponSystem` | `WeaponHudEvents`(迁入) | WeaponSystem | 2_System |
| 库存 | `IInventory` | `GridInventoryEvents`(迁入) | ItemRtDataMgr | 2_System |
| 生存数值 | `IPlayerStatus` | `PlayerStatEvents`(迁入) | PlayerController.Stat | 2_System |
| 交互 | `IPlayerInteract` | `CombatFeedbackEvents`(迁入) | PlayerInteractCore | 2_System |
| UI流程 | — | `UiFlowEvents`(迁入 4_UI) | UICoreMgr 侧 | 4_UI |

**Communication 目录处置（整体废弃 包括接口与事件）**：
- `2_System/*/Communication/Outer/Interface/*.cs` → 迁至 `2_System/<子系统>/Interface/`
- `2_System/*/Communication/Outer/Event/*.cs` → 迁至 `2_System/<子系统>/Event/`
- `2_System/*/Communication/` 目录整体删除（meta 随迁 GUID 不变）
- `2_System/SystemCommunication/`（UiFlowEvents）→ 迁至 `4_UI/GameRuntime/Event/`
- 本体文件若散在子系统根目录 → 收拢进 `Core/`（内部组织 Data/Runtime 自主保留）
- 迁完同步改命名空间与 asmdef 引用（迁移后编译验证）

### 阶段 4：Gameplay 层归位（玩法与能力分家）

**边界**：System = 能力框架（GAS/交互/库存/扫描），Gameplay = 玩法编排（何时何地怎么用）。

**重要事实（已核实）**：攻击伤害公式/重击倍率/耐久规则**已经在 System 分离**——`WeaponSystem`+`WeaponScanner` 只有能力（装备/挂点/扫描 只抛命中不结算），伤害公式在 `Interaction/PlayerInteract/Modules/AttackInteractModule.cs`（通过 IPlayerInteract 接口挂载 已符合四件套）。**无需从武器系统拆任何规则**。

| 归属 | 内容 |
|---|---|
| 留在 System | GAS 数值、交互核心（含 AttackInteractModule 伤害规则）、库存框架、武器能力（装备/挂点/扫描） |
| 归位 Gameplay | Tieline / GameFlow / LevelFeature（空壳补实）+ 生存规则（饱食度下降速度 死亡条件 等 P0 内容） |

- `DownBreak.Gameplay.asmdef` 补引用 `DownBreak.System`（System 不引 Gameplay，无循环）
- Gameplay 通过门面接口/事件取数据 不直引用 System 实现
- **例外：GameBootstrap（组装根 Composition Root）**——`3_Gameplay/GameFlow/GameBootstrap.cs` 集中注册服务，必须 FindFirstObjectByType 引用实现类。组装根是唯一允许引用实现的点，业务代码仍走门面

### 阶段 5：3C 收敛（叶子层瘦身）

3C 是纯叶子（运行时无人引用），保留独立程序集**不合入 System**（可复用 Mcc 组件）。但收敛其对 System 实现的 5 处 using：

| 文件 | 现在 using | 改法 |
|---|---|---|
| `PlayerTPCamera.cs` | MmInventory | 经契约接口/事件取数 |
| `AnimationModelSoData.cs` / `IAnimationController.cs` / `PlayerAnimationController.Eat/FP.cs` | DBWeaponSystem | 经 `IWeaponSystem` 门面取武器动画模组 |
| `PlayerConfig.cs` / `PlayerController*.cs` | GAS.StateSystem | 保留（GAS 是契约层 合理） |
| `PlayerController.UIFlow.cs` | UiFlow | UiFlowEvents 迁 UI 后改引用 4_UI 事件 |

目标：3C 从"引用实现"降级为"引用契约"，叶子层引用面从 11 条收敛。

### 阶段 6：迁移与清理（渐进 不爆破）

1. 消费方逐步替换：`Xxx.Instance.` → `GameHub.Get<IXxx>()?.`
2. 替换完毕的旧单例字段保留 `Instance` 但标记 `[Obsolete]`
3. 全部替换后删除旧 Instance（可选 不急）
4. 本清单归档

---

## 五、明确不做（防跑偏）

| 事项 | 原因 |
|---|---|
| asmdef 重拆/改名 | 断 prefab 场景引用 成本远大于收益 |
| 引入 DI 容器(VContainer 等) | 现有手写注册表已够用 单人项目不值 |
| ModuleHub 收编游戏系统 | 污染框架可复用性 职责必须分家 |
| UI 面板进 Hub | 面板是窗口 归 UIHub 生命周期 不注册服务 |
| 静态工具类(如 GridMainContainerManager)收编 | 无状态工具不配注册 |
| 3C 合入 System | 3C 是纯叶子 合入只增耦合 且破坏 Mcc 可复用性 |
| Gameplay 反引 3C/UI | 依赖单行道必须保持 UI → System → Gameplay → 3C → Data |

---

## 六、验收标准

1. `GameHub.Get<IWeaponSystem>()` 与旧 `WeaponSystem.Instance` 行为一致（Unity 编译 + 跑一把）
2. UIHub 改名后 背包/轮盘/设置面板 Show/Hide 全部正常
3. 全工程搜索 `Instance` 剩余项均为"窗口类/配置类/工具类"（合规项）
4. 无新增跨层引用：新代码依赖方向全部满足 UI → System → Gameplay → 3C → Data
5. `Communication/` 目录全部清空删除，事件/接口已归位到各子系统 `Event/` `Interface/`
6. 3C 对 System 的 using 收敛完成（`DBWeaponSystem`/`MmInventory` 引用消失 只剩契约层）
7. `DownBreak.Gameplay` 补引用 System 后编译通过，武器玩法规则文件已归位

---

## 七、改动文件清单（预计）

| 文件 | 动作 |
|---|---|
| `2_System/GameHub/GameHub.cs` | 新增 |
| `2_System/Weapon/Communication/Outer/Interface/IWeaponSystem.cs` | 新增 |
| `2_System/Weapon/WeaponSystem.cs` | 实现接口 + 注册 |
| `2_System/Inventory/.../ItemRtDataMgr.cs` | 同武器流程 |
| `4_UI/MmUIFrameWork/Core/Core/UICoreMgr.cs` | 改名 UIHub.cs + 类名 |
| `MieMieFrameTools/Scripts/Frame/A_FrameBase/ModuleHub.cs` | L176 反射字符串 |
| GameRuntime 引用 UICoreMgr 的 2 处 | 改名同步 |
| 各子系统 `Event/` `Interface/` 目录 | 新增（Communication 内容迁入） |
| `2_System/*/Communication/**` | 迁移后删除 |
| `3_Gameplay/Tieline/ GameFlow/ LevelFeature/` | 空壳补实（按 P0 排期） |
| `DownBreak.Gameplay.asmdef` | 补引用 `DownBreak.System` |
| `1_3C/Camera/PlayerTPCamera.cs` 等 5 处 | using 收敛（经契约接口） |
| 消费方替换文件 | 按搜索 `Instance` 结果逐个替换 |

> 备注：事件类与接口目录已有雏形 本次以"补 GameHub + 收编单例 + UIHub 改名 + 子 System 目录规范化 + 3C 收敛 + Gameplay 归位"为主 不动既有事件/接口内容。

> 变更记录：2026-08-10 新增阶段 3 四件套目录规范（Communication 废弃）、阶段 4 Gameplay 归位、阶段 5 3C 收敛；阶段 6 及以后顺延。

> **执行记录 2026-08-10（全部完成）**：
> - 阶段 1：GameHub.cs 新建（`2_System/GameHub/`，namespace DBGameSystem）；IWeaponSystem 门面（`2_System/Weapon/Interface/`）+ WeaponSystem 实现注册；IInventory 门面（`2_System/Inventory/Interface/`）+ ItemRtDataMgr 实现注册
> - 阶段 2：UICoreMgr → UIHub 改名（含 .meta GUID 保留 fb1df8aa292ebfb4d873ee542181ef5c）；ModuleHub 反射字符串/方法名同步；GameRuntime 8 文件引用替换
> - 阶段 3：Communication 目录全部废弃迁移（Interaction/Inventory/Weapon 的接口→`<子系统>/Interface/` 事件→`<子系统>/Event/`）；SystemCommunication 拆散（PlayerStatEvents→`GAS/Event/`，UiFlowEvents→`2_System/SystemEvent/`）；Weapon 本体收拢 `Core/`（5 文件 + Data 4 文件）
> - 阶段 4：DownBreak.Gameplay.asmdef 补引用 DownBreak.System + MiMieEventBus；IPlayerStatus 门面（`GAS/Interface/`）+ StatController 实现注册；SurvivalRule 生存规则开荒（`3_Gameplay/Survival/`）；GameFlowEvents 死亡事件（`2_System/SystemEvent/`，**注意：契约放 System 层 3C/UI 才能订阅**）
> - 阶段 5/6：3C 5 处 WeaponSystem.Instance → GameHub.Get<IWeaponSystem>()；全项目 WeaponSystem.Instance 18 处 + ItemRtDataMgr.Instance 10 处全部替换清零
> - **IGameService 改造（2026-08-10 追加）**：GameHub 加 `IGameService` 标记接口约束（`Dictionary<Type, IGameService>` + `where T : class, IGameService`）；三个门面接口继承它；改为**集中注册**——移除 WeaponSystem/ItemRtDataMgr/StatController 自我注册，新建 `3_Gameplay/GameFlow/GameBootstrap.cs`（`[DefaultExecutionOrder(10000)]` 组装根 唯一允许引用实现类）统一注册
> - 待 Unity 编译验证（静态检查已过：无 UICoreMgr/WeaponSystem.Instance/ItemRtDataMgr.Instance/Communication 残留 无散落 Register）
