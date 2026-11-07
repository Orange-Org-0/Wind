# 分叉风动实验：交付与组装

## 2026-11-01 伪顶点色归一化修订（优先于所有历史布局说明）

- 数据 EXR 的 RGB 改为 `(TipOS - BakeBoundsMin) / BakeBoundsSize`，A 仍为 0。与 Vertex Color RGB 的 Pivot 使用同一套原始制作边界。JSON 中的 Pivot/Tip 仍保留原始对象空间坐标。
- ASE 采样后必须解码：`TipOS = Sample.rgb * _HWBakeSize.xyz + _HWBakeMin.xyz`。不得把归一化 RGB 直接当作坐标。
- 保持 1×84、浮点精度、线性、Point、Clamp、无 Mipmap、无压缩、NPOT 不缩放。可按 Vertex ID 整数 Load；ASE 使用 Texture Sample 时，以 `float2(0.5,(VertexID+0.5)/Height)` 采样 LOD 0，不占用模型 UV。
- 已修改 build_assets.py、HierarchyWindAssembler.cs、HierarchyWindDeform.hlsl；新增 Source/rebuild_normalized_tip_texture.py，可仅从 JSON 重写 EXR，不重新生成 FBX 或 Blender 源模型。
- 当前 EXR 已实际重写并用 Blender 重新读回，84 行 RGBA 全部位于 0～1；逐行解码与 JSON Tip 的最大误差约 3.42e-8。未生成或替换原生 Mesh、Prefab；未运行视觉或性能验收。
- Unity 重新导入后确认尺寸为 1×84、RGBAHalf、Point、Clamp、sRGB关闭、Mipmap关闭、无压缩、NPOT=None；原贴图GUID保留。UnitySkills报告修改后的组装器编译已结束且无错误。上面的3.42e-8是EXR源文件回读误差，RGBAHalf导入后会有额外半精度量化；尚未逐像素回读Unity纹理或验证ASE画面。
- 历史原始 Tip 贴图与新的解码器不兼容，必须使用本次归一化贴图。若另有旧生成的原生 Texture2D 资产，应同步重建其数据后再使用新的解码器。

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

## 2026-10-31 当前修订（优先于下文旧版记录）

Blender 可编辑源文件和 `.blend1` 备份现存放在项目根目录 `SourceAssets/Wind/HierarchyWind/`，位于 Assets 外，避免 Unity 将 `.blend` 再导入成第二个同名模型。生成器也已更新此输出位置。Models 只保留 FBX 模型及 JSON 数据；下文原来的 Models/.blend 路径为旧记录。

按最新要求改为**一套顶点色 + 剩余数据贴图**：

- Color RGB：`(Pivot - BakeBoundsMin) / BakeBoundsSize`；Color A：Weight。
- 贴图：`1×24` RGBA Half，每行保存该顶点的 Tip.xyz，A 预留。
- Shader 以 `SV_VertexID` 作为行号直接整数读取；不通过任何 UV 传递数据或贴图索引。
- 包围盒解码参数 `_HWBakeMin`、`_HWBakeSize` 由组装器写入共享材质，后续运行时扩展渲染包围盒不影响解码。
- 仍执行原来的两个 Unity 菜单，但新版输出目录改为 `GeneratedColorTexture/`。旧 `Generated/` 保留，旧对象不自动升级。请使用菜单新生成的 Prefab，或重新添加一组 Demo。
- Blender 源文件、FBX 和 EXR 已按新布局重新生成。正式组装仍使用 JSON 生成原生 Mesh 和贴图，避免 FBX 重排导致顶点编号不一致。
- 禁止静态批处理、Mesh.CombineMeshes、Mesh.Optimize、顶点重排或直接替换导入 Mesh；这些操作可能改变编号，必须同步重建贴图。Shader 禁用普通 batching。GPU instancing 已保留声明，但尚未验收。
- 当前没有运行 Unity 验收；原有运动、外观和性能限制继续适用。

以下为首版交付记录，其中 `2×24`、`UV3` 与 `Generated/` 的布局描述仅供历史参考，实施以本节和当前代码为准。

交付日期：2026-10-31。当前状态：文件已编写，Blender 生成流程已执行。按本次要求未进行任何验收：没有启动 Unity 编译检查、场景绑定检查、Play Mode、视觉或性能测试。以下行为是设计目标，不是已验证结果。

## 文件分类

本轮文件集中在 `Assets/Wind/Experiments/HierarchyWind/`：

