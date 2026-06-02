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
dotnet run --project tools/Taiwu.Mods.Cli -- pack --name {{ModName}}
```

`pack` 会把 `Config.Lua` 和插件 DLL 组装到仓库根目录的
`artifacts/mods/{{ModName}}/`。

项目结构：

- `Config.Lua`：游戏读取的 mod 配置。
- `src/Frontend/`：前端插件项目，目标框架为 `netstandard2.1`。
- `src/Backend/`：后端插件项目，目标框架为 `net6.0`。

前后端项目默认引用 `Taiwu.ModKit.References.Plugin`。需要访问更宽的游戏 API
时，再按实际代码需要添加对应引用包。

## 依赖内部化和 Publicizer

构建默认会把需要随插件输出的 DLL 合并进插件主 DLL，并对这些输入程序集做内部化和重命名。
这些依赖在处理后成为插件 DLL 内部的私有实现；作为编译期引用的太吾游戏 DLL 保持为编译输入，
不进入合并流程。

```xml
<PropertyGroup>
  <InternalizeRuntimeDependencies>false</InternalizeRuntimeDependencies>
</PropertyGroup>
```

上面的配置可以关闭默认内部化。需要让某个会被合并的 DLL 保持为独立文件时，在
`Taiwu.Mod.props` 中排除对应程序集文件名，不带 `.dll`：

```xml
<ItemGroup>
  <KeepDependencySeparate Include="Your.Dependency.AssemblyName" />
</ItemGroup>
```

需要访问游戏 DLL 的非 public API 时，先启用 Publicizer，再自行声明要公开化的程序集、
类型或成员：

```xml
<PropertyGroup>
  <UsePublicizer>true</UsePublicizer>
</PropertyGroup>

<ItemGroup>
  <Publicize
    Include="Assembly-CSharp"
    IncludeCompilerGeneratedMembers="false"
    IncludeVirtualMembers="false"
  />
</ItemGroup>
```

前端常用 `Assembly-CSharp`，后端常用 `GameData`。如果只需要具体类型或成员，优先写更窄的
`Publicize Include`。
