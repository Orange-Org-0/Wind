# Tree Wind

2026-10-31。统一植被顶点变形入口；当前仅实现主干连续弯曲、刚性分枝跟随与支点旋转。未来的顶点变形继续汇入这个模块，尚未实现的功能不作为当前能力。

## 文件

- `Shaders/TreeWind.hlsl`：正式模块名。
- `TreeWind(...)`：ASE File Custom Expression入口，只返回一个Float3顶点位置/位移，无Out参数；当前ASE Vertex Offset模式下输出对象空间偏移。
- `TreeWindEvaluate(...)`：内部统一计算入口，接收对象空间方向，始终输出最终对象空间位置和法线；供直接HLSL调用及后续扩展。
- `Experiments/HierarchyWind/Shaders/TreeWindTest.shader`：独立测试包装，Shader名 `Wind/Experiments/TreeWindTest`。实际材质由用户自己的ASE Shader接入。
- 旧BasicHierarchyWindBend文件已重命名；原HierarchyWind.shader及用户的WindTest.shader未修改。存在.meta时随文件移动保留GUID。

## ASE接入

Custom Expression设置：Mode=`File`，文件选择本HLSL，Name=`TreeWind`，Output Type=`Float3`，Precision=`Float`，关闭Precision Suffix（函数名不带_float）。按以下顺序建立参数：

| 参数 | 类型 | Qualifier / 来源 |
| --- | --- | --- |
| PositionOS | Float3 | In，原始对象空间顶点位置 |

| DirectionWS | Float3 | In，世界空间风向 |
| Strength | Float | In，-1～1有符号强度；负值反向弯曲 |
| TrunkHeight | Float | In，主干对象空间长度，当前模型3 |
| PivotOS | Float3 | In，Vertex Color RGB × BakeBoundsSize + BakeBoundsMin |
| BranchWeight | Float | In，Vertex Color A；主干0，分枝1 |
| TrunkMaxAngleDegrees | Float | In，建议30，限制0～75度 |
| BranchMaxAngleDegrees | Float | In，建议20；先设0只看主干跟随 |


主输出连接ASE的顶点位置/偏移端口。**不要在节点外再减一次原始PositionOS。**

- 文件保护宏：`TREE_WIND_INCLUDED`，避免重复include定义。
- `ASE_VERSION`存在且`ASE_ABSOLUTE_VERTEX_POS`未定义：返回最终位置−原始位置，供ASE Vertex Offset模式相加。
- `ASE_ABSOLUTE_VERTEX_POS`已定义：返回最终位置，供Absolute Vertex Position模式直接赋值。
- 非ASE调用：适配函数返回最终位置。
- 这些ASE宏由生成的Shader提供，本HLSL不替用户定义。TreeWind不再提供法线输出，也不再需要NormalOS输入；本轮先验证位移，ASE材质继续使用原始法线，弯曲后的光照准确性后续单独处理。内部TreeWindEvaluate保留法线计算供独立测试Shader使用。
- ASE包装使用Unity提供的`unity_WorldToObject`将世界风向转成对象空间；在Unity Shader已有矩阵声明之后include。内部函数不依赖ASE宏。

## 当前几何约定

使用当前JSON组装的84顶点原生Mesh。主干68点，分枝各8点。主干沿高度形成恒曲率圆弧、截面随局部切线旋转，最低一圈四个顶点保持固定；分枝八点共用变形后Pivot的位置和朝向，再做自身刚性偏转，避免按各顶点高度弯曲分枝造成拉伸。

Pivot使用制作时包围盒解码，与运行时扩展后的渲染包围盒无关。Tip与1×84数据纹理保留，本阶段不读取。2026-11-01起，该纹理RGB改为归一化Tip，读取后须用RGB×BakeBoundsSize+BakeBoundsMin解码；与Pivot共用原始制作边界。中间Weight为两种姿势混合，不承诺刚性；当前模型使用0/1。

强度0、零水平风向或无效高度恢复原姿势，方向长度不影响强度。当前没有时间、噪声、弹簧或延迟。支持直立本地Y主干、绕Y旋转、正均匀缩放；倾斜、非均匀/负缩放和极短粗模型未支持。

渲染器包围盒需覆盖弯曲范围。已有HierarchyWindResponse启用时可提供Bounds Padding，但其旧运动输入不驱动本HLSL；本HLSL不更新CPU包围盒。

## 验证

