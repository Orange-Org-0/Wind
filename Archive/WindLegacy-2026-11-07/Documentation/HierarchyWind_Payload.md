# 当前模型数据布局

本记录对应 Experiments/HierarchyWind 中的模型。FBX 线性颜色导出已修正，并用 Blender 回读实际 FBX/EXR：24 个源顶点，一套 PivotWeight 颜色，0 个 UV 集，1×24 浮点 EXR。RGB 回读值与原始包围盒归一化坐标一致到打印的六位小数。未检查 Unity 导入和运行效果。

| 存储 | 内容 |
| --- | --- |
| 顶点色 RGB | 分支旋转中心 Pivot.xyz，按源模型包围盒归一化 |
| 顶点色 A | 当前子层旋转影响 Weight：主干 0，两个枝条各自全部顶点 1 |
| HierarchyWindTree_Data.exr RGB | 该顶点所属棍子的末端 Tip.xyz，最终 Unity 对象空间原始浮点坐标 |
| EXR A | 预留，0 |

Pivot 解码：RGB × _HWBakeSize.xyz + _HWBakeMin.xyz。没有 UV 信息或索引；Shader 用 SV_VertexID 读取同编号贴图行。原生 Mesh 与纹理必须由同一份 JSON 生成并保持顶点顺序。组装后 Textures/HierarchyWindData.asset 是同一份 Tip 数据的 Unity 原生 RGBAHalf 副本，不是额外的一类信息。

| 部分 | Pivot | Tip | Weight |
| --- | --- | --- | --- |
| 主干，行 0–7 | (0,0,0) | (0,3,0) | 0，跳过子层旋转 |
| 左枝，行 8–15 | (0,1.2,0) | (-1.05,2.05,0.1) | 1 |
| 右枝，行 16–23 | (0,1.78,0) | (1,2.62,-0.12) | 1 |

每枝的八个顶点共享 Pivot、Tip、Weight。Weight 是刚性分支旋转的整枝影响，不是沿枝条从根到梢的渐变弯曲权重。会议讨论过 Pivot+Tip 推导权重，或直接提供权重；当前保留三项，满足旋转中心、末端与影响数据需求。会议没有规定此通道布局，它来自后续批准的工程方案。

整体主干根部旋转另由材质 _HWRoot 提供，不需要每个顶点再存一套主干 Pivot。动态风、弹簧与历史值保存在运行时状态，不烘焙进静态贴图。当前没有颜色外观贴图、法线贴图或独立权重贴图。
