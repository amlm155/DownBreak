# AB 打包策略铁律

## 普适六条

| 序号 | 规则 | 一句话解释 |
|------|------|-----------|
| 1 | **同生命周期同包** | 同时加载 同时卸载的资源打一起 永不碰面的资源分开 |
| 2 | **共享依赖独立** | 被 ≥2 个包引用的资源单独成包 防止一份贴图打进 3 个 ab |
| 3 | **粒度适中** | 单包太大会拖慢峰值内存 太碎会导致 IO 次数爆炸 |
| 4 | **启动最小化** | 首屏必须用的塞随包 其余全部延迟到用时加载 |
| 5 | **高频变动分离** | 频繁更新的打出单独的更新单元 不动的不需要重下 |
| 6 | **依赖链扁平** | 包 A → 包 B → 包 C 这种链式依赖尽量避免 降低加载等待串行 |

---

## 编辑器四种分包策略速查

| 策略名称 | 字段 | 效果 | 适用场景 |
|----------|------|------|----------|
| **模块整包** | `wholePackFiles: BundleFileInfo[]` | AB名 + 文件夹路径 一对一 整个文件夹递归打进一个 AB | 需要手动组合多个文件夹到同一个 AB 纯单一文件夹打包 |
| **子文件分包** | `subFolderPacks: string[]` | 指定父目录 每个子文件夹自动生成一个 AB 名称用子文件夹名 | 子文件夹天然对应逻辑分组 如武器大类的子文件夹 |
| **预制体分包** | `prefabPacks: string[]` | 目录下每个 .prefab 单独打一个 AB 自动收集依赖 | 单个大预制体独立加载 如场景超大模型 |
| **场景分包** | `scenePacks: string[]` | 每个场景单独打 AB 自动收集场景依赖 | 按场景划分资源包 |

模块整包中 多个 `BundleFileInfo` 条目使用**相同 abName 时** 会将多个文件夹的内容合并到同一个 AB 中 这是跨文件夹合并打包的关键手段

---

## UI 加载三档

| 档位 | 时机 | 内容 | 打包要求 |
|------|------|------|----------|
| **首屏阻塞** | `GameUIInitScript.Start()` 同步等待 | PlayerPanel ItemWheel 页面图标 | 必须进 BuiltIn 否则首屏白屏等加载 |
| **后台预热** | `WarmUpWindowsAsync` 异步分帧 不阻塞 | BagPanel全家桶 全部装备图标 TipPanel | 必须进 BuiltIn 否则预热阶段触发下载等待 背包打开变慢 |
| **按需懒加载** | 首次使用时触发 | GameStopPanel SettingPanel 3DItemInfoPanel | 可放 BuiltIn 也可走热更 体感影响小 |

---

## 模块划分与打包策略

按功能域拆分为 6 个独立模块 每个模块可独立设置交付方式

| 模块 | 交付方式 | 模块内 AB | 说明 |
|------|---------|----------|------|
| `Player` | BuiltIn | player | 玩家模型 手臂 基础动画 启动即用 |
| `UI` | BuiltIn | ui_hud ui_bag ui_popup icon | 全部 UI 面板与图标 |
| `Weapon` | HotUpdate | weapon_small weapon_onehand weapon_twohand | 全部武器模型 |
| `Consumable` | HotUpdate | consum_food consum_med | 食物药品模型+动画 |
| `Config` | HotUpdate | config | 全部 SO 配置 平衡性热更用 |
| `World` | HotUpdate | world | 搜刮容器等世界物件 |

模块间独立打包 独立交付 互不干扰。产物路径 `BuildOutput/Bundles/{模块名小写}/{平台}/` 包名为 `{模块名小写}_{AB名}`

### Player 模块（BuiltIn 随包 首帧起驻留）

**策略：模块整包**

| AB 名称 | 路径 | 说明 |
|---------|------|------|
| `player` | `Assets/Arts/InteranlArts/Prefabs/PlayerModel/` | 玩家角色控制器 prefab |
| `player` | `Assets/Arts/InteranlArts/Prefabs/Arms/` | 手臂模型 3 个文件 |
| `player` | `Assets/Arts/InteranlArts/Aniamtions/PlayerAm/OriginCc/` | 基础动画 Clip + Controller |
| `player` | `Assets/Arts/InteranlArts/Aniamtions/PlayerAm/Fp/` | FP Override Controller（含空手/单武器/双武器/小刀/防身喷雾/手电筒/进食/药品 8 个） |

> 多个条目使用相同 abName=`player` 合并为同一个 AB

### UI 模块（BuiltIn 随包 首屏+预热+按需）

**策略：模块整包**

