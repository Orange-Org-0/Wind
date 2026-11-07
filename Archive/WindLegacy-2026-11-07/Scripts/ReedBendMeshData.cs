using UnityEngine;

namespace WindPrototype
{
    // Supplies geometry metadata only. Deformation stays on the GPU.
    [ExecuteAlways, DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class ReedBendMeshData : MonoBehaviour
    {
        static readonly int BoundsId = Shader.PropertyToID("_WindBendLocalBounds");
        MeshFilter meshFilter;
        MeshRenderer meshRenderer;
        MaterialPropertyBlock properties;

        void OnEnable() => RefreshMeshData();
        void OnValidate() => RefreshMeshData();

        [ContextMenu("Refresh Mesh Data")]
        public void RefreshMeshData()
        {
            if (!meshFilter) meshFilter = GetComponent<MeshFilter>();
            if (!meshRenderer) meshRenderer = GetComponent<MeshRenderer>();
            if (!meshFilter || !meshRenderer || !meshFilter.sharedMesh) return;
            if (properties == null) properties = new MaterialPropertyBlock();
            Bounds bounds = meshFilter.sharedMesh.bounds;
            meshRenderer.GetPropertyBlock(properties);
            properties.SetVector(BoundsId, new Vector4(bounds.min.y, bounds.size.y, 0, 0));
            meshRenderer.SetPropertyBlock(properties);

            // Conservative envelope for any horizontal bend up to 110 degrees.
            float reach = bounds.size.y + bounds.extents.x + bounds.extents.z;
            meshRenderer.localBounds = new Bounds(
                new Vector3(bounds.center.x, bounds.min.y, bounds.center.z),
                Vector3.one * (2f * reach));
        }

        void OnDisable()
        {
            if (!meshRenderer) return;
            if (properties != null)
            {
                meshRenderer.GetPropertyBlock(properties);
                properties.SetVector(BoundsId, Vector4.zero);
                meshRenderer.SetPropertyBlock(properties);
            }
            meshRenderer.ResetLocalBounds();
        }
    }
}