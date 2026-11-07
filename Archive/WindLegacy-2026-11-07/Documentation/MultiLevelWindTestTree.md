# MultiLevelWindTestTree

## 2026-11-01 数据贴图修订

当前 1×84 伪顶点色贴图 RGB 为归一化 Tip：`(TipOS-BakeBoundsMin)/BakeBoundsSize`，采样后使用 `RGB*BakeBoundsSize+BakeBoundsMin` 还原。A 保留为 0。与 Vertex Color 的 Pivot 共用制作时边界；JSON 继续保留原坐标。此规则替代下文所有“贴图存原始 Tip.xyz”的历史描述。

## 2026-10-31 主干细分修订（当前模型）

- 当前实验模型为 `Models/HierarchyWindTree.fbx`，原始 `Models/MultiLevelWindTestTree.fbx` 保留。
- 主干沿高度均匀划分 16 段，17 个截面环、每环 4 点，共 68 点；两侧枝条仍各 8 点、保持刚性盒子。总计 84 个源顶点、78 个四边面（156 个三角形），三部分仍为相交的独立岛。
- 保留原尺寸、分叉位置、根部位置和 Pivot/Tip/Weight 含义。JSON 顺序为主干 0–67、下分枝 68–75、上分枝 76–83。主干 Weight=0；各分枝全部八点 Weight=1。
- 同步生成 FBX、JSON、1×84 Tip 浮点 EXR 及项目根目录 SourceAssets/Wind/HierarchyWind/ 下的已有 Blender 源文件。顶点色继续保存归一化 Pivot RGB 和 Weight A，无 UV 载荷。
- 数据纹理实际高度应始终等于最终原生 Unity Mesh 的顶点数；本模型组装器从 JSON 构建时为 84。禁止将可能拆分/重排顶点的 FBX 导入网格直接代替该原生 Mesh。
- 组装器的网格名称改为使用实际顶点数；保留已有生成资产的行为不变。如已生成旧 GeneratedColorTexture，先在 Unity 中重命名备份，再执行生成菜单得到新模型，旧场景对象不会自动升级。
- 本次只调整模型几何及匹配数据，未改变运动 Shader：现有 Shader 仍对主干做整体刚性旋转，新增分段为后续连续弯曲提供几何条件。后续需要在主干连接高度计算变形后的位置/朝向，分枝整体跟随该位置/朝向，不能对分枝八点分别按各自高度弯曲，否则会造成分枝变形。
- 已执行 Blender 导出后重导入检查：84 点、78 面、三个岛为 68+8+8，主干 17 环、根部高度 0、封闭边及朝外面、JSON 记录数与全部 Tip 贴图行一致，检查通过。
- 未执行 Unity 编译、导入、Scene/Game、Play Mode 或性能验收；本机 8090–8100 未发现可连接的 UnitySkills 服务。Blender 保存时报告默认笔刷材质路径无法转为相对路径，不影响本模型的几何/数据检查。

以下历史记录中出现的 24 顶点与 1×24，均由本节的 84 顶点与 1×84 替代；历史两列贴图/UV3 方案继续作废。

## Purpose

`Models/MultiLevelWindTestTree.fbx` is a simple connected test mesh for validating two-level GPU vegetation bending before using a final reed or tree asset.

## Shape

- One Mesh object containing three intersecting box islands.
- Exactly 8 vertices per box, 24 vertices total, and 18 quad faces.
- Approximately 3 m tall.
- Main trunk is one elongated box.
- Lower branch begins near 1.2 m.
- Upper branch begins near 1.8 m.
- Root surface and object origin are at local height 0.
- Exported as Unity Y-up with identity scale.

## Data convention

**2026-10-31 修订：下文两列贴图/UV3 布局已被替代。当前批准方案为一套顶点色加剩余数据贴图：Color RGB 存按模型包围盒归一化的 Pivot，A 存 Weight；1×24 浮点贴图存 Tip.xyz；通过 SV_VertexID 读取，无 UV 数据或索引。实际新版模型与工具位于 Experiments/HierarchyWind，见 HierarchyWind_Setup.md。下文保留为原始方案记录，不作为实施规则。**

The vertex data texture path is approved. The existing FBX is still geometry-only until the baking step is implemented; approval of this specification does not claim that the payload already exists.

### Information stored per hierarchy level

Each independently rotating child level stores seven scalar values per authored vertex:

- `Pivot.xyz`: the rotation centre in final Unity object space.
- `Tip.xyz`: the branch tip in final Unity object space.
- `Weight`: influence in the range 0 to 1. It also serves as membership for this level, so no separate enable mask is stored.

The first whole-object bend is calculated from object-space height and does not use a baked hierarchy record. A future additional independently rotating level requires another complete `Pivot + Tip + Weight` dataset.

### Approved texture layout

For the current 24-source-vertex test mesh, one hierarchy level uses a `2 x 24` floating-point data texture. Authored source vertex `i` owns row `i`.

| Texel | RGBA payload |
| --- | --- |
| Column 0, row `i` | `Pivot.x`, `Pivot.y`, `Pivot.z`, `Weight` |
| Column 1, row `i` | `Tip.x`, `Tip.y`, `Tip.z`, reserved |

The shader samples the two column centres at normalized X coordinates `0.25` and `0.75`. The vertex's row-centre coordinate is `(i + 0.5) / 24`.

The row lookup is stored in Unity UV3 (`Mesh.uv3` / `TEXCOORD2`):

- `UV3.x`: normalized row-centre coordinate.
- `UV3.y`: reserved.

UV0 (`Mesh.uv` / `TEXCOORD0`) remains available for material mapping. Unity UV2 (`Mesh.uv2` / `TEXCOORD1`) remains available for lightmapping. The payload is not stored in Unity's single Color semantic.

### Current model values

- Main trunk vertices use secondary-level `Weight = 0`; their Pivot and Tip values are ignored by the shader.
- Each branch's eight vertices share that branch's object-space Pivot and Tip.
- Each branch's eight vertices use `Weight = 1` for a rigid child-level rotation around its Pivot. With only eight vertices per box, this test intentionally validates hierarchical rotation rather than smooth curvature inside a branch.
- Blender or another DCC must convert Pivot and Tip into the same final Unity object space as the exported mesh before writing the texture.

### Unity texture import requirements

- Linear data (`sRGB` disabled).
- RGBA Half or equivalent floating-point precision.
- No texture compression.
- Point filtering.
- Clamp wrap mode.
- Mipmaps disabled.

This is a vertex-data texture, not a visible colour texture. The vertex shader must use explicit LOD sampling. Support, shader bindings, axis conversion, and the imported UV lookup must be verified in Unity before the bake is described as working.

## Generation and validation

- Generator: `Tools/create_three_box_geometry_only.py`
- Validator: `Tools/validate_multilevel_wind_test_mesh.py`
- Blender: 5.2.0 LTS, background mode.
- Generated mesh: 24 vertices and 18 quad faces before FBX re-import.

The Blender FBX re-import check verifies one mesh object, three box islands, 24 vertices, 18 faces, identity scale, non-empty file data, and a root surface at Blender world Z=0 after the Unity-axis round trip. Unity compilation, scene binding, Play Mode deformation, visual quality, and runtime performance are not validated by this mesh-generation step.

Unity's FBX importer reports 60 render vertices even with calculated 180-degree normals, no tangents, and Weld Vertices enabled. The authored geometry still contains exactly 8 control vertices per box (24 total); Unity duplicates some render vertices to represent imported FBX attribute boundaries. A strict 24-runtime-vertex requirement would need a custom Unity Mesh build/postprocess path rather than relying on ordinary FBX import.
