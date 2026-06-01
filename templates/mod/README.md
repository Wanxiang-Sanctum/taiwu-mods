# {{ModName}}

太吾绘卷 Mod。

## 简介

在这里写明这个 mod 解决的问题、主要玩法或功能边界。

## 使用

在这里写明安装、启用、配置和存档兼容性注意事项。

## 兼容性

- 游戏版本：
- 依赖 mod：
- 已知冲突：

## 开发

从仓库根目录构建项目：

```powershell
dotnet build mods/{{ModName}}/src/Frontend/{{ModName}}.Frontend.csproj
dotnet build mods/{{ModName}}/src/Backend/{{ModName}}.Backend.csproj
```

打包可部署目录：

```powershell
dotnet msbuild repo.proj -t:PackMod -p:ModName={{ModName}}
```

`PackMod` 会把 `Config.Lua` 和插件 DLL 组装到仓库根目录的
`artifacts/mods/{{ModName}}/`。

项目结构：

- `Config.Lua`：游戏读取的 mod 配置。
- `src/Frontend/`：前端插件项目，目标框架为 `netstandard2.1`。
- `src/Backend/`：后端插件项目，目标框架为 `net6.0`。

前后端项目默认引用 `Taiwu.ModKit.References.Plugin`。需要访问更宽的游戏 API
时，再按实际代码需要添加对应引用包。
