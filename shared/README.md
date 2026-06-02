# shared

内部共享项目目录。

每个一级子目录是一个可被多个 mod 引用的内部项目。这里的项目不是游戏可部署 mod，不参与
`pack-mod` 输出，也不默认执行 ILRepack 内部化；最终插件项目引用它们后，由插件自己的构建和打包
流程决定如何产出 DLL。

新建内部共享项目：

```powershell
dotnet run --project tools/Taiwu.Mods.Cli -- create-shared --name MyCompany.Taiwu.Shared
```

默认 `Shared` 项目是普通 C# class library，不自动引用太吾游戏包。需要访问游戏 DLL 或
non-public API 时，在项目自己的 `.csproj` 中显式添加对应 `PackageReference`、Publicizer
配置和具体 `Publicize` 项。
