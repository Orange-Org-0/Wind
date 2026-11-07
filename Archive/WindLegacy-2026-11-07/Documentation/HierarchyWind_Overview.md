# HierarchyWind 实验

组装入口：[`../../Documentation/HierarchyWind_Setup.md`](../../Documentation/HierarchyWind_Setup.md)。

Unity 菜单：**Tools → Wind → Hierarchy Experiment → 1 Generate Assets**，然后 **2 Add Demo To Current Scene**。

本目录含模型、浮点数据贴图、源生成器、Shader、运行时代码和 Editor 组装工具。当前采用一套顶点色（RGB=归一化 Pivot，A=Weight）加 1×84 Tip 数据贴图，通过 SV_VertexID 读取，无 UV 数据或索引。按要求未验收；默认阶段是 Directional Wind。执行生成菜单后，新版资产创建到 `GeneratedColorTexture/`，旧版 Generated 保留。

主干已细分为16段（68点），两分枝仍各8点，总计84点。Blender几何/数据检查通过；Unity未验收。现有Shader仍为主干整体旋转，连续弯曲及分枝连接处跟随尚待接入。

新增独立基础弯曲入口：Shaders/TreeWind.hlsl 与 TreeWindTest.shader。仅输入方向/强度做主干连续弯曲和刚性分枝跟随，见 ../../Documentation/TreeWind.md；原版运动Shader不变。

正式统一入口为 TreeWind，采用 ASE File Custom Expression；HLSL 已通过 ASE_ABSOLUTE_VERTEX_POS 适配位置/位移两种模式，，当前对ASE仅返回单一Float3，未输出法线。实际 Shader 由用户的 ASE 图接入。
