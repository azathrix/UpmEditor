<h1 align="center">UPM Editor</h1>

<p align="center">
  Unity Package Manager 编辑器工具，用于创建、编辑和发布 UPM 包
</p>

<p align="center">
  <a href="https://github.com/Azathrix/UpmEditor"><img src="https://img.shields.io/badge/GitHub-UpmEditor-black.svg" alt="GitHub"></a>
  <a href="https://www.npmjs.com/package/com.azathrix.upm-editor"><img src="https://img.shields.io/npm/v/com.azathrix.upm-editor.svg" alt="npm"></a>
  <a href="https://github.com/Azathrix/UpmEditor/blob/main/LICENSE"><img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="License"></a>
  <a href="https://unity.com/"><img src="https://img.shields.io/badge/Unity-6000.3+-black.svg" alt="Unity"></a>
</p>

---

## 特性

- 创建 UPM 包，支持自定义模板
- Inspector 编辑 package.json
- 发布到 npm 官方、Verdaccio 或其他 npm 仓库
- npm 登录状态检测
- Unity 签名打包（Unity 6.3+）
- 右键菜单快速操作
- 自动同步 asmdef 文件

## 安装

### 方式一：Package Manager 添加 Scope（推荐）

1. 打开 `Edit > Project Settings > Package Manager`
2. 在 `Scoped Registries` 中添加：
   - Name: `Azathrix`
   - URL: `https://registry.npmjs.org`
   - Scope(s): `com.azathrix`
3. 点击 `Save`
4. 打开 `Window > Package Manager`
5. 切换到 `My Registries`
6. 找到 `UPM Editor` 并安装

### 方式二：Git URL

1. 打开 `Window > Package Manager`
2. 点击 `+` > `Add package from git URL...`
3. 输入：`https://github.com/Azathrix/UpmEditor.git#latest`

### 方式三：npm 命令

在项目的 `Packages` 目录下执行：

```bash
npm install com.azathrix.upm-editor
```

## 使用方法

### 创建新包

菜单: `Azathrix > UPM Editor > 创建 UPM`

创建包时可选择生成：
- Runtime/ 目录（含 .asmdef）
- Editor/ 目录（含 .asmdef）
- Tests/ 目录（含测试 .asmdef）
- Documentation~/ 目录
- README.md / CHANGELOG.md / LICENSE.md

### 编辑现有包

在 Project 窗口中选中 UPM 包目录，Inspector 会显示编辑界面：
- 修改包名、版本、描述等基本信息
- 管理依赖项和关键词
- 添加/删除目录和文件
- 点击"保存"按钮保存更改

修改包名后保存会自动更新所有 asmdef 文件的名称和命名空间。

### 发布包

1. 在 Inspector 中点击"发布"按钮，或菜单 `Azathrix > UPM Editor > 发布 UPM`
2. 选择 Registry 类型（npm 官方 / Verdaccio / 自定义）
3. 确认 npm 登录状态
4. 选择打包方式（普通打包 / Unity 签名打包）
5. 点击"发布"

#### 发布到 npm 官方

1. 先在终端登录：`npm login`
2. 如果启用了 2FA，需要创建 Access Token：
   - 登录 https://www.npmjs.com → Access Tokens
   - 创建 Granular Access Token，勾选 "bypass 2FA"
   - 运行：`npm config set //registry.npmjs.org/:_authToken=你的token`
3. 在发布窗口选择 "NpmOfficial"，点击发布

### 右键菜单

在 Project 窗口中右键文件夹：
- **Move to Packages**: 将 Assets 下的包移动到 Packages
- **Move to Assets**: 将 Packages 下的包移动到 Assets
- **Create Package Here**: 在当前目录创建新包

## 命名规范

包名和程序集名的转换规则：
- `com.company.name1.name2` → `Company.Name1.Name2`
- `com.company.name1-name2` → `Company.Name1Name2`

## License

MIT
