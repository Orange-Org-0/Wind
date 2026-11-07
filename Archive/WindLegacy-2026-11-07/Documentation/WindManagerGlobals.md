# WindManager global output (2026-11-01)

`Scripts/WindManager.cs` owns the current fixed-rest Strength spring. See `SpringReturn.md` for implementation, tuning and actual validation.

- Global `_BendStrength` is the C# response, not always raw WindStrength. While wind is active it holds the input strength without oscillation; on release it returns toward destination=0 with signed damped oscillation.
- Global `_BendDirection` is normalized horizontal world direction; the last active valid direction is preserved during release. Zero/invalid horizontal direction falls back to the default (1,0,0); use WindStrength=0 to release the spring.
- Both Edit and Play Mode integrate release toward fixed destination=0. Edit Mode uses the editor clock; the editor callback skips Play Mode, which uses Time.deltaTime. Changed response requests preview repaint; cached IDs/last values avoid redundant global writes. Enable resets state; disabling removes the editor callback and clears output.
- Active, WindDirection and WindStrength remain available. SpringStrength and SpringVelocity expose current state for inspection.
- The current ASE shader has Global nodes and reads the globals directly. TreeWind.hlsl now clamps signed Strength to -1..1, so negative spring output reverses trunk and branch bending. See the signed Shader follow-up in SpringReturn.md for validation.
- Historical `_BendDir`, WindBendResponse and older hierarchy response experiments remain unchanged. This manager owns a shared scalar response, not independent spring states per plant.