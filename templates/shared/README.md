# {{ProjectName}}

太吾绘卷 mod 仓库内部共享项目。

## 职责

在这里写明这个项目承载的稳定抽象、共享实现或游戏 API 封装边界。

## 开发

从仓库根目录构建项目：

```powershell
dotnet build shared/{{ProjectName}}/{{ProjectName}}.csproj
```

这个项目用于被 `mods/` 下的插件项目引用，不会被 `pack-mod` 命令直接打包成可部署 mod。

## Taiwu 引用和 Publicizer

这个项目按标准 C# class library 组织。`--side` 只决定生成时的默认目标框架，不自动添加太吾
引用包。需要访问更宽的游戏 API 时，在 `.csproj` 中按实际代码需要添加对应引用包。

需要访问游戏 DLL 的非 public API 时，先启用 Publicizer，再自行声明要公开化的程序集、
类型或成员：

```xml
<PropertyGroup>
  <UsePublicizer>true</UsePublicizer>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="Krafs.Publicizer" PrivateAssets="all" />
  <Publicize
    Include="Assembly-CSharp"
    IncludeCompilerGeneratedMembers="false"
    IncludeVirtualMembers="false"
  />
</ItemGroup>
```

前端常用 `Assembly-CSharp`，后端常用 `GameData`。如果只需要具体类型或成员，优先写更窄的
`Publicize Include`。
