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
dotnet run --project tools/Taiwu.Mods.Cli -- create --name MyMod
```

`ModName` 必须是 C# 命名空间风格的标识符，例如 `MyMod` 或
`MyCompany.MyMod`。创建后，生成器会复制 `templates/mod/`，替换模板变量，并把
前后端项目加入 `Taiwu.Mods.slnx`。

创建一个内部共享项目：

```powershell
dotnet run --project tools/Taiwu.Mods.Cli -- create-shared --name MyCompany.Taiwu.Shared
```

共享项目默认使用 `Shared` 端侧，不自动引用太吾游戏包。这样纯共享抽象和通用实现可以在没有
游戏引用包凭据的环境中正常构建。如果项目只服务前端或后端，可以显式指定端侧来选择默认目标
框架：

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
dotnet run --project tools/Taiwu.Mods.Cli -- pack --name MyMod
```

`pack` 默认使用 `Release` 构建前后端项目，并把 `Config.Lua` 和插件 DLL
组装到 `artifacts/mods/MyMod/`。这个目录可直接替换游戏内对应 mod 目录，也可作为后续分发归档的输入。

从解决方案取消注册某个 mod，但保留文件：

```powershell
dotnet run --project tools/Taiwu.Mods.Cli -- remove --name MyMod
```

从解决方案取消注册某个内部共享项目，但保留文件：

```powershell
dotnet run --project tools/Taiwu.Mods.Cli -- remove-shared --name MyCompany.Taiwu.Shared
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

普通 `dotnet build` 使用 SDK 默认的 `bin/` 和 `obj/` 输出目录；完整 mod 目录由
`tools/Taiwu.Mods.Cli` 的 `pack` 命令生成，用于部署或测试。

前后端项目默认引用 `Taiwu.ModKit.References.Plugin`。需要访问更宽的游戏 API
时，再按实际代码需要添加 `Taiwu.ModKit.References.Frontend` 或
`Taiwu.ModKit.References.Backend` 等引用包。

## 内部共享项目结构

新建内部共享项目后的核心结构如下：

```text
shared/MyCompany.Taiwu.Shared/
  README.md
  MyCompany.Taiwu.Shared.csproj
```

`shared/` 下每个一级子目录是一个内部项目。它们不是游戏可部署 mod，不参与 `pack` 输出，也
不默认执行 ILRepack 内部化。共享项目应承载可复用抽象、通用实现或对游戏 API 的窄封装；
最终由 `mods/` 下的插件项目引用并随插件构建产出。

共享项目按标准 C# class library 组织：目标框架和引用包直接写在项目自己的 `.csproj`
中。默认 `Shared` 和 `Frontend` 项目目标框架为 `netstandard2.1`，`Backend` 项目目标框架为
`net6.0`。`create-shared` 不自动添加太吾引用包；需要访问更宽的游戏 API 时，再按实际代码需要
添加 `Taiwu.ModKit.References.Frontend`、`Taiwu.ModKit.References.Backend` 等引用包。

## Taiwu 引用、依赖内部化和 Publicizer

`mods/Directory.Build.props` 承载插件项目特有的端侧、基础引用、Publicizer 和 ILRepack
内部化约定。`shared/Directory.Build.props` 只继承仓库级 C# 规则；共享项目需要太吾引用包或
Publicizer 时，在项目自己的 `.csproj` 中显式声明。

mod 项目默认使用 `ILRepack.Lib.MSBuild.Task` 把依赖内部化进插件 DLL，降低不同 mod
携带同名依赖时的冲突风险。这个流程先收集 MSBuild 解析出的 runtime/copy-local DLL，再由
ILRepack 合并进插件主 DLL，并对这些输入程序集做内部化和重命名。被处理的依赖是在这个流程
之后成为插件 DLL 内部的私有实现；输入范围来自构建输出语义，而不是依赖名称或包来源。

进入 NuGet `ref/` 目录的编译期引用，以及标记为 `CopyLocal=false` 的引用，保持为编译输入，
不进入合并流程。因此太吾游戏引用包会保留为外部游戏依赖，而不是打进插件 DLL。

```xml
<PropertyGroup>
  <InternalizeRuntimeDependencies>false</InternalizeRuntimeDependencies>
</PropertyGroup>
```

上面的配置可以关闭默认内部化。需要让某个 runtime/copy-local DLL 保持为独立文件时，在
`Taiwu.Mod.props` 中排除对应程序集文件名，不带 `.dll`：

```xml
<ItemGroup>
  <KeepDependencySeparate Include="Your.Dependency.AssemblyName" />
</ItemGroup>
```

NuGet 第三方包版本仍在 `Directory.Packages.props` 中集中管理。

mod 项目默认启用 build-time Publicizer 包，但模板不默认公开任何程序集或成员；没有
`Publicize` 项时不会 publicize 游戏 DLL，也不会引入运行时依赖。需要在编译期访问游戏 DLL
的非 public API 时，在 `Taiwu.Mod.props` 中声明要公开化的依赖和成员即可。共享项目则在自己的
`.csproj` 中显式添加 `Krafs.Publicizer` 引用、启用 `UsePublicizer`，并声明具体 `Publicize`
项。

```xml
<ItemGroup>
  <Publicize
    Include="Assembly-CSharp"
    IncludeCompilerGeneratedMembers="false"
    IncludeVirtualMembers="false"
  />
</ItemGroup>
```

需要关闭 mod 项目的默认 Publicizer 包时，可以在 `Taiwu.Mod.props` 中设置：

```xml
<PropertyGroup>
  <UsePublicizer>false</UsePublicizer>
</PropertyGroup>
```

前端通常从 `Assembly-CSharp` 开始，后端通常从 `GameData` 开始；如果只需要具体类型或
成员，优先写更窄的 `Publicize Include`。批量公开化时建议保留
`IncludeCompilerGeneratedMembers="false"` 和 `IncludeVirtualMembers="false"`，减少编译器
生成成员冲突和虚成员访问级别不匹配。

## 仓库边界

- `tools/Taiwu.Mods.Cli/`：mod 生命周期命令入口，负责创建、取消解决方案注册和打包。
- `repo.proj`：安装本地工具、检查和格式化等仓库维护 target。
- `mods/`：mod 源码目录。每个一级子目录是一个独立 mod。
- `shared/`：内部共享项目目录。每个一级子目录是一个可被多个 mod 引用的内部库。
- `templates/mod/`：`create` 的生成输入，维护源码骨架；具体 mod 开发在 `mods/<ModName>/`。
- `templates/shared/`：`create-shared` 的生成输入，维护内部共享项目骨架。
- `Directory.Build.props`：仓库级 C# 编译、分析器和代码质量规则。
- `mods/Directory.Build.props`：mod 项目共享约定，包括插件端侧、基础引用和 ILRepack 设置。
- `shared/Directory.Build.props`：内部共享项目目录约定，只继承仓库级 C# 规则。
- `Directory.Packages.props`：集中管理 NuGet 包版本。
- `NuGet.config`：固定 NuGet 源和包源映射。
- `Taiwu.Mods.slnx`：解决方案入口，收录仓库工具、已注册的 mod 项目和内部共享项目。
