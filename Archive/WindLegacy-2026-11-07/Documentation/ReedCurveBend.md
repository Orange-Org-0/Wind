# ReedCurveBend：平滑芦苇弯曲原型

## 本次修改
- 实际 FBX 网格本地 bounds：center=(0,1,0)、size=(0.1,2,0.1)，272 顶点。
- 旧代码在本地 Y=1 后停止增加弯曲角，导致上半段直线；现已移除该截断假设。
- ReedBendMeshData 自动读取 sharedMesh.bounds，将底部和高度传给 GPU，无需手填高度。
- 固定平滑角度分布，移除 BendPower 控制；角度限制为 ±110 度。
- 12 步中点积分，保留 GPU 变形、固定底面、世界 +Y 向上。
- 自动组件同时设置保守 Renderer 包围盒；不改变网格或 Transform。
- 组件无 Update、不逐帧写参数；更换网格后可用 Refresh Mesh Data。

## 当前文件
- Shaders&Materials/ReedCurveBend.hlsl
- Scripts/ReedBendMeshData.cs
- Documentation/ReedCurveBend.md

## Amplify 使用
File 节点函数 ReedCurveBend；三个输入：
PositionOS(Vector3)、DirectionWS(Vector3)、TipAngleDegrees(Float)。
单 Vector3 返回值为绝对模型空间位置，接绝对 Vertex Position。
旧四输入重载暂保留以兼容当前 WindTest.shader；第四输入完全忽略，
可在 ASE 删除 BendPower 输入及其属性节点后保存，换成三输入调用。
不要手改 ASE 生成源码与图数据。
Shader Graph 对应 ReedCurveBend_float 的三输入、单 out 适配仍保留。

角度建议：30 轻弯、60 明显弯、90 顶部切线水平、100 至 110 顶部略向下。
负数向反方向弯。这里角度指中心线顶部切线，不代表叶片或穗头的朝向。

ReedBendMeshData 挂在同一个 MeshFilter/MeshRenderer 对象上，无参数可填。
没有组件时仅回退到本地高度为 1 的网格，不保证任意 FBX 尺寸正确。
输入高度权重仍只依赖网格本地坐标/本地 bounds，与世界位置和缩放无关。
正等比缩放保持相似形；不支持倾斜、剪切或负缩放。
逐 Renderer 的 MaterialPropertyBlock 可能影响 SRP Batcher；
未做性能测试，不宣称大规模方案已通过。

## 阶段边界
当前仅静态曲线，无噪声、弹簧、最终芦苇集成，也未更新法线/切线。
Unlit 轮廓先验证，后续按阶段接动态风。固定轮廓属于视觉近似，不是真实受力解。

## 验证
数学检查：新曲线底部角度为 0、顶部为指定值；110 度顶部切线向下。
12 步与 10,000 步对比，在测试角度 ±60/90/110 下最大中心线误差约高度 0.082%。
源码写入后须以 Unity 编译、实际组件绑定与画面验证分别确认。本次 Unity 验证：ReedBendMeshData 已编译并挂到 WindPrism_32Segments，原对象位置/旋转/缩放保持不变。HLSL 和 WindTest 重新导入后 shader_check_errors 为 hasErrors=false、messageCount=0。尚未验证新画面或 Play Mode；组件挂载尚未保存场景。
补充接口修复：当前 ASE 未定义 ASE_ABSOLUTE_VERTEX_POS，实际是 Offset 模式。ReedCurveBend 现按 ASE 模式自动返回位置或位移；Shader Graph 的 BentPositionOS 仍始终返回绝对模型空间位置。现有 ASE 接线不变。适配后再次编译检查 0 消息。
