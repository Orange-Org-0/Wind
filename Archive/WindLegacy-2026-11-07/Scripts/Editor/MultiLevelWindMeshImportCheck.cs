using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Wind.Editor
{
    public static class MultiLevelWindMeshImportCheck
    {
        public static void Run()
        {
            const string assetPath = "Assets/Wind/Models/MultiLevelWindTestTree.fbx";
            var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null)
                throw new System.InvalidOperationException("FBX ModelImporter was not found.");

            // Preserve the eight shared corners of each test box in Unity.
            // Hard face normals would force Unity to split the corners.
            importer.importNormals = ModelImporterNormals.Calculate;
            importer.normalSmoothingAngle = 180f;
            importer.importTangents = ModelImporterTangents.None;
            importer.keepQuads = true;
            importer.weldVertices = true;
            importer.SaveAndReimport();
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (root == null)
                throw new System.InvalidOperationException("FBX root was not imported.");

            var filters = root.GetComponentsInChildren<MeshFilter>(true);
            if (filters.Length != 1)
                throw new System.InvalidOperationException($"Expected one MeshFilter, got {filters.Length}.");

            var mesh = filters[0].sharedMesh;
            var attributes = mesh.GetVertexAttributes();
            var colors = new List<Color>();
            mesh.GetColors(colors);

            var uvCounts = new int[8];
            for (var channel = 0; channel < uvCounts.Length; channel++)
            {
                var values = new List<Vector4>();
                mesh.GetUVs(channel, values);
                uvCounts[channel] = values.Count;
            }

            Debug.Log(
                "WIND_MESH_IMPORT_CHECK " +
                $"vertexCount={mesh.vertexCount} subMeshes={mesh.subMeshCount} " +
                $"colors={colors.Count} colorAttribute={mesh.HasVertexAttribute(VertexAttribute.Color)} " +
                $"attributes=[{string.Join(", ", attributes.Select(a => a.attribute + ":" + a.dimension))}] " +
                $"uvCounts=[{string.Join(",", uvCounts)}]");
        }
    }
}