| 路径 | 用途 |
| --- | --- |
| `Models/HierarchyWindTree.fbx` | 新的实验 FBX，一个 Mesh、三个长方体部分，设计为每部分 8 个源顶点 |
| `Models/HierarchyWindTree.blend` | Blender 可编辑源文件 |
| `Models/HierarchyWindTree.json` | 最终 Unity 对象空间的几何与 Pivot/Tip/Weight，组装工具的权威输入 |
| `Textures/HierarchyWindTree_Data.exr` | 已生成的浮点数据贴图，2×24 像素 |
| `Source/build_assets.py` | 本机 Blender 后台生成器；重新运行会覆盖本目录 Models 和 Textures 中的同名输出 |
| `Shaders/HierarchyWind.shader` | URP 主光漫反射、主光阴影、ShadowCaster、DepthOnly |
| `Shaders/HierarchyWindDeform.hlsl` | 读取数据贴图、子枝条局部旋转、父级整体旋转及法线旋转 |
| `Scripts/Runtime/HierarchyWindField.cs` | 共享世界空间风向、强度及平缓扰动 |
| `Scripts/Runtime/HierarchyWindResponse.cs` | 每实例响应、可切换阶段、弹簧状态、历史缓冲和 MaterialPropertyBlock |
| `Scripts/Editor/HierarchyWindAssembler.cs` | 在 Unity 中生成 Mesh、原生数据纹理、材质、Prefab，并添加示例对象 |

原有 `Models/MultiLevelWindTestTree.fbx` 与既有风系统没有被本轮覆盖。

## 最快组装步骤

1. 用 Unity **2023.2.22f1 / URP** 打开项目，等待文件导入。若出现编译错误，先保留 Console 信息供修复；本轮没有执行编译验收。
2. 点击菜单 **Tools → Wind → Hierarchy Experiment → 1 Generate Assets**。工具会在实验目录创建 `Generated/`，生成：24 顶点的 Unity Mesh、RGBA Half 数据纹理、共享材质和 Prefab。该操作是资产制作步骤。
3. 打开你希望使用的场景，点击 **Tools → Wind → Hierarchy Experiment → 2 Add Demo To Current Scene**。它会添加一个根对象和三个模型，自动连接共享风场；三个模型位于 X=-3、0、3，根部 Y=0。这个菜单每次都会添加一组，请勿重复点击，除非想增加示例。
4. 为场景准备能看到模型的 Camera 和一个 Directional Light。场景原有相机、灯光、旧风模型由你决定保留或隐藏。默认观察范围大约 X=-5～5、Y=0～3。
5. 进入 Play Mode。选中根对象 `HierarchyWindExperiment` 调风向和风力；选中子模型调 Motion Stage、转角和弹簧参数。确认当前场景后自行保存。

如果只要一个模型：把 `Generated/HierarchyWindTree.prefab` 拖进场景，创建一个空对象挂 `HierarchyWindField`，再把它拖到模型 `HierarchyWindResponse.field`。三个示例共用一个材质，但每个模型保存自己的响应状态。

组装工具只在同名 Generated 资产缺失时创建，保留已有资产及 GUID。修改源数据后若需重建，请先在 Unity 中将原 Generated 文件夹重命名备份，再运行菜单；旧场景引用不会自动迁移。Prefab 的 field 默认留空，必须由场景提供。

## 四种运动阶段

| Motion Stage | 操作与设计效果 |
| --- | --- |
| Static Pose | Static Pose 的 X/Y 分别代表世界 X/Z 响应，用来手动摆出静态姿势 |
| Directional Wind（默认） | 直接读取共享风场，适合先组装出随风运动 |
| Spring | 每个模型保存二维响应与速度，风强变化或停风后产生阻尼回弹 |
| Delayed Spring | 主干读当前弹簧值，分支读历史值；Delay Seconds=0 可回到同步响应 |

切换阶段自动清空弹簧及历史状态；首次切入延迟阶段时，历史缓冲从零开始，因此会经历一段建立历史的时间。组件菜单 `Reset Motion` 可手动复位。

## 参数

共享风场：

- Direction：世界空间方向，只用 X/Z；方向长度不影响强度，零方向等同零风。
- Strength：默认 0.6；采样输出限制在 0～2。
- Gust Amplitude：默认 0.25，风力平缓扰动幅度。
- Gust Speed / Spatial Frequency：默认 0.65 / 0.3；两个连续正弦组成轻量扰动，模型所在世界位置改变相位。这里是艺术化扰动，不是流体模拟。

每模型响应：

- Trunk Angle / Branch Angle：默认 18° / 24°，每单位风响应的旋转幅度；单层最终角度限制到 75°。
- Branch Variation：默认 0.15，按分支 Pivot→Tip 方向给两枝不同的幅度。
- Spring Frequency：默认 1.1 Hz；Damping Ratio：默认 0.38，越高回弹越克制。
- Delay Seconds：默认 0.18 秒，可调 0～2 秒。缓存的是响应值，绝不是主干上一帧的世界位置。
- Bounds Padding：默认 4，对渲染包围盒的每侧扩展量；修改后重新启用组件使其生效，较大的模型需要相应增加。