| AB 名称 | 路径 | 说明 |
|---------|------|------|
| `ui_hud` | `Assets/Arts/InteranlArts/Prefabs/UIPanel/PlayerPanel/` | PlayerPanel prefab 首屏 |
| `ui_hud` | `Assets/Arts/InteranlArts/Prefabs/UI/UIBase/ItemWheel/` | UIItemWheel prefab 首屏 |
| `ui_bag` | `Assets/Arts/InteranlArts/Prefabs/UIPanel/BagPanel/` | BagPanel 全部 5 个 prefab 预热 |
| `ui_bag` | `Assets/Arts/InteranlArts/Prefabs/UI/UIBase/GridInventory/` | GridInventory 全部 5 个 prefab 预热 |
| `ui_popup` | `Assets/Arts/InteranlArts/Prefabs/UIPanel/GameStopPanel/` | GameStopPanel prefab 按需 |
| `ui_popup` | `Assets/Arts/InteranlArts/Prefabs/UIPanel/SettingPanel/` | SettingPanel prefab 按需 |
| `ui_popup` | `Assets/Arts/InteranlArts/Prefabs/UIPanel/TipPanel/` | TipPanel prefab 按需 |
| `ui_popup` | `Assets/Arts/InteranlArts/Prefabs/UIPanel/Wrold/` | 3DItemInfoPanel prefab 按需 |
| `icon` | `Assets/Arts/InteranlArts/Icons/` | 全部图标 Sprite 多面板共享 |

### Weapon 模块（HotUpdate 热更 按需加载）

**策略：模块整包**

| AB 名称 | 路径 | 说明 |
|---------|------|------|
| `weapon_small` | `Assets/Arts/InteranlArts/Prefabs/Items/Model/Weapon/小刀短柄类/` | 3 个武器 prefab |
| `weapon_onehand` | `Assets/Arts/InteranlArts/Prefabs/Items/Model/Weapon/单手长柄类/` | 3 个武器 prefab |
| `weapon_twohand` | `Assets/Arts/InteranlArts/Prefabs/Items/Model/Weapon/双手类/` | 3 个武器 prefab |

> 动画 OverrideController 已在 Player 模块中 不需要重复打入

### Consumable 模块（HotUpdate 热更 按需加载）

**策略：模块整包**

| AB 名称 | 路径 | 说明 |
|---------|------|------|
| `consum_food` | `Assets/Arts/InteranlArts/Prefabs/Items/Model/FoodAndWater/` | 沙丁鱼罐头 2 个 prefab |
| `consum_food` | `Assets/Arts/InteranlArts/Aniamtions/ItemAm/Food/` | 食物动画 Oringin + Override |
| `consum_med` | `Assets/Arts/InteranlArts/Prefabs/Items/Model/Medicine/` | 注射器 3 个 prefab |
| `consum_med` | `Assets/Arts/InteranlArts/Aniamtions/ItemAm/Medicine/` | 药品动画 Oringin + Override |

### Config 模块（HotUpdate 热更 平衡性调整用）

**策略：模块整包**

| AB 名称 | 路径 | 说明 |
|---------|------|------|
| `config` | `Assets/Arts/InteranlArts/Configs/` | 全部 10 个 ScriptableObject |

### World 模块（HotUpdate 热更 按需加载）

**策略：模块整包**

| AB 名称 | 路径 | 说明 |
|---------|------|------|
| `world` | `Assets/Arts/InteranlArts/Prefabs/SearchContainer/` | 搜刮容器 AmoBox_01 |

---

## 策略选择总结

| 场景 | 推荐策略 | 原因 |
|------|----------|------|
| 单一文件夹全进一个 AB | 模块整包 | 最简单 一个 `BundleFileInfo` 搞定 |
| 多个文件夹合并到一个 AB | 模块整包 | 多个条目用相同 abName 自动合并 |
| 文件夹下子目录天然对应逻辑分组 | 子文件分包 | 无需手动维护 加新子目录自动成新 AB |
| 超大独立 prefab 需单独加载 | 预制体分包 | 每个 prefab 独立 AB 自动收依赖 |
| 按场景关卡划分 | 场景分包 | 每个场景独立 AB |

---

## 规则校验记录

### 规则1 同生命周期同包

| 包 | 结论 | 说明 |
|----|------|------|
| Player/player | 通过 | PlayerModel + Arms + Origin 基础动画 开局到结束一直存在 |
| UI/icon | 通过 | 图标总量小 粒度换内存划算 |
| UI/ui_hud | 通过 | PlayerPanel 和 ItemWheel 首屏同时出现 HUD 常驻 |
| UI/ui_bag | 通过 | BagPanel 和 GridInventory 打开背包时同时使用 |
| UI/ui_popup | 通过 | 弹窗类面板使用时机相近 |
| Weapon/weapon_* | 通过 | 按操作类型分组 同姿态武器同时加载 |
| Consumable/consum_food | 通过 | 食物模型+食物动画 吃东西时同时加载 |
| Consumable/consum_med | 通过 | 药品模型+药品动画 药品专属 吃罐头不触发药品 |
| Config/config | 通过 | 全部 SO 启动即加载 |
| World/world | 通过 | 搜刮容器独立 后续物件增多可拆 |

### 规则2 共享依赖独立

