# {{ProjectName}}

太吾绘卷 mod 仓库内部共享项目，不直接参与 `pack-mod` 输出。

## 开发

从仓库根目录构建：

```powershell
dotnet build shared/{{ProjectName}}/{{ProjectName}}.csproj
```

特殊目标框架、Taiwu 引用和 Publicizer 设置写在 `.csproj`；只在影响调用方或 API 边界时说明。
