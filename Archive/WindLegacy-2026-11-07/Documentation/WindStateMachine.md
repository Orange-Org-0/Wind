# Wind 两态响应状态机

## 范围
仅 C# 风源和响应输出，不改 HLSL、材质参数、网格或 Transform。
WindBendStateMachine 为纯 C# 实例状态，WindBendResponse 是 Unity 生命周期桥接。
全局风源和每根长杆响应分离，不向全局 Shader 属性写回摆状态。

## 文件
Scripts/WindManager.cs：环境风向和风强；Active 注册避免每帧查找。
Scripts/WindBendStateMachine.cs：WindDriven/Recovering 两态。
Scripts/WindBendResponse.cs：读取风源、驱动状态机、提供 Inspector/Gizmo 输出。

## 状态
WindDriven：有风时用临界阻尼平滑接近目标角度，无风且直立时保持静止。
停风条件：水平风向或风强接近零；水平 Y 分量不参与。
使用 0.0001/0.0002 的离开/进入阈值避免零附近抖动。
有风时强度按 StrengthForMaximum 归一化并限制为 0..1。
目标角度不超过 MaximumAngle（0..110 度）。

Recovering：保持当前弯曲向量和角速度，以零为目标欠阻尼回摆。
默认 6 秒、4 个完整周期，周期数可调 3..5。
衰减包络在完整时间内降至初始约 0.1%，末尾清零避免无限微小摆动。
最终清零有微小截断，并非声称绝对无数值跳变。
恢复期间参数在进入时捕获，避免中途调周期导致相位跳变。
重新有风立即切回 WindDriven，保留姿态和速度；目标可以换向。
初始仍向外运动时可能先继续少量弯曲，这是保持速度连续的结果。

## 输出约定
CurrentBendAngle 为非负角度幅值；反向回摆通过 CurrentBendDirection 反向表示。
CurrentBendVector 为世界 X/Z 弯曲分量（度），过零连续。
角速度为相应两分量（度/秒）。不应对 Direction 单独看作连续角度。
后续 Shader 接入需同时使用方向和角度；目前尚未连接。

## 使用
场景仅保留一个启用的 WindManager；在 Play Mode 改 Wind Direction/Wind Strength。
长杆挂 WindBendResponse。可选 Wind Source 引用；留空自动使用 Active Manager。
选中长杆并开启 Gizmos，黄色调试线表示受风态，青色表示恢复态。
调试线只表示顶部朝向，不是完整网格形变。
Inspector Runtime Output 显示状态、弯曲角、方向、速度和恢复时间。
Play Mode 中把强度归零，或把方向 X/Z 归零，观察回摆。
恢复中改回有效风向与强度，观察连续切回受风态。
停止 Play Mode 后 Inspector 中运行时改值不保留。

## 可调项
Maximum Angle：满风目标角度，默认 100。
Strength For Maximum：达到最大角度所需风强，默认 1。
Driven Response Seconds：受风目标响应时间尺度，默认 0.5 秒。
Recovery Cycles：完整来回数，默认 4。
Recovery Duration：总恢复时间，默认 6 秒。
Debug Length：调试线长度，默认 2，不控制网格大小。

## 已执行算法检查
直接编译实际 WindBendStateMachine.cs 运行断言：
有风趋近目标、方向归零、强度归零、切换姿态连续、反向回摆、
逐次峰值衰减、结束直立、直立不反复恢复、途中重起风保持位置与速度、
30/144 FPS 一致性，全部通过。
默认恢复共 8 次过零，对应约 4 个周期；
反向首峰约 42 度（从初始 100 度静止释放），后续逐次下降。
30/144 FPS 在同样输入时刻的计算差约 5e-14 度。
这些是算法行为证据，不是 Shader 画面或性能证据。

## 限制
只模拟水平弯曲响应；不包含风噪声、碰撞、多级骨架、每叶状态或 GPU 大批量模拟。
每根杆一个响应组件，尚未评估多实例性能。
运行时替换网格的尺寸处理属于已有 ReedBendMeshData，不由状态机管理。
禁用或重新启用响应组件会重置为直立；禁用风源则视为停风并恢复。Unity 验证补充：脚本编译完成，WindManager 对象及其组件已创建，WindBendResponse 已挂在 WindPrism_32Segments。Play Mode 实测受风角度 99.99675 度、停风恢复中弯曲向量 (-42.0556755,0)、结束为零并回 WindDriven、重起风切回 WindDriven。测试后已退出 Play Mode。没有改 HLSL 或材质，也未保存场景。

