# AGENTS.md

## Current project override — 2026-11-07

This copy belongs to `E:/UnityHub/Wind`, Unity `6000.6.7f1`, HDRP `17.6.0`.
The older URP environment and scene descriptions below are historical source-project notes.
Do not switch the destination pipeline. Shader conversion was cancelled by the user.

Use the simplified current layout: `Scripts/` for runtime C#, `Scripts/Editor/` for
editor-only C#, `Shaders/` for ShaderLab/HLSL, `Materials/`, `Models/`, `Textures/`,
`Prefabs/`, `Tools/` for offline Python generators, and `Documentation/` for notes.
Do not recreate Runtime, HierarchyWind, GeneratedColorTexture or Experiments wrappers.
The hierarchy wind content remains experimental even though its files are now grouped by type.
See `Documentation/FolderGuide.md` for the current paths and migration notes.


## Project Overview

This folder contains a reusable URP vegetation-wind prototype. The immediate goal is to validate the core deformation model on a simple elongated Cube before adapting it to reeds, grass, branches, or other vegetation.

The implementation should be reusable outside the current demonstration scene and should not depend on a specific camera, GameObject name, or scene-only reference.

## Environment

- Unity version: `2023.2.22f1`
- Render pipeline: Universal Render Pipeline (URP)
- Primary language: C#
- Shader options: URP Shader Graph, Amplify Shader Editor, or custom ShaderLab/HLSL
- Working scene: `Assets/Scenes/Wind.unity`
- Wind module root: `Assets/Wind/`

Do not change the Unity version or render pipeline unless the user explicitly requests it.

## Current Weekly Task

Complete the second task from this week's plan: create a simple test model that can be driven by a simulated wind effect.

The current test object is `WindTestCube`:

- Position: `(0, 0.5, 0)`
- Rotation: `(0, 0, 0)`
- Scale: `(0.5, 3, 0.5)`

### Required first implementation

1. Apply one configurable global wind direction.
2. Add time-varying noise so the motion is not perfectly uniform.
3. Make the elongated object bend or rotate from a stable base/reference point instead of translating as a rigid block.

### Completion standard

The Cube visibly sways in Play Mode under a single wind direction with noise variation. Its base remains visually stable, the mesh does not translate as one rigid block, and the important parameters can be adjusted without editing shader code.

The first implementation does not require the final reed model, dynamic Vortex interaction, multi-level pivots, or spring simulation.

## Reference Direction

Use the vegetation response in `Ghost of Tsushima` as the primary visual and technical reference for a lightweight solution:

- global wind for the baseline motion;
- local Vortex-style wind only as a later extension;
- vegetation motion based on pivot-aware rotation or bending;
- per-instance state and spring return only after the basic wind deformation works.

The `God of War` wind-field example is a comparison reference for volume-based and heavier simulation approaches. Do not build a full voxel or fluid wind field for this prototype unless explicitly requested.

The exact titles and URLs of the referenced GDC videos are currently unconfirmed. Do not invent them. Treat conclusions beyond the meeting notes and available reference material as inference.

## Implementation Stages

Work in this order and validate each stage before starting the next:

1. **Static deformation test**
   - Verify that height or local position can be used as the deformation weight.
   - Keep the bottom of the Cube fixed while the top moves.
2. **Global directional wind**
   - Expose wind direction and strength.
   - Normalize direction safely and handle zero strength.
3. **Noise variation**
   - Expose speed, spatial frequency, and amplitude.
   - Avoid obvious synchronized motion across multiple instances.
4. **Pivot-aware vegetation support**
   - Replace the temporary height-weight assumption with authored vertex data when required.
   - Reserve vertex-color channels deliberately and document their meaning.
5. **Spring simulation**
   - Add persistent state only after the stateless wind result is correct.
   - Wind stopping should produce damped overshoot and eventual rest.
6. **Final vegetation asset**
   - Test a reed or similarly tall plant only after the Cube prototype passes.

Do not merge several stages into one large implementation.

## Suggested Asset Organisation

Create subfolders only when the corresponding asset type is actually needed:

```text
Assets/Wind/
├── AGENTS.md
├── Shaders/
├── Materials/
├── Scripts/
│   ├── Runtime/
│   └── Editor/
├── Models/
├── Textures/
├── Prefabs/
├── Documentation/
└── Experiments/
```

Keep prototypes in `Experiments/`. Do not describe experimental assets as production-ready.

## Shader Rules

- Remain compatible with Unity `2023.2.22f1` URP.
- Prefer GPU vertex deformation for visual wind; do not update every vegetation Transform from C# each frame.
- Keep object, world, and tangent-space calculations explicit.
- Preserve a stable base by using a height/vertex weight mask rather than moving every vertex equally.
- Expose only useful artist controls, grouped and named clearly.
- Provide safe defaults and clamp values that can easily produce broken geometry.
- Avoid unnecessary texture samples, shader variants, keywords, and transparent rendering.
- Use opaque or Alpha Clip rendering for vegetation unless transparency is genuinely required.
- Consider GPU instancing and SRP Batcher compatibility; verify rather than merely claiming support.
- Do not hard-code scene names, camera names, asset paths, or one specific model height.

## Runtime Code Rules