当前分组中无跨包共享依赖冲突。打包完成后建议用 Unity AssetBundle 依赖分析工具做交叉验证。

### 规则3 粒度适中

全部通过。无单包 >10MB 也无碎到一个 prefab 一个包。

### 规则4 启动最小化

| 包 | 结论 | 说明 |
|----|------|------|
| UI/ui_hud | 通过 | 首屏必须品 |
| UI/ui_bag | 通过 | 预热阶段加载 打开之前完成即可 |
| UI/ui_popup | 通过 | 按需加载 |
| Weapon/weapon_* | 通过 | 非首屏所需 |
| Consumable/consum_* | 通过 | 非首屏所需 |
| World/world | 通过 | 非首屏所需 |

### 规则5 高频变动分离

| 模块 | 结论 | 说明 |
|------|------|------|
| Config | 通过 | 独立 HotUpdate 模块 平衡性调整只需重下 Config 包 |
| Weapon/Consumable/World | 通过 | 内容更新不影响核心包 只重下对应模块 |

### 规则6 依赖链扁平

武器包对 Player 包有单向一层依赖 Origin AnimController 作为 OverrideController 基础 单层依赖可接受 无链式嵌套。

---

## Editor 模式注意事项

编辑器中 `BundleSettings.loadAssetType == Editor` 时 走 `AssetDatabase.LoadAssetAtPath` 直接读工程文件 完全绕过 AB。此时修改任何资源无需重新打包 改后即刻生效。

---

## 实际打包记录

### 打包入口

编辑器内调用 `BuildBundleComplier.BuildAsseetBundle(module, E_EditorBuildKind.AssetBundle, "1.0.0", "MCP Build")` 或 Unity 命令行 `MmAssetCIBuild.BuildFromCommandLine`。多模块需逐个调用。

### 打包前置条件

1. `BundleSettings` 中 `buildTarget` 与 `buildAssetBundleOptions` 正确
2. `AssetBundleConfig.asset` 配置好模块与分包
3. **项目无编译错误**（否则 `BuildPipeline.BuildAssetBundles` 返回 null 报"构建资源包失败"）
4. 重新生成模块枚举：`BundleEnumCreator.GenerateBundleModuleEnum()`
5. **启动脚本改造**：`MmAssetBootManager` 需遍历枚举对 BuiltIn 模块逐个 Boot（勿硬编码模块名）

### 打包产出

输出目录 `BuildOutput/Bundles/{模块名小写}/{平台}/` 每个模块独立文件夹

| 模块 | AB 包 | 大小 | 交付 |
|------|-------|------|------|
| player | player_player | 10MB | BuiltIn |
| ui | ui_common + ui_icon + ui_ui_bag/hud/popup | 9MB | BuiltIn |
| weapon | weapon_common + weapon_weapon_small/onehand/twohand | 3.4MB | HotUpdate |
| consumable | consumable_consum_food + consumable_consum_med | 61MB | HotUpdate |
| config | config_config | 小 | HotUpdate |
| world | world_world | 小 | HotUpdate |

### 遇到的问题与解决

**1. 资源别名重复（框架约束）**

`GenerateAssetAlias` 用文件名去后缀作为资源地址。同名文件（跨目录）在同一打包范围必撞。

- `Fp/单武器_AmCc.overrideController` ↔ `Tp/单武器_AmCc.overrideController`
- `Crosshair.png` ↔ `Crosshair.prefab`

解决：改名区分（`单武器_Tp_AmCc`、`Crosshair_Prefab`、`Crosshair_Sprite`）。Unity `AssetDatabase.RenameAsset` 自动更新所有引用。

**2. BundleModuleEnum 被清空**

打包流程会触发枚举重新生成，若配置里模块与现有枚举不一致，`BundleModuleEnum.cs` 可能被覆盖成只剩 `None`，导致引用旧模块名的代码编译失败。

解决：配置新模块后立即调用 `GenerateBundleModuleEnum()` 并同步改所有引用旧枚举的代码（`MmAssetBootManager` 样例脚本）。

**3. 编译错误阻断打包**

`BuildAssetBundles` 在项目有编译错误时静默失败返回 null。排查方式：清 console 后重跑，抓 `error CS` 日志。

本次遇到并修复：
- `InventoryState.Debug.cs` 的 `#endif` 位置错误（应在 namespace 闭合括号之后）
- `CenterCircleDraw.OnValidate()` 错误 override（`MaskableGraphic` 无此虚方法，改 private）

**4. 多模块批量打包超时**

一个 `execute_code` 循环打包全部 6 模块可能中途超时中断。建议逐个模块单独调用打包。

### 已知遗留

- `Toon` shader 的 `_Surface` 重定义报错是第三方 shader 与 URP 兼容问题 不阻塞打包 后续单独处理
- `consumable_consum_med` 48MB 偏大 后续按药品拆分或压缩贴图
- HotUpdate 模块运行时需在游戏层显式 `BootModule` 加载（如切换场景前）

---

最后核验：2026-08-12
