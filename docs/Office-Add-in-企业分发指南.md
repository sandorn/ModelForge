# ModelForge Office Add-in 企业分发指南

> 适用范围：Excel / PowerPoint / Word Web Add-in 的企业内测和试点分发。Microsoft 365 管理入口可能调整，实际操作以 Microsoft Learn 当前文档为准。

## 1. 分发模式选择

| 模式 | 适用场景 | 说明 |
| --- | --- | --- |
| Microsoft 365 Admin Center 集中部署 | 企业试点/生产 | 推荐方式，可按用户或组分配 Add-in。参考 Microsoft Learn: https://learn.microsoft.com/en-us/microsoft-365/admin/manage/manage-deployment-of-add-ins |
| Integrated apps 门户 | Microsoft 365 统一应用管理 | Microsoft 当前推荐的集中管理入口之一。参考 Microsoft Learn: https://learn.microsoft.com/en-us/microsoft-365/admin/manage/manage-addins-in-the-admin-center |
| 网络共享受信任目录 | 开发/测试 | 仅用于 Windows 桌面 Office 测试，不作为生产首选。参考 Microsoft Learn: https://learn.microsoft.com/en-us/office/dev/add-ins/testing/create-a-network-shared-folder-catalog-for-task-pane-and-content-add-ins |
| 本机 WEF 目录复制 | 单机调试 | 适用于开发者快速 sideload。 |

## 2. 发布前准备

- [ ] 构建 Web Add-in：`cd src\web && npm run build`。
- [ ] 确认 manifest：`manifest\modelForge.web.xml` 中 Excel/PowerPoint/Word 均声明 `FunctionFile`。
- [ ] 确认 `SourceLocation` 和 `FunctionFile` 指向 HTTPS 企业可信域名，或试点安装目录。
- [ ] 确认 Backend / Sidecar 地址和端口策略：默认 `http://localhost:5095`、`http://localhost:5200`。
- [ ] 准备用户分组：Pilot、Business Champions、General Availability。

## 3. Microsoft 365 集中部署流程

1. 使用具备应用管理权限的管理员账号登录 Microsoft 365 Admin Center。
2. 进入 **Settings / Integrated apps** 或 Add-ins 管理入口。
3. 选择部署自定义应用或上传 manifest。
4. 上传 `modelForge.web.xml`。
5. 分配给试点用户或安全组。
6. 等待策略同步后，让用户重启 Office 桌面应用。
7. 在 Excel、PowerPoint、Word 中确认 ModelForge Ribbon 和任务窗格可见。

## 4. 网络共享测试流程

本地开发/手工回归推荐使用脚本自动创建本机共享目录并注册 Office Trusted Catalog：

```powershell
# 需要管理员 PowerShell 创建本机 SMB 共享。
cd D:\CODES\model-forge
.\scripts\setup-office-addin-sideload.ps1
```

脚本会将 `manifest\modelForge.web.xml` 复制到 `artifacts\office-addin-catalog\`，创建 `\\<计算机名>\ModelForgeOfficeAddins` 只读共享，并写入 `HKCU\Software\Microsoft\Office\16.0\WEF\TrustedCatalogs`。完成后关闭并重新打开 Office，在“加载项 / 更多加载项 / 共享文件夹”中添加 `ModelForge`。直接复制到 `%LOCALAPPDATA%\Microsoft\Office\WEF` 属于缓存目录写入，不作为可靠侧载验收方式。

真实点击回归前必须排除 WPS/Kingsoft Office 抢占 COM 的干扰：

```powershell
.\scripts\check-office-runtime.ps1 -StopWps
```

预期只保留 Microsoft Office 2016+/Office 2024 的 `EXCEL.EXE` / `POWERPNT.EXE` / `WINWORD.EXE`，且 Sidecar `/api/status` 返回 `version=16.0`。若仍有 `wps.exe`、`et.exe` 或 `wpp.exe`，不要继续记录 Ribbon 行为结论。

1. 在文件服务器创建只读共享目录，例如 `\\fileserver\OfficeAddins\ModelForge`。
2. 将 `modelForge.web.xml` 放入共享目录。
3. 在测试机 Office 信任中心添加“受信任 Web 加载项目录”，或按 Microsoft Learn 注册 TrustedCatalog。
4. 重启 Excel/PowerPoint/Word。
5. 通过“插入 → 我的加载项 → 共享文件夹”添加 ModelForge。

## 5. 验证清单

- [ ] Excel 可显示 ModelForge Ribbon。
- [ ] PowerPoint / Word 可显示 ModelForge Ribbon 或任务窗格入口。
- [ ] `scripts/check-office-runtime.ps1 -StopWps` 通过，且无 WPS/Kingsoft Office 进程残留。
- [ ] Ribbon `ExecuteFunction` 按钮能调用 Sidecar `/api/execute`。
- [ ] `function-file.html` 能正确映射 `excel.*`、`ppt.*`、`word.*` 命令宿主。
- [ ] 任务窗格能连接 Backend `/health` 和 Sidecar `/health`。
- [ ] 管理员 Console 可登录并访问 Corporate Dictionary。

## 6. 回滚策略

- Microsoft 365 集中部署：在 Integrated apps / Add-ins 管理页取消分配用户或移除应用。
- 网络共享：从受信任目录删除 manifest，并通知用户重启 Office。
- MSI：管理员 PowerShell 执行 `$msi = (Resolve-Path ".\ModelForge.msi").Path` 后运行 `msiexec /x $msi /l*v "$PWD\uninstall.log"`，或直接运行 `.\scripts\test-msi-admin.ps1` 覆盖安装/卸载回归。
- 回滚后验证 Office Ribbon 消失、本机服务停止、`/health` 不再可达或按预期返回。

## 7. 当前限制

- 真实 Ribbon 点击回归仍需在 Excel/PowerPoint/Word 侧载后人工逐项验证。
- 当前 manifest 仍以本地/试点部署为主；生产需替换 HTTPS 域名、图标 URL 和企业证书。
- SSO/RBAC 生产接入未完成，默认账号仅适合本地开发和封闭试点。
- Sidecar 本地 API 尚未启用本地令牌鉴权；生产前需配合终端防火墙和本机访问边界控制。
