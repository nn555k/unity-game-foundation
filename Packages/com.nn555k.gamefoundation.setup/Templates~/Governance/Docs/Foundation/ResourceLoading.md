# Resource Loading Convention

当前项目默认使用 Unity `Resources` 体系按路径加载资源，不默认改用 YooAsset 或 Addressables。此约定适用于后续新建或明确授权整理的 UI、Prefab 和美术资源，不是批量迁移已有资源的指令。

## 先判断是否需要按路径加载

| 使用方式 | 存放位置 | 示例 |
| --- | --- | --- |
| 运行时通过路径加载的 UI / Prefab | `Assets/Resources/Prefabs/UI/` | `SettingsPopup.prefab` |
| 运行时通过路径加载的 Sprite | `Assets/Resources/Sprites/UI/` | `MusicOn.png` |
| 仅通过 Inspector、场景或 Prefab 直接引用的图片 | `Assets/Sprites/UI/` | 弹窗背景、按钮底图 |
| 仅直接引用、不独立按路径加载的 UI / Prefab | `Assets/Sprites/Prefabs/UI/` | 列表条目、装饰组件 |
| Figma 原始导入层 | `Assets/Sprites/DesignImports/` | 未规范化的页面、切图 |

需要按路径加载的其他资源也放在 `Assets/Resources` 的类型子目录。上述 `Assets/Sprites` 规则针对 UI、Prefab 和美术资源；脚本、场景、Editor 设置、存档文件等仍按各自职责存放，不挪到 Sprites。

“不需要加载”指不需要单独调用 `Resources.Load`，不是运行时完全不使用。一个动态加载的弹窗可以引用 `Assets/Sprites/UI` 下的图片，无需将整套依赖一起移入 Resources。无场景、Prefab 或其他可打包资源引用的文件，不会仅因位于 Sprites 目录而成为可访问的运行时资源。

## 加载与生命周期

- 普通加载使用 `Resources.Load<T>`；需要异步预加载时使用同属 Resources 体系的 `Resources.LoadAsync<T>`。
- 加载键相对 `Resources`，使用 `/`，不带 `Assets/Resources/` 前缀和文件扩展名。例如 `Assets/Resources/Prefabs/UI/SettingsPopup.prefab` 对应 `Prefabs/UI/SettingsPopup`。
- 文件名、Prefab 根名和规范类型后缀保持一致；路径常量或资源目录表由项目拥有，不在点击回调中随意拼接路径。
- 缓存已加载引用，空结果必须明确报错或降级；避免在 Update、刷新循环或每次点击中重复加载。异步加载完成时检查请求方是否已销毁。
- 加载 Prefab 与实例化是两步；关闭时按项目生命周期销毁或回收实例。不要在每次关窗时调用全局 `Resources.UnloadUnusedAssets`，也不要卸载仍被其他视图使用的共享资源。
- 禁止同一 Resources 加载键对应多个文件（包括大小写或扩展名不同、分布在多个 Resources 目录的情况），避免寻址歧义。

## AI 制作 / Figma 规范化流程

1. 在 Feature Spec 中为每项新增 UI 资源写明：最终路径、Resources 加载键或直接引用方、加载/缓存/释放责任。
2. 原始 Figma 导入层留在 `Assets/Sprites/DesignImports`；下载与同步插件不随框架分发，由开发者自行接入。
3. 根据实际使用方式选择生产 Prefab 目录，再预览并应用既有层级和命名规范。弹窗仍遵循 `DimBackground` + `PopupContent`。
4. 经 Unity Editor API 创建或移动资源，保留 GUID、Figma 稳定 ID 和引用。不得文本修改 Prefab，也不得未经授权批量移动旧资源。
5. 验证真实 Resources 加载、实例化和直接引用依赖；仅文件存在或编辑器 AssetDatabase 能加载，不等于运行时路径有效。

## 与 YooAsset 及旧项目的边界

- YooAsset 包与 Collector 可以保留，但它们不会接管默认 UI 加载。`contentRoot` / `Assets/GameContent` 只用于项目明确选择的热更内容，不是默认 UI 生产目录。
- 不把 Resources 加入 YooAsset 采集根，不把同一 UI 资源复制到两套目录。已有热更业务保持原状；将来切换加载方案必须单独说明资源迁移、地址与生命周期影响。
- Setup 新建上述目录，不移动资源、不修改存档，也不覆盖既有 Prefab。旧项目要人工确认并合并新的文档/Skill 模板，不能用全量覆盖修复治理文件。
- Unity 和 Python 校验器检查重复 Resources 键及 `Assets/Sprites` 内错误嵌套的 Resources。它们不能推断一项资源是否应该动态加载；目录归属与动态路径仍需 Feature Spec 和运行时验证。

Unity 依据：[Resources.Load](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Resources.Load.html)、[Loading Resources at Runtime](https://docs.unity3d.com/2022.3/Documentation/Manual/LoadingResourcesatRuntime.html)。