- 基础几何公式的既有24组数值参考检查通过：固定底部、零风还原、分枝点对距离和Pivot连接、截面宽度与中心线长度。重命名和ASE适配未改变这些几何计算。
- 重命名后DXC严格编译、警告视为错误：普通调用、ASE Offset、ASE Absolute三种宏路径均通过；每种都重复include以检查保护宏。
- 已查本项目安装的ASE `CustomExpressionNode.cs` File模式与Out端口生成逻辑，以及本项目ASE生成Shader中的`ASE_ABSOLUTE_VERTEX_POS`行为。
- 未运行ASE图生成、Unity/URP导入编译或Scene/Game/Play Mode视觉验收；离线编译不等同于ASE已接通。阴影/法线外观、批处理与性能仍未验收。

先测试强度0、0.2、1以及X/Z方向；分枝额外角先设0确认跟随，再调高观察绕支点旋转。下一步修改以该视觉测试结果为依据。
单输出修订：ASE节点删除 NormalOS 输入和 BentNormalOS Out 参数；主返回值仍为Float3。该历史修订曾用DXC检查普通/ASE Offset/ASE Absolute路径；未进行Unity视觉验收。

2026-11-01：TreeWind 的 RootOS 输入已删除，入口内部固定根部为对象空间 (0,0,0)。模型制作需将主干根部中心放在本地原点。ASE 节点同步删除 RootOS，剩余8个输入依次为 PositionOS、DirectionWS、Strength、TrunkHeight、PivotOS、BranchWeight、TrunkMaxAngleDegrees、BranchMaxAngleDegrees。内部 TreeWindEvaluate 保留原参数结构。此次已静态核对入口和调用；尚未重新运行 Shader 编译或视觉验收。

## 2026-11-01 ASE / mesh mismatch diagnosis

The current ASE graph calls TreeWind in this order: PositionOS, DirectionWS, Strength, BranchWeight, PivotOS, TrunkHeight, TrunkMaxAngleDegrees, BranchMaxAngleDegrees. The HLSL entry signature was corrected to this order; TreeWindEvaluate remains unchanged. This order supersedes the earlier table and historical notes above. Only Shaders/TreeWind.hlsl was changed; graph connections were preserved.

The shader now declares _BendDirection and _BendStrength outside UnityPerMaterial and outside Properties. The old material-property caveat in WindManagerGlobals.md no longer describes the current user-edited shader.

Verified current scene: HierarchyWindTree uses a 192-vertex Mesh, bounds centre approximately (0.02, 0.01, 1.50), size (2.20, 0.44, 3.00), and object rotation approximately (270, 0, 0). The material decoding size is (2.20398116, 3, 0.439238429). Thus geometry is Z-up in local space while the shader and JSON Pivot decoding are Y-up. This is incompatible with the documented 84-vertex native JSON mesh convention. Rotating the object does not convert its local vertices or vertex-color Pivot encoding.

Validation: force-reimported include and WindTest.shader; shader diagnostic returned no errors/messages. Scene screenshot still showed detached branches after the signature correction. Visual issue remains unresolved pending alignment of mesh and authored data in the same local space. No mesh, material, scene, or Transform was replaced; no Play Mode or performance validation performed.
## 2026-11-01 native Mesh alignment follow-up

Ran existing HierarchyWindAssembler Generate menu; created GeneratedColorTexture assets (native Mesh, matching RGBAHalf Tip texture, legacy experiment material and prefab). No new baker was implemented; original FBX preserved. Current scene HierarchyWindTree MeshFilter now references Models/HierarchyWindTree.asset; rotation reset to (0,0,0), original position and scale retained, user WindTest material retained. No ASE connections changed.

Readback: 84 vertices, 156 triangles, local bounds size approximately (2.20,3.00,0.44), matching JSON Y-up space. No unit rescaling applied; trunk height remains 3 Unity units (3 m under the project's 1 unit = 1 m convention).

Scene-view visual checks: strength 0 restored straight trunk; X-direction strength 0.2 and 1 bent the trunk; Z-direction strength 0.2 produced a different bend direction. Base appeared stationary in these views; prior detached island positions no longer observed. Branches overlap in this viewing angle; all-angle attachment/lighting quality is not fully verified. Current max-angle constants remain 100/75 and produce aggressive folding at high strength. Restored manager direction (1,0,0), strength 0.66. Shader diagnostic: no errors/messages. No Game-view, Play Mode, culling-range or performance validation. Scene remains unsaved for user review.

## 2026-11-01 signed Strength

Strength 改为 clamp(-1,1)，仅近零强度提前返回。主干和分枝角度保留 Strength 符号，负值产生反向弯曲；BranchWeight 的 0～1 限制保持不变。现有 WindManager 的固定零目标回弹通过全局 Strength 驱动，无需修改 ASE 图。编译和 Play Mode 实测记录见 SpringReturn.md 的 Signed Shader follow-up。
