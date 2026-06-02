# Taiwu.Mods

太吾绘卷 mod monorepo 模板仓库。

从 GitHub 模板创建自己的仓库后，在 `mods/` 下维护一个或多个 mod，在 `shared/`
下维护可被多个 mod 引用的内部共享项目。仓库维护入口是 `tools/Taiwu.Mods.Cli/`：
新增 mod、内部共享项目、取消解决方案注册和打包可部署目录都通过它执行。`repo.proj`
承担安装本地工具、检查和格式化等仓库维护 target。

## 开始

首次进入仓库：

```powershell
mise trust
dotnet msbuild repo.proj -t:InstallTools
```

创建一个 mod：

```powershell
dotnet run --project tools/Taiwu.Mods.Cli -- create-mod --name MyMod
```

`ModName` 必须是 C# 命名空间风格的标识符，例如 `MyMod` 或
`MyCompany.MyMod`。创建后，生成器会复制 `templates/mod/`，替换模板变量，并把
前后端项目加入 `Taiwu.Mods.slnx`。

创建一个内部共享项目：

```powershell
dotnet run --project tools/Taiwu.Mods.Cli -- create-shared --name MyCompany.Taiwu.Shared
```

共享项目默认使用 `Shared` 端侧，适合纯共享抽象和通用实现。如果项目面向前端或后端，可以显式
指定端侧来选择默认目标框架：

```powershell
dotnet run --project tools/Taiwu.Mods.Cli -- create-shared --name MyCompany.Taiwu.FrontendSupport --side Frontend
dotnet run --project tools/Taiwu.Mods.Cli -- create-shared --name MyCompany.Taiwu.BackendSupport --side Backend
```

## 常用命令

```powershell
dotnet build Taiwu.Mods.slnx
dotnet msbuild repo.proj -t:Check
dotnet msbuild repo.proj -t:Format
```

打包可部署目录：

```powershell
dotnet run --project tools/Taiwu.Mods.Cli -- pack-mod --name MyMod
```

`pack-mod` 默认使用 `Release` 构建前后端项目，并把 `Config.Lua` 和插件 DLL
组装到 `artifacts/mods/MyMod/`。这个目录可直接替换游戏内对应 mod 目录，也可作为后续分发归档的输入。

从解决方案取消注册某个 mod，但保留文件：

```powershell
dotnet run --project tools/Taiwu.Mods.Cli -- remove-mod --name MyMod
```

从解决方案取消注册某个内部共享项目，但保留文件：

```powershell
dotnet run --project tools/Taiwu.Mods.Cli -- remove-shared --name MyCompany.Taiwu.Shared
```

## 源码目录

`mods/` 和 `shared/` 是源码目录；目录级约定分别在各自 README 中维护：

- `mods/README.md`：mod 源码目录、前后端插件项目和打包入口。
- `shared/README.md`：内部共享项目目录、共享边界和项目级配置入口。

`templates/mod/` 和 `templates/shared/` 是生成输入；其中 README 会复制到新建 mod 或共享项目中，
成为项目内 README。

## 构建约定

`mods/Directory.Build.props` 承载插件项目约定：端侧验证、默认目标框架、基础 Taiwu 引用、
Publicizer 包和依赖内部化。插件项目的本地例外写在项目旁的 `Taiwu.Mod.props`；具体规则见
`mods/README.md`。

`shared/Directory.Build.props` 承载共享项目目录的 C# 规则入口。共享项目的目标框架、Taiwu 引用和
Publicizer 配置写在项目自己的 `.csproj` 中；具体边界见 `shared/README.md`。

NuGet 第三方包版本在 `Directory.Packages.props` 中集中管理。

## 仓库边界

- `tools/Taiwu.Mods.Cli/`：仓库项目生命周期命令入口，负责创建、取消解决方案注册和打包。
- `repo.proj`：安装本地工具、检查和格式化等仓库维护 target。
- `mods/`：mod 源码目录。目录级约定见 `mods/README.md`。
- `shared/`：内部共享项目目录。目录级约定见 `shared/README.md`。
- `templates/mod/`：`create-mod` 的生成输入，维护源码骨架；具体 mod 开发在 `mods/<ModName>/`。
- `templates/shared/`：`create-shared` 的生成输入，维护内部共享项目骨架。
- `Directory.Build.props`：仓库级 C# 编译、分析器和代码质量规则。
- `mods/Directory.Build.props`：mod 项目共享约定，包括插件端侧、基础引用和 ILRepack 设置。
- `shared/Directory.Build.props`：内部共享项目目录的 C# 规则入口。
- `Directory.Packages.props`：集中管理 NuGet 包版本。
- `NuGet.config`：固定 NuGet 源和包源映射。
- `Taiwu.Mods.slnx`：解决方案入口，收录仓库工具、已注册的 mod 项目和内部共享项目。
