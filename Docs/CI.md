# CI And Platform Validation

## Automatic static checks

`.github/workflows/static-validation.yml` runs on every push and pull request without a Unity license. It validates package identities, dependency direction, excluded business concepts, JSON/asmdef syntax and Skill structure.

本地发布前运行 `python3 Scripts/validate_clean_install.py`，它会创建临时空白项目，以外部 `file:` 依赖安装全部包，执行 Setup Wizard 和消费端编译测试后自动删除临时目录。发布 Tag 后传入 `--repository` 与 `--ref`，可用相同流程验证远端 Git URL 安装。

## Unity tests

`.github/workflows/unity-tests.yml` is manually triggered. Configure these private repository secrets first:

- `UNITY_LICENSE`
- `UNITY_EMAIL`
- `UNITY_PASSWORD`

The workflow runs EditMode and PlayMode tests using Unity `2022.3.62f2c1` and uploads the results.

## Android and iOS compile smoke tests

`.github/workflows/platform-builds.yml` is manually triggered with Android or iOS. It uses the same Unity secrets and uploads build output. These jobs prove package and player compilation; they do not prove store login, ATT, push notification, vendor dashboard delivery or device-only SDK behavior.

Locally, install the corresponding Unity platform module and run:

```bash
Scripts/run-platform-build.sh Android
Scripts/run-platform-build.sh iOS
```

The initial development machine only has macOS Standalone support, so Android/iOS must be verified by CI or a machine with those modules installed.
