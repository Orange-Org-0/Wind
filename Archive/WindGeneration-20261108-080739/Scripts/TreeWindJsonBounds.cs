using System;
using UnityEngine;

namespace WindPrototype.HierarchyExperiment
{
    /// <summary>Supplies the authored Pivot decoding bounds to an explicitly assigned material.</summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class TreeWindJsonBounds : MonoBehaviour
    {
        [Serializable]
        private sealed class Source
        {
            public Vector3[] vertices = Array.Empty<Vector3>();
        }

        private static readonly int BakeMinId = Shader.PropertyToID("_HWBakeMin");
        private static readonly int BakeSizeId = Shader.PropertyToID("_HWBakeSize");

        [Tooltip("The JSON used to author this mesh's vertex colors; contains vertices with x/y/z fields.")]
        [SerializeField] private TextAsset sourceJson;

        [Tooltip("Writes directly to this material. All renderers sharing it receive the same decoding bounds.")]
        [SerializeField] private Material targetMaterial;

        private void OnEnable()
        {
            if (sourceJson != null && targetMaterial != null)
                ApplyJsonBounds();
        }

        [ContextMenu("Apply JSON Bounds")]
        public void ApplyJsonBounds()
        {
            if (sourceJson == null || targetMaterial == null)
            {
                Debug.LogError("TreeWindJsonBounds: assign Source Json and Target Material first.", this);
                return;
            }

            if (!targetMaterial.HasProperty(BakeMinId) || !targetMaterial.HasProperty(BakeSizeId))
            {
                Debug.LogError("TreeWindJsonBounds: the material's shader must expose _HWBakeMin and _HWBakeSize as Vector properties.", this);
                return;
            }

            Source source;
            try
            {
                source = JsonUtility.FromJson<Source>(sourceJson.text);
            }
            catch (ArgumentException exception)
            {
                Debug.LogError("TreeWindJsonBounds: invalid JSON. " + exception.Message, this);
                return;
            }

            if (source == null || source.vertices == null || source.vertices.Length == 0)
            {
                Debug.LogError("TreeWindJsonBounds: JSON must contain a non-empty vertices array.", this);
                return;
            }

            var bounds = new Bounds(source.vertices[0], Vector3.zero);
            foreach (Vector3 vertex in source.vertices)
            {
                if (!IsFinite(vertex))
                {
                    Debug.LogError("TreeWindJsonBounds: JSON contains a non-finite vertex.", this);
                    return;
                }
                bounds.Encapsulate(vertex);
            }

            // Match HierarchyWindAssembler's normalization, including flat-axis protection.
            Vector3 size = bounds.size;
            size = new Vector3(Mathf.Max(size.x, 0.0001f),
                Mathf.Max(size.y, 0.0001f), Mathf.Max(size.z, 0.0001f));
            if (!IsFinite(bounds.min) || !IsFinite(size))
            {
                Debug.LogError("TreeWindJsonBounds: JSON bounds exceed the supported numeric range.", this);
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEditor.Undo.RecordObject(targetMaterial, "Apply TreeWind JSON Bounds");
#endif
            targetMaterial.SetVector(BakeMinId, bounds.min);
            targetMaterial.SetVector(BakeSizeId, size);
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEditor.EditorUtility.SetDirty(targetMaterial);
#endif
        }

        private static bool IsFinite(Vector3 value)
        {
            return !float.IsNaN(value.x) && !float.IsInfinity(value.x)
                && !float.IsNaN(value.y) && !float.IsInfinity(value.y)
                && !float.IsNaN(value.z) && !float.IsInfinity(value.z);
        }
    }
}
