# DownBreak

Unity 6 工程项目（编辑器版本 `6000.5.1f1`）。

## 打开项目

1. 安装 **Unity Hub**：https://unity.com/download
2. 在 Unity Hub → **Installs** 安装 **Unity 6000.5.1f1**（版本必须严格匹配）
3. Unity Hub → **Projects** → **Open** → 选择本目录 `DownBreak/`
4. 首次打开会重建 `Library/`（几分钟到十几分钟），按提示 **Auto-resolve** 即可

## 目录结构（已通过 .gitignore 过滤）

- `Assets/` — 游戏资源、脚本、场景（25k+ 文件，**全部入库**）
- `ProjectSettings/` — 工程配置
- `Packages/manifest.json` + `packages-lock.json` — 包依赖清单
- `.gitignore` — 已排除 `Library/`、`Temp/`、`Logs/`、`.codex/`、`.opencode/`、`.vscode/`、`node_modules/`、`*.csproj`、构建产物等（这些由 Unity 或 AI 工具自动生成，不需要入库）

## 提交到 GitHub

仓库**新建一个独立的**（不要和博客 `amlm155.github.io` 混），建议名 `DownBreak`。

```bash
git remote add origin https://github.com/amlm155/DownBreak.git
git push -u origin main
```

## 注意事项

- `Library/` 不要入库（会被 gitignore 排除），打开项目时 Unity 会自动重建
- 不要在 GitHub 上勾选 `include Library` 之类的选项
- 单个文件 >100MB 会被 GitHub 拒绝推送——本项目已通过 gitignore 排除 `Library/` 内的几个大索引文件（ArtifactDB / SearchIndex / libburst），无单文件超标