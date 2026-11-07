# Wind migration — 2026-11-07

Source: E:\UnityHub\URP_Shaders\Assets\Wind
Destination: E:\UnityHub\Wind\Assets\Wind

Copied all 117 source files (530350 bytes), preserving .meta files and the Wind folder .meta. SHA-256 comparison immediately after copying: 0 mismatches. Source retained. No duplicate GUIDs found in destination Assets; serialized non-built-in GUID references in the copied module resolved within the module.

Destination uses Unity 6000.6.7f1 and HDRP 17.6.0. Source uses Unity 2023.2.22f1 and URP 16.0.6. Copied AGENTS.md and historical documentation describe the SOURCE environment and prior state, not the current destination. Keep destination HDRP settings unchanged.

Unity batch import completed with exit code 0; no error CS diagnostics found. Log: Logs/WindMigrationImport.log.

Rendering is NOT validated or HDRP-compatible yet. Three copied .shader files target URP. Import also reports that Experiments/HierarchyWind/Shaders/HierarchyWind.shader cannot resolve HierarchyWindDeform.hlsl (the include file is in its parent folder). No shader conversion or source-code changes were made. No Play Mode or visual verification was performed.

Scope: the complete Assets/Wind module. The old project's Assets/Scenes/Wind.unity is outside this module and was not copied. Existing destination scenes and project settings were retained.
