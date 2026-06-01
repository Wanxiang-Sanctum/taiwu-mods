# Taiwu.Mods

太吾绘卷 mod monorepo 模板仓库。

从 GitHub 模板创建自己的仓库后，在 `mods/` 下维护一个或多个 mod。仓库维护入口是
`repo.proj`：新增 mod、取消解决方案注册、检查格式和打包可部署目录都通过它执行。

## 开始

首次进入仓库：

```powershell
mise trust
dotnet msbuild repo.proj -t:InstallTools
```

创建一个 mod：

```powershell
dotnet msbuild repo.proj -t:NewMod -p:ModName=MyMod
```

`ModName` 必须是 C# 命名空间风格的标识符，例如 `MyMod` 或
`MyCompany.MyMod`。创建后，生成器会复制 `templates/mod/`，替换模板变量，并把
前后端项目加入 `Taiwu.Mods.slnx`。

## 常用命令

```powershell
dotnet build Taiwu.Mods.slnx
dotnet msbuild repo.proj -t:Check
dotnet msbuild repo.proj -t:Format
```

打包可部署目录：

```powershell
dotnet msbuild repo.proj -t:PackMod -p:ModName=MyMod
```

`PackMod` 默认使用 `Release` 构建前后端项目，并把 `Config.Lua` 和插件 DLL
组装到 `artifacts/mods/MyMod/`。这个目录可直接替换游戏内对应 mod 目录。仓库
只产出目录，不负责压缩归档。

从解决方案取消注册某个 mod，但保留文件：

```powershell
dotnet msbuild repo.proj -t:RemoveMod -p:ModName=MyMod
```

## Mod 结构

新建 mod 后的核心结构如下：

```text
mods/MyMod/
  Config.Lua
  README.md
  src/
    Frontend/
      MyMod.Frontend.csproj
      Taiwu.Mod.props
      FrontendPlugin.cs
    Backend/
      MyMod.Backend.csproj
      Taiwu.Mod.props
      BackendPlugin.cs
```

前端项目目标框架为 `netstandard2.1`，后端项目目标框架为 `net6.0`。这两个
目标框架由各项目旁边的 `Taiwu.Mod.props` 标记端侧，再由
`mods/Directory.Build.props` 统一设置。

普通 `dotnet build` 使用 SDK 默认的 `bin/` 和 `obj/` 输出目录，不直接生成完整 mod
目录。需要部署或测试完整目录时，使用 `PackMod`。

前后端项目默认引用 `Taiwu.ModKit.References.Plugin`。需要访问更宽的游戏 API
时，再按实际代码需要添加 `Taiwu.ModKit.References.Frontend` 或
`Taiwu.ModKit.References.Backend` 等引用包。

## 仓库边界

- `repo.proj`：维护命令入口。面向日常开发者，隐藏工具项目的命令行细节。
- `mods/`：mod 源码目录。每个一级子目录是一个独立 mod。
- `templates/mod/`：`NewMod` 的生成输入。这里维护源码骨架，不作为具体 mod 开发目录。
- `tools/Taiwu.Mods.ModCreator/`：仓库内生成、取消注册和打包工具。普通使用优先走 `repo.proj`。
- `Directory.Build.props`：仓库级 C# 编译、分析器和代码质量规则。
- `mods/Directory.Build.props`：mod 项目共享约定，包括端侧标记、目标框架和基础引用。
- `Directory.Packages.props`：集中管理 NuGet 包版本。
- `NuGet.config`：固定 NuGet 源和包源映射。
- `Taiwu.Mods.slnx`：解决方案入口，收录仓库工具和已注册的 mod 项目。
