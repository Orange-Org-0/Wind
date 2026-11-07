#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace WindPrototype.Editor
{
    // Explicit, repeatable Play Mode integration check; never runs automatically.
    public static class WindBendBindingCheck
    {
        [Serializable] sealed class Sample
        {
            public string phase, state;
            public float angle, materialAngle;
            public Vector3 direction, materialDirection;
        }
        [Serializable] sealed class Report
        {
            public bool passed;
            public string message;
            public int checkedFrames, zeroCrossings;
            public List<Sample> samples = new List<Sample>();
        }

        static readonly int DirectionId = Shader.PropertyToID("_BendDir");
        static readonly int AngleId = Shader.PropertyToID("_BendStrength");
        static readonly int BoundsId = Shader.PropertyToID("_WindBendLocalBounds");
        static WindBendResponse response;
        static WindManager manager;
        static Renderer target;
        static Material material;
        static MaterialPropertyBlock block;
        static Vector3 savedDirection;
        static float savedStrength, savedTimeScale, duration, phaseStart, previousSigned;
        static bool savedBackground;
        static double deadline;
        static int phase;
        static Report report;

        [MenuItem("Tools/Wind/Check Material Binding And Recovery")]
        public static void Run()
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException("Enter Play Mode before running this check.");
            if (report != null) throw new InvalidOperationException("A check is already running.");
            response = Selection.activeGameObject
                ? Selection.activeGameObject.GetComponent<WindBendResponse>() : null;
            if (!response)
            {
                var candidates = UnityEngine.Object.FindObjectsByType<WindBendResponse>(
                    FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                if (candidates.Length > 0) response = candidates[0];
            }
            if (!response) throw new InvalidOperationException("No active WindBendResponse.");
            var settings = new SerializedObject(response);
            manager = settings.FindProperty("windSource").objectReferenceValue as WindManager;
            if (!manager) manager = WindManager.Active;
            if (!manager || !manager.isActiveAndEnabled)
                throw new InvalidOperationException("No active wind source.");
            duration = settings.FindProperty("recoveryDuration").floatValue;
            target = response.GetComponent<Renderer>();
            material = target.sharedMaterial;
            if (!material || !material.HasProperty(DirectionId) || !material.HasProperty(AngleId))
                throw new InvalidOperationException("Renamed shader properties are missing.");

            savedDirection = manager.WindDirection;
            savedStrength = manager.WindStrength;
            savedTimeScale = Time.timeScale;
            savedBackground = Application.runInBackground;
            Time.timeScale = 1f;
            Application.runInBackground = true;
            block = new MaterialPropertyBlock();
            report = new Report();
            phase = 0;
            phaseStart = Time.time;
            deadline = EditorApplication.timeSinceStartup + duration + 25.0;
            manager.WindDirection = Vector3.right;
            manager.WindStrength = settings.FindProperty("strengthForMaximum").floatValue;
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        static void Tick()
        {
            try
            {
                if (!response || !manager || !EditorApplication.isPlaying)
                    throw new Exception("Play Mode or a test object was interrupted.");
                if (EditorApplication.timeSinceStartup > deadline)
                    throw new Exception("Timed out waiting for Play Mode frames.");
                target.GetPropertyBlock(block);
                float received = block.GetFloat(AngleId);
                Vector4 receivedDirection = block.GetVector(DirectionId);
                if (Mathf.Abs(received - response.CurrentBendAngle) > 0.001f)
                    throw new Exception("Renderer angle does not match state machine output.");
                if ((new Vector3(receivedDirection.x, receivedDirection.y, receivedDirection.z)
                    - response.CurrentBendDirection).sqrMagnitude > 0.000001f)
                    throw new Exception("Renderer direction does not match state machine output.");
                if (block.GetVector(BoundsId).y <= 0f)
                    throw new Exception("Mesh bounds were lost from the PropertyBlock.");
                if (target.sharedMaterial != material)
                    throw new Exception("Shared material reference changed.");
                report.checkedFrames++;

                float elapsed = Time.time - phaseStart;
                if (phase == 0 && elapsed >= 1.5f)
                {
                    if (response.CurrentBendAngle <= 1f)
                        throw new Exception("Wind did not create a nonzero bend.");
                    Capture("wind-driven");
                    previousSigned = response.CurrentBendVector.x;
                    manager.WindStrength = 0f;
                    Next(1);
                }
                else if (phase == 1)
                {
                    float signed = response.CurrentBendVector.x;
                    if (signed * previousSigned < 0f)
                    {
                        report.zeroCrossings++;
                        Capture("recovery-crossing-" + report.zeroCrossings);
                    }
                    if (signed != 0f) previousSigned = signed;
                    if (elapsed >= duration + 0.15f)
                    {
                        Capture("settled");
                        if (response.CurrentState != WindBendState.WindDriven
                            || response.CurrentBendAngle > 0.001f || report.zeroCrossings < 4)
                            throw new Exception("Recovery did not oscillate and settle.");
                        manager.WindStrength = 1f;
                        Next(2);
                    }
                }
                else if (phase == 2 && elapsed >= 1f)
                {
                    Capture("wind-restarted");
                    if (response.CurrentState != WindBendState.WindDriven || response.CurrentBendAngle < 1f)
                        throw new Exception("Restarting wind did not drive the renderer.");
                    manager.WindStrength = 0f;
                    Next(3);
                }
                else if (phase == 3 && elapsed >= 0.3f)
                {
                    Capture("recovery-before-interruption");
                    if (response.CurrentState != WindBendState.Recovering)
                        throw new Exception("Second stop did not enter recovery.");
                    manager.WindDirection = Vector3.forward;
                    manager.WindStrength = 1f;
                    Next(4);
                }
                else if (phase == 4 && elapsed >= 0.5f)
                {
                    Capture("recovery-interrupted-new-direction");
                    if (response.CurrentState != WindBendState.WindDriven || response.CurrentBendVector.y <= 0f)
                        throw new Exception("Rewind interruption or new wind direction failed.");
                    manager.WindDirection = Vector3.zero;
                    Next(5);
                }
                else if (phase == 5 && elapsed >= 0.15f)
                {
                    Capture("zero-direction-recovery");
                    if (response.CurrentState != WindBendState.Recovering)
                        throw new Exception("Zero direction did not trigger recovery.");
                    Finish(true, "Renamed material properties receive live output; recovery, rest, restart, direction change and bounds preservation passed.");
                }
            }
            catch (Exception error) { Finish(false, error.Message); }
        }

        static void Next(int next) { phase = next; phaseStart = Time.time; }
        static void Capture(string label)
        {
            Vector4 d = block.GetVector(DirectionId);
            report.samples.Add(new Sample {
                phase = label, state = response.CurrentState.ToString(),
                angle = response.CurrentBendAngle, materialAngle = block.GetFloat(AngleId),
                direction = response.CurrentBendDirection,
                materialDirection = new Vector3(d.x, d.y, d.z)
            });
        }
        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode && report != null)
                Finish(false, "Play Mode exited before check completed.");
        }
        static void Finish(bool passed, string message)
        {
            EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            if (manager) { manager.WindDirection = savedDirection; manager.WindStrength = savedStrength; }
            Time.timeScale = savedTimeScale;
            Application.runInBackground = savedBackground;
            report.passed = passed;
            report.message = message;
            string path = Path.Combine(Application.dataPath, "Wind/Documentation/WindBindingTestResults.json");
            File.WriteAllText(path, JsonUtility.ToJson(report, true));
            Debug.Log("Wind binding check: " + (passed ? "PASS. " : "FAIL. ") + message);
            report = null;
        }
    }
}
#endif