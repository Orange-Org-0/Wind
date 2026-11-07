# TreeWind JSON Bounds

`Scripts/TreeWindJsonBounds.cs` reads the assigned
JSON `vertices` array (objects with x/y/z fields), computes the original local
bounds using the same algorithm and 0.0001 minimum size as HierarchyWindAssembler,
and writes `_HWBakeMin` / `_HWBakeSize` to an explicitly assigned material.

## Setup

1. Expose Vector properties named `_HWBakeMin` and `_HWBakeSize` in the ASE shader.
2. Add TreeWindJsonBounds to an object; assign Source Json and Target Material.
   The current source is `Models/HierarchyWindTree.json`.
3. Open the component context menu and select **Apply JSON Bounds** after assigning
   or changing inputs. Save the material/project normally to persist editor edits.
4. Decode `PivotOS = VertexColor.rgb * _HWBakeSize.xyz + _HWBakeMin.xyz`.
   RootOS remains independently authored; the current model uses (0,0,0).

Configured components also apply on enable in Edit Mode and Play Mode. There is
no Update loop or automatic JSON reimport watcher. The component writes directly
to the assigned material, with editor Undo support; all objects sharing that
material share these bounds. Use the JSON matching the actual baked vertex colors.
Do not use Renderer bounds, which may have been expanded for deformation.
Existing MaterialPropertyBlock overrides of these properties take precedence.

## Validation

Source and JSON format inspected; current JSON bounds were previously computed as
min=(-1.1255233,0,-0.2295386), size=(2.2039812,3,0.4392384).
Unity asset refresh imported the component and generated its meta file. After
domain reload, UnitySkills reported compilation idle and zero errors for this
script. Warning diagnostics were not separately queried. No scene,
renderer, or material was assigned or saved automatically. Shader connection,
Play Mode behavior, visuals, batching, and performance remain unverified.
