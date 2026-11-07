# Wind 文件夹使用说明

当前项目：Unity 6000.6.7f1 / HDRP 17.6.0。

| 文件夹 | 放什么 |
| --- | --- |
| Scripts | 游戏运行时使用的 C#，例如 WindManager、HierarchyWindResponse |
| Scripts/Editor | Unity 编辑器菜单、检查器、资源组装工具；这个 Editor 层必须保留 |
| Shaders | .shader 和配套 .hlsl；当前主要编辑 WindTest.shader 与 TreeWind.hlsl |
| Materials | 材质，例如 WindTest.mat；在 Inspector 中调参数 |
| Models | FBX、Unity 原生 Mesh，以及生成 Mesh 所用的 JSON |
| Textures | EXR 与 Unity 原生数据贴图 |
| Prefabs | 可拖入场景的预制体 |
| Tools | 离线 Python / Blender 生成和检查工具，不是挂到物体上的 C# |
| Documentation | 设计说明、历史记录和验证结果 |

## 常用入口

- 风场控制：`Scripts/WindManager.cs`
- 层级树风算法：`Shaders/TreeWind.hlsl`
- 原 ASE 图生成的 Shader：`Shaders/WindTest.shader`
- 对应材质：`Materials/WindTest.mat`
- 数据组装菜单实现：`Scripts/Editor/HierarchyWindAssembler.cs`
- 组装后的模型 / 数据 / 材质 / 预制体分别在 Models、Textures、Materials、Prefabs。

## 本次整理

通过 Unity AssetDatabase 移动了 30 个文件，保留原 .meta / GUID。
合并 HierarchyWind 内重复的分类目录；移除 Scripts/Runtime 中不必要的 Runtime 层。
旧 Experiments 中的离线工具放入 Tools，历史日志放入 Documentation。
生成器改为使用本项目的新目录，历史 Python 脚本不再写回 URP_Shaders。
未运行模型生成器，未修改 Shader 算法、材质参数或场景绑定。
实验代码按用途归类后仍然是实验代码，不表示已通过正式验收。

当前 Shader 仍为原 URP 实现，本次没有进行 HDRP 转换。
部分历史芦苇脚本/检查工具引用的旧模型或 Shader 在整理前已不在此模块中；保留源码供参考，不自动重建。

整理前完整备份与逐文件移动表：项目根目录 `Tools/WindFolderCleanup-2026-11-07/`。
## 整理后检查

- 30 个移动文件全部存在，GUID 对比为 0 差异。
- Shader、材质、Mesh、贴图、Prefab 和运行时 C# 的内容哈希均未变化。
- 修改内容仅涉及组装器/离线工具的路径、目录说明和项目规则。
- Unity 已结束编译与导入；控制台留有一条 UnityEditor.Experimental.EditorResources.BuildCatalog 的 EndOfStreamException，未将其判定为已修复，也未清理用户控制台。
- 未执行 Play Mode 或重新生成模型；本次是目录整理，不代表 HDRP 渲染验收。
