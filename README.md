# Unity Game Foundation

`Unity Game Foundation` 是面向 Unity 2022.3 与 QFramework 的基础设施仓库。它提供通用 UI、资源热更编排、SDK 生命周期、存档和新项目初始化能力，不包含玩法、排行榜、经济、商店或 IAP。公开版本从 `v0.6.0` 开始，使用独立干净历史，不包含旧版设计下载与同步工具。仓库可公开访问，但使用授权仍以 [LICENSE.md](LICENSE.md) 为准。

## Packages

| Package | Responsibility |
| --- | --- |
| `com.nn555k.gamefoundation.core` | QFramework 组合、日志与时钟 |
| `com.nn555k.gamefoundation.save` | 版本化存档、迁移、校验、主备恢复 |
| `com.nn555k.gamefoundation.hotupdate` | 内容检查、下载、校验、提交、回滚、离线降级 |
| `com.nn555k.gamefoundation.hotupdate.yooasset` | YooAsset 2.3 安装、预下载、校验、清单提交与回退适配 |
| `com.nn555k.gamefoundation.sdk` | 授权门、顺序初始化、硬超时、故障隔离与生命周期 |
| `com.nn555k.gamefoundation.ui` | 页面/弹窗导航、Safe Area、Prefab 校验与 Figma 规范化 |
| `com.nn555k.gamefoundation.setup` | Editor-only 新项目、AI 治理、Feature Spec 与 CI 脚手架 |

## Install

AI 接入默认走完整流程。克隆本仓库并关闭目标工程的 Unity Editor，然后从本仓库运行：

```bash
python3 Scripts/onboard_project.py --project /path/to/MyGame --project-name MyGame --namespace Studio.MyGame
```

该入口会安装 QFramework 与基础推荐包、生成唯一项目架构和 AI 规范、初始化 YooAsset 采集配置。框架不包含设计下载与同步工具，开发者按需自行接入。新代码重新编译后，结果写入 `.gamefoundation/onboarding-report.json`。重复接入只需 `--project`，不会生成第二套 App。外部 Token、CDN、SDK 参数仍需项目提供，详见 `Docs/NewProjectSetup.md`。

仅安装包引用时可使用 `Scripts/install_upm_packages.py`；该步骤本身不代表接入完成。也可以手动将需要的 Git URL 加入目标项目 `Packages/manifest.json`：

```json
{
  "com.nn555k.gamefoundation.core": "https://github.com/nn555k/unity-game-foundation.git?path=/Packages/com.nn555k.gamefoundation.core#v0.6.0"
}
```

能力包依赖 Core；使用 Git URL 安装时，应把 Core 和所选能力包都列为目标项目的直接依赖。默认安装脚本会同时加入 YooAsset `2.3.18` 与 Foundation YooAsset Backend，不加入 Figma 工具；已有 Figma 安装引用保持原样。

安装 Setup 后打开 `Game Foundation > Project Setup`，输入项目名、根命名空间和代码根目录。工具会同时安装 Feature Spec、AI Skills、项目规范校验和 GitHub CI，默认保留项目自有文件与 `AGENTS.md` 非托管内容。已有基础包的项目也可以执行 `Game Foundation > Install Optional Tools > Recommended All` 补装 YooAsset。设计工具安装入口已移除，UI 规范化与校验菜单继续可用。

Save、HotUpdate 和 SDK 包都带有可从 Package Manager 导入的适配示例。真实厂商接入方式和验收边界见 `Docs/IntegrationAdapters.md`。

## AI workflow

Setup 会把消费项目所需的 `AGENTS.md` 托管区块、`.agents/skills`、Foundation 文档与 Feature Spec 模板写入新项目。每个行为改动先建立 `Docs/Features/<feature-id>.md`，实现后由同一份规范驱动目录、QFramework、Prefab、编译、测试和 CI 验收。

`v0.5.0` 起，默认 UI / Prefab 使用 Resources 按路径加载，直接引用的 UI / Prefab / 美术资源放在 `Assets/Sprites`。Setup 自动建立分类目录，但不搬迁旧资源、不替换旧加载代码。详细规则见 [资源加载规范](Docs/ResourceLoading.md)。

## Validate

```bash
Scripts/validate-foundation.sh
Scripts/run-unity-tests.sh
python3 Scripts/validate_clean_install.py
```

仓库推送并打 Tag 后，还可以直接验证 Git URL 安装：

```bash
python3 Scripts/validate_clean_install.py \
  --repository https://github.com/nn555k/unity-game-foundation.git \
  --ref v0.6.0
```

仓库自身是一个最小 Unity 验证工程。CI 配置见 `Docs/CI.md`。

## Scope notes

- HotUpdate 主包保持供应商无关；可选 YooAsset 包提供真实的资源预下载和清单提交，但不包含托管代码热修复运行时。
- SDK 包只管理供应商 Adapter 生命周期，不包含供应商密钥和 IAP。
- Figma、蓝湖等设计下载与同步工具不随框架分发；保留导入后 UI 规范化、稳定 ID 保护规则和校验能力。
- 从旧私有版本升级请阅读 [公开版本迁移说明](Docs/PublicRelease.md)，尤其注意旧标签和工具包的来源变化。
