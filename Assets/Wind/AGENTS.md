# Wind project rules

## Current scope
- Project: E:/UnityHub/Wind; Unity 6000.6.7f1; HDRP 17.6.0.
- Keep only the WindManager -> WindTest.shader -> TreeWind.hlsl workflow.
- WindTest.shader remains the original ASE-generated URP shader. HDRP conversion was cancelled; do not claim current rendering compatibility.
- The original URP_Shaders project is a reference and must not be changed as part of cleanup.

## Layout
- Scripts/: WindManager.cs and TreeWindJsonBounds.cs.
- Scripts/Editor/: HierarchyWindAssembler.cs generates native Mesh and data texture only.
- Shaders/: WindTest.shader and TreeWind.hlsl.
- Materials/: WindTest.mat.
- Models/: FBX, native Mesh and source JSON. They are different representations, not interchangeable duplicates.
- Textures/: baked EXR and native data texture.
- Tools/: the three retained offline Blender/Python generation and inspection tools.
- Do not recreate Runtime, Experiments, HierarchyWind, GeneratedColorTexture, old demo Prefabs or Documentation wrappers.
- The user requested removal of old documentation. Keep cleanup records outside Assets in Archive/WindLegacy-2026-11-07.

## Asset and code boundaries
- Preserve .meta GUIDs and serialized references. Prefer AssetDatabase operations.
- Do not modify unrelated assets, package files or project settings.
- Do not change wind algorithms, shader graph connections or scene bindings during cleanup.
- Keep editor-only C# under Editor; retain runtime class/namespace identities.
- Preserve authored object-space Pivot/Weight in vertex Color RGB/A. Decode Pivot using bake bounds.
- The one-column floating-point texture stores normalized Tip RGB by native Mesh vertex index; no UV payload.
- Mesh vertex order must match texture rows. Do not substitute FBX geometry for the native Mesh without validation.
- Existing generated Mesh/data should not be overwritten automatically by the generator.
- Validate compile status and asset references after changes. Compilation is not proof of correct rendering or performance.