材质：Base Color、数据贴图、Root Pivot Object Space。当前模型的根部中心是 `(0,0,0)`。建议模型使用单位缩放或等比缩放，支持绕 Y 转向；倾斜的本地向上轴、负缩放和非均匀缩放不属于当前设计支持范围。

## 数据与旋转约定

一层分支信息保持已批准的七个标量：`Pivot.xyz + Tip.xyz + Weight`。第 i 个源顶点对应纹理第 i 行，行中心 `(i+0.5)/24` 存在 `Mesh.uv3.x / TEXCOORD2.x`；纹理两列采样 X 分别为 0.25 和 0.75。第 0 列是 Pivot+Weight，第 1 列是 Tip+预留。主干 Weight=0；每根枝条的全部八个顶点 Weight=1，同枝共用 Pivot/Tip。Tip 在当前版本用于产生分支幅度差异，也为以后升级弯曲保留几何信息。

两枝 Pivot/Tip 分别为：`(0,1.20,0) → (-1.05,2.05,0.10)`、`(0,1.78,0) → (1.00,2.62,-0.12)`。主干从 `(0,0,0)` 到 `(0,3,0)`。这沿用已有测试结构，属于近似分叉：两枝从主干不同高度伸出，并非严格对称的 Y。

先在对象空间对子枝条做刚性旋转，再对全部顶点施加当前父级旋转；这是“先整体变换支点与旋转轴，再绕变换后的支点做子层旋转”的等价表达。这样子枝条 Pivot 始终跟随当前主干。延迟只影响子层转角。

本实验为保持八顶点棍子的长度，采用整段绕根部中心的刚性旋转；根部中心固定，底面角点会随转动扫动，并不保证整个底面四点锁死。若要求底面四点绝对固定且上段保持完全刚性，需要额外的连接/过渡几何，这是八顶点模型的限制。它与既有连续高度弯曲 Shader 是独立实验。

Unity 组装器从 JSON 原始数据直接生成 Mesh 和 Texture2D `.asset`，避免 FBX 导入对顶点拆分、UV 集排序、坐标转换造成映射不一致。原生纹理为线性 RGBAHalf、Point、Clamp、无压缩、无 Mipmap；Shader 用显式 LOD 0。EXR/FBX 是可编辑交换输出，默认 Prefab 不依赖其导入结果。若自行把 FBX 替换进 Prefab，需自行对齐坐标和 UV3；尚未验收这一条导入路径。不要勾选会重写数据 UV 通道的导入或处理流程。

## 后续可能需要处理的问题

- 按要求未验收，Unity 编译、Shader 变体、实际视觉、阴影、轴向、相机视角和性能均未确认；不将文件生成成功等同于效果完成。
- 24 顶点原生 Mesh 共享角点并重算法线，棱角会呈平滑着色；想要硬边通常需要拆分渲染顶点。源几何仍可保持每棍八个顶点。
- Shader 只实现主光与环境光，不含附加灯光、PBR、烘焙光照、雾、MotionVectors、DepthNormals/SSAO 专用 Pass；透明、叶片 Alpha Clip 与正式植被材质未接入。
- Shader 声明了 instancing 数据和编译变体；是否合批、SRP Batcher 行为及规模性能未测量。
- 每实例使用一个 CPU 组件和固定 512 个 Vector2 的历史缓冲，120 Hz 固定步长；每帧最多接收 0.1 秒模拟时间。长卡顿时采取时间截断，模拟时间会落后，未实现离线追帧或真实树木力学。
- 历史延迟为视觉技巧，不是双向耦合的物理链条；连接部分是相交长方体，没有蒙皮、焊接或枝干过渡面。
- 组件未连接启用的 Field 时输出零风。旧 WindManager 不会自动驱动本实验，需要使用本实验的 Field；旧风系统可继续独立存在。
- 当前没有真实芦苇/树叶资产，没有动态玩家风或 Vortex；这些需要后续资产选择与扩展。

## 本轮实际执行记录

编写上述源文件并运行 Blender 生成模型、EXR、JSON 和 `.blend`。Blender 报告保存成功，并提示其安装目录中默认笔刷材质路径无法转为相对路径；本实验没有使用这些笔刷。没有运行任何验收脚本，也没有在 Unity 中执行生成菜单、添加对象或保存场景。Generated 资产由你按上面的菜单步骤制作。

无需额外选择即可先组装当前效果；后续若要改变分叉结构、硬边外观、固定整个底面或接正式植物资产，再确定对应要求。