## Renderer 绑定更新
WindBendResponse 现在自动获取同对象 Renderer，不需手动拖材质。
通过 MaterialPropertyBlock 将 CurrentBendDirection 写入 _WindDir，
将 CurrentBendAngle（度）写入 _WindStrength。
注意后者为材质已有的弯曲角名称，与 Manager 的环境风强不是同一单位。
每次写入前读取现有 Block，保留 ReedBendMeshData 的网格尺寸数据。
不使用 renderer.material，不复制或修改共享材质。
停止/禁用时恢复启用前这两个参数的值，其他 Block 数据保留。
材质更换后需重新启用组件以重新绑定与校验。
当前噪声由 _NoiseStrength * _WindStrength 控制，角度归零时噪声也归零。
方向保持非零有效值，避免现有 ASE Normalize 对零向量做归一化。
HLSL、Shader 图、材质文件及状态机核心均未修改。

原文中“尚未接入材质”的描述为之前阶段记录，以本节更新为准。
绑定验证：Unity 编译完成，Play Mode 中 rendererBound=true、hasWrittenRenderer=true，实际写入方向 (1,0,0,0) 和非零角度；Console 错误数为 0。已退出测试。此次未取得可靠的恢复阶段材质采样或视觉证据，不将编译/参数写入当作画面验证。
当前测试实例参数调整：Recovery Duration 从 6 秒改为 5 秒，Recovery Cycles 保持 4，频率从约 0.67 Hz 提高到 0.8 Hz（约快 20%）。仅修改 Inspector 实例参数，代码默认值不变；场景尚未保存。

## Ctrl+D / Prefab 复用验证
WindBendResponse 增加 RequireComponent(ReedBendMeshData)，后者自动要求 MeshFilter/MeshRenderer。
重新启用复用已有状态机对象并 Reset；各实例状态、Renderer 与 PropertyBlock 均独立。
默认/Prefab 无风源引用时使用 WindManager.Active，避免 Prefab 绑定场景对象。
场景仅需要一个 WindManager，勿将 Manager 一起放入每根杆的 Prefab。

已实际测试：原对象、复制对象、保存后重新实例化的 Prefab，共 3 个实例。
Prefab 的 windSource 为 null，仍自动绑定成功；三个实例 rendererBound 和 hasWrittenRenderer 均为 true。
临时 Prefab 实例 maximumAngle=40、原对象=100，实际写入角度分别为 3.368021 与 8.420053，
证明不同响应参数写入各自 Renderer，没有共享材质互相覆盖；未测性能或大量实例视觉表现。
Unity 编译和 Console 检查无错误。
临时复制和 Prefab 测试实例已删除，原对象未替换，测试后退出 Play Mode。

实验 Prefab：Prefabs/WindBendTest.prefab。
保留源对象当前配置（包括 5 秒恢复），可直接拖入有 WindManager 的场景。
只复制/实例化带完整组件及 WindTest 材质的对象；对象应保持直立、正缩放。
本次不修改 HLSL、材质，不保存现有场景。

## Shader 属性重命名同步
当前用户 Shader：_BendDir、_BendStrength、_NoiseSpeed、_NoiseStrength。
WindBendResponse 的 PropertyToID 和报错提示已同步为 _BendDir/_BendStrength。
这两个 ID 同时用于验证、读取、写入和禁用恢复，因此所有路径同步。
_NoiseSpeed/_NoiseStrength 仍由材质控制，响应脚本未写入。
_WindBendLocalBounds 为 HLSL 与 ReedBendMeshData 共用的内部尺寸数据，保持不变。
历史章节中的旧 _WindDir/_WindStrength 名称以本节为准。

## 重命名后的 Play Mode 集成验证
已通过 Scripts/Editor/WindBendBindingCheck.cs 的显式菜单检查。
运行时直接 GetPropertyBlock 回读 _BendDir/_BendStrength，共检查 618 次，与状态机输出一致。
受风输出 100 度；停风恢复共 8 次过零（约 4 个完整来回），最终参数归零并回 WindDriven。
重新起风、恢复中切换 +Z 风向、方向归零触发恢复均通过。
同时确认模型尺寸参数仍在 Block 中、共享材质引用未替换、Console 无错误。
测试记录：Documentation/WindBindingTestResults.json。
测试结束恢复原风向/强度、Time.timeScale 和 runInBackground 设置，并退出 Play Mode。
本次为真实 Unity 运行时参数链路验证，未采集画面对比或性能数据。
可重复测试：进入 Play Mode，Tools > Wind > Check Material Binding And Recovery。
