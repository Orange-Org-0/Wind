# Fixed-rest Strength spring: C# stage (2026-11-01)

## Implementation

Only `Scripts/WindManager.cs` implements this stage. WindManager owns `springValue` and `springVelocity` and a private `SpringValue(deltaTime)` method. The destination is the constant zero; it never follows WindStrength.

- Wind present: directly hold springValue at WindStrength and clear springVelocity. This is an artist-controlled displacement/hold, not a simulated external-force response. A constant nonzero wind does not oscillate. Changing strength/direction or restarting wind can switch the pose immediately.
- Wind absent (strength zero): release the held value. Integrate acceleration = springForce * (0 - springValue) - springDamp * springVelocity, velocity first and value second. Keep the previous active direction during return.
- C# publishes the signed result to global `_BendStrength` and direction to `_BendDirection`. These globals remain the existing output interface.
- Spring Force = 60, Spring Damp = 5 by default. Force is stiffness, not external wind force. Supported ranges 0..1000 and 0..100. Zero force removes restoring force; zero damping removes decay. Large damping can eliminate visible oscillation.
- Maximum integration substep 1/120 s. Per frame, at most 0.25 s is simulated; longer hitch time is discarded to bound work. Small value/velocity residuals snap to zero when both coefficients are positive.
- Edit and Play Mode both simulate release. Edit Mode uses EditorApplication.update and the editor clock; Play Mode uses Update and Time.deltaTime. The editor callback skips Play Mode to avoid double integration. Enable resets state; disable clears globals and removes the editor callback.

WindManager currently owns ONE shared response for all shaders consuming these globals. It is reasonable for the current shared prototype. Independent plant responses will need independently owned states; no per-vertex/leaf MonoBehaviours are involved. Runtime does not update vegetation Transforms.

## Usage and boundary

In Edit or Play Mode, set Wind Strength to 0.6, then set it to zero. Read-only public SpringStrength and SpringVelocity expose C# state for inspection. Spring Force and Spring Damp are in the WindManager Inspector.

The initial C# stage did not modify Shader files. The signed Shader follow-up below now enables negative bending through the existing global Strength connection.

## Validation actually performed

Unity 2023.2.22f1 C# compilation completed without compiler errors. Live Play Mode readback of the actual component and cached published output:

| Phase | WindStrength | SpringStrength / published Strength | SpringVelocity |
| --- | --- | --- | --- |
| Wind held, three samples | 0.6 | 0.6 in every sample | 0 |
| Approximately 0.4 s after release | 0 | -0.17878418 | 0.6591773 |
| Approximately six further seconds | 0 | 0 | 0 |

Initial sampling attempts did not observe frame updates while Unity was in the background or transitioning/importing. Successful validation was performed with Unity foreground and Play Mode entry completed. No conclusion about the precise cause of the initial stalled sampling is claimed. The added empty-registry guard is retained for lifecycle robustness.

Left Play Mode to restore original edit-time inputs (Strength=0, direction=(1,0,0)). No scene or ASE graph save was performed. No new visual quality, multi-instance or performance validation was performed for this C# stage. Shader and mesh data remain unchanged.
## Signed Shader follow-up (2026-11-01)

Modified only `Shaders/TreeWind.hlsl` for deformation: Strength now uses clamp(-1,1), and only near-zero magnitude returns early. The same signed scalar multiplies trunk and branch angles, so negative Strength reverses the bend. BranchWeight still saturates to 0..1 as a membership/influence mask. WindManager C#, fixed destination=0, ASE graph connections, mesh and material assignments were not changed in this follow-up.

Validation: Unity's currently assigned experimental WindTest shader reported hasErrors=false, messageCount=0. In Play Mode, X-direction held Strength=0.6 visibly bent the trunk. Releasing wind and pausing at Strength=-0.15541546 showed the opposite bend in Scene and Game views, with the base appearing stationary and branches attached. Low 0.15 and strong 0.8 readbacks matched their held targets with velocity=0. Z-direction held 0.6 then released to -0.182179064; that negative pose was also visually inspected. After six more seconds, Strength and velocity returned to zero. These are limited current-lighting visual observations, not performance or all-angle quality measurements. Existing culling bounds and lighting limitations remain unassessed.

Exited Play Mode to restore original Strength=0, direction=(1,0,0); no scene or ASE graph save. Final Console: zero warnings/errors.
## Default direction and continuous Edit Mode follow-up (2026-11-01)

Modified WindManager.cs only. Default wind direction is (1,0,0), not strength 100. Zero, non-finite or effectively zero horizontal direction (including vertical-only input) falls back to this default. The public setter, OnValidate and the update path protect script, Inspector and direct-field inputs; invalid serialized direction is replaced with the default. A zero direction no longer stops wind. Set WindStrength=0 to stop it. During release the last active bend direction stays available.

SpringDestination remains constant zero. While wind is present its displacement is held with zero velocity. On release the same integration runs in Edit and Play Mode. Editor-only API references are behind UNITY_EDITOR, and the editor callback is subscribed/unsubscribed with enable/disable. Only changing response/direction requests scene/player-loop repaints; settled output does not force a constant repaint.

Validation: Unity finished C# compilation without errors. In Edit Mode, direct field direction=(0,0,0) restored (1,0,0) while strength stayed 0.6. Foreground editor-clock sampling showed release values 0.177665, -0.121071, -0.203642, then positive and negative decaying values; later strength and velocity both reached zero without entering Play Mode. An initial background/transition sampling attempt did not catch a negative value; the successful check used foreground Unity. Edit Mode Scene preview was visually checked at held 0.6 and after release/rest; no all-angle or lighting-quality claim is made. The small Game View was partly covered by the user's existing floating ASE window, so this follow-up does not claim an unobstructed Game View visual assessment.

Play Mode regression: zero direction restored +X, held strength=0.6 with zero velocity; release reached -0.17769611 and subsequently strength/velocity both zero. The editor callback is skipped during Play Mode by source inspection. Original input strength=0, direction=(1,0,0), Force=60, Damp=5 restored. No scene save, Shader/mesh/material changes, new package or performance test in this follow-up.