- Keep runtime scripts under `Scripts/Runtime/` and editor-only tools under `Scripts/Editor/`.
- Use shared/global shader parameters for scene-wide wind when appropriate.
- Use `MaterialPropertyBlock` or another per-renderer data path only when instances genuinely need different values; do not duplicate materials per object by default.
- Avoid `Find*` calls, allocations, and unnecessary property writes every frame.
- Cache references and shader property IDs.
- Keep simulation state ownership explicit: global wind describes the field; each spring-enabled instance owns or indexes its own response state.
- Do not create one MonoBehaviour per leaf or per vertex.

## Vertex Data Convention

The approved authored-data path is one Unity vertex-color set plus a floating-point data texture.

- The current experimental test asset is `Experiments/HierarchyWind/Models/HierarchyWindTree.fbx`: one Mesh with a 16-segment trunk (68 authored vertices) and two rigid branches (8 vertices each), 84 authored vertices total. The original `Models/MultiLevelWindTestTree.fbx` remains a historical 24-vertex geometry asset.
- The first whole-object bend remains derived from object-space height and does not consume baked hierarchy data.
- Each independently rotating child level stores exactly `Pivot.xyz`, `Tip.xyz`, and `Weight`: seven scalar values per authored vertex.
- `Pivot` and `Tip` are positions in the final Unity object space, never world-space positions or separate island-local coordinates.
- `Weight` is the influence and membership mask for that level. Do not add a separate enable mask unless a later requirement proves it necessary.
- Store normalized object-space `Pivot.xyz` in vertex Color RGB and `Weight` in Color A. Decode Pivot using the authored mesh bounds min and size, stored on the material.
- Store normalized object-space `Tip.xyz + Reserved` in a one-column floating-point data texture (1 x N, where N is the final native Mesh vertex count; 84 for the current JSON-built test). Encode RGB as `(Tip - BakeBoundsMin) / BakeBoundsSize` and decode using the same bounds as Pivot. Alpha remains reserved (0). Read row `SV_VertexID` using an integer texture load, or sample the texel centre at LOD 0 using a Vertex-ID-derived UV. No mesh UV channel stores payload or lookup information.
- The native Mesh vertex order must match the data texture rows. Do not optimize/reorder vertices, static-batch, combine meshes, or substitute an FBX-imported mesh without rebuilding its texture mapping. GPU instancing must keep each instance's local vertex numbering; support remains unverified.
- Use linear, uncompressed, point-filtered, clamp, no-mipmap texture import settings. The target precision is RGBA Half or an equivalent floating-point format.
- Add another complete `Pivot + Tip + Weight` dataset only when another independently rotating hierarchy level is actually introduced.

The detailed model and packing specification is `Documentation/MultiLevelWindTestTree.md`. Do not reinterpret the word "vertex color" from meeting notes as permission to invent multiple Unity Color semantics or a different channel layout.

## Performance Requirements

- The baseline wind should run primarily on the GPU.
- Test more than one instance before claiming the solution scales.
- Separate shader deformation cost from C# simulation cost when profiling.
- Do not infer performance from Edit Mode responsiveness alone.
- Record the test conditions when reporting numbers: instance count, mesh vertex count, resolution, graphics API, and whether Play Mode or a build was used.

## Validation Workflow

For every implementation stage:

1. Confirm the intended files and assets before editing.
2. Make the smallest reasonable change.
3. Confirm Unity compilation completes without new errors or warnings.
4. Inspect the material and renderer bindings.
5. Test in both Scene view and Game view.
6. Enter Play Mode and verify motion over time.
7. Test zero wind, low wind, strong wind, and at least two wind directions.
8. Check that the base remains stable and the mesh does not visibly stretch or shear unexpectedly.
9. Save the scene only after verifying the correct active scene.

Treat these as separate confidence levels:

- static/source inspection;
- successful compilation;
- correct serialized references;
- Play Mode runtime behaviour;
- visual quality under multiple lighting and wind conditions;
- measured performance.

Never describe compilation or static inspection as proof that the visual result is correct.

## Change Boundaries

- Keep new Wind assets under `Assets/Wind/` unless an existing shared project asset must be reused.
- Do not modify unrelated Anime Tree, Snow, third-party, package, or ProjectSettings files.
- Do not add third-party packages without explicit permission.
- Preserve `.meta` files and Unity references.
- Do not manually edit Unity YAML unless ordinary Editor or UnitySkills operations cannot perform the task safely.
- Do not implement the final spring system, dynamic player wind interaction, vertex baker, or vegetation asset integration before the user approves that stage.
- Do not overwrite user-authored materials, shaders, or scripts when a separate experimental asset is safer.

## Documentation and Reporting

Keep concise implementation notes under `Assets/Wind/Documentation/` once code or shader work begins. Record:

- what was implemented;
- files created or modified;
- setup and usage;
- material and component parameters;
- validation actually performed;
- known limitations and the next approved stage.

When reporting progress, clearly distinguish confirmed facts, visual observations, measurements, and inferences. Do not exaggerate completeness or call the prototype production-ready without the required runtime and performance evidence.

## Current Known State

- The active scene is expected to be `Assets/Scenes/Wind.unity`.
- `WindTestCube` exists as the initial deformation target.
- `Assets/Wind/` is the dedicated module folder.
- No wind shader, wind controller, pivot baker, spring simulation, or final reed integration is confirmed complete yet.
