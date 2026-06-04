# ModelForge 安装器

## 构建 MSI

### 前置条件

- WiX Toolset v5: `dotnet tool install --global wix`
- .NET 10 SDK
- 已执行 `scripts/build-installer.ps1`，`publish/` 目录包含所有发布产物

### 构建步骤

```powershell
# 1. 构建所有组件到 publish/
.\scripts\build-installer.ps1 -Configuration Release

# 2. 构建 MSI 安装包
dotnet build installer\ModelForge.Installer\ModelForge.Installer.wixproj -c Release

# 输出: installer\ModelForge.Installer\bin\Release\net10.0\ModelForge-0.1.0.msi
```

### 安装

```powershell
# 静默安装
msiexec /i ModelForge-0.1.0.msi /quiet

# 交互式安装
msiexec /i ModelForge-0.1.0.msi

# 卸载
msiexec /x ModelForge-0.1.0.msi
```

### 安装内容

| 组件 | 路径 | 说明 |
|------|------|------|
| Sidecar | `C:\Program Files\ModelForge\Sidecar\` | Windows Service (自启动) |
| Backend | `C:\Program Files\ModelForge\Backend\` | Windows Service (自启动) |
| Web Add-in | `C:\Program Files\ModelForge\Web\` | 静态文件 |

### Office Add-in 侧载

安装完成后，需手动将 manifest 注册到 Office 受信任目录：

```powershell
# 复制 manifest
Copy-Item "manifest\modelForge.web.xml" "$env:LOCALAPPDATA\Microsoft\Office\WEF\"

# 或在 Excel 中：文件 → 选项 → 信任中心 → 受信任的加载项目录
# 添加 C:\Program Files\ModelForge\Web\ 到受信任目录
```
