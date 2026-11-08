using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace WindPrototype.HierarchyExperiment.Editor
{
    public static class HierarchyWindAssembler
    {
        [Serializable] sealed class Record { public Vector3 pivot; public Vector3 tip; public float weight; }
        [Serializable] sealed class Source { public Vector3[] vertices; public int[] triangles; public Record[] records; }

        static string Root
        {
            get
            {
                string[] guids = AssetDatabase.FindAssets("HierarchyWindAssembler t:MonoScript");
                if (guids.Length != 1) throw new InvalidOperationException("Expected one HierarchyWindAssembler script.");
                return Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(
                    AssetDatabase.GUIDToAssetPath(guids[0])))).Replace('\\','/');
            }
        }

        [MenuItem("Tools/Wind/Generate Tree Mesh And Data")]
        public static void Generate()
        {
            string root = Root;
            // Store generated assets alongside their corresponding asset types.
            foreach (string folder in new[] { "Models", "Textures" })
                if (!AssetDatabase.IsValidFolder(root + "/" + folder)) AssetDatabase.CreateFolder(root, folder);
            Source source = JsonUtility.FromJson<Source>(File.ReadAllText(root+"/Models/HierarchyWindTree.json"));
            if (source.vertices == null || source.records == null || source.vertices.Length != source.records.Length)
                throw new InvalidDataException("Missing or inconsistent vertex records.");
            Bounds bakeBounds = new Bounds(source.vertices[0],Vector3.zero);
            foreach (Vector3 vertex in source.vertices) bakeBounds.Encapsulate(vertex);
            Vector3 bakeSize = bakeBounds.size;
            bakeSize = new Vector3(Mathf.Max(bakeSize.x,0.0001f),Mathf.Max(bakeSize.y,0.0001f),Mathf.Max(bakeSize.z,0.0001f));
            string meshPath = root+"/Models/HierarchyWindTree.asset";
            string texturePath = root+"/Textures/HierarchyWindData.asset";
            string materialPath = root+"/Materials/HierarchyWind.mat";
            // Preserve existing generated assets/references; do not silently overwrite artist edits.
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (!mesh)
            {
                mesh = new Mesh { name = "HierarchyWindTree_" + source.vertices.Length + "Vertices" };
                mesh.vertices = source.vertices;
                mesh.triangles = source.triangles;
                var colors = new Color[source.vertices.Length];
                for (int i=0;i<colors.Length;i++)
                {
                    Vector3 p = source.records[i].pivot-bakeBounds.min;
                    colors[i]=new Color(p.x/bakeSize.x,p.y/bakeSize.y,p.z/bakeSize.z,source.records[i].weight);
                }
                mesh.colors=colors;
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                AssetDatabase.CreateAsset(mesh,meshPath);
            }
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (!texture)
            {
                texture = new Texture2D(1,source.records.Length,TextureFormat.RGBAHalf,false,true)
                    { name="HierarchyWindData", filterMode=FilterMode.Point, wrapMode=TextureWrapMode.Clamp, anisoLevel=0 };
                for (int i=0;i<source.records.Length;i++)
                {
                    Record r=source.records[i];
                    Vector3 tip = r.tip - bakeBounds.min;
                    texture.SetPixel(0,i,new Color(tip.x/bakeSize.x,tip.y/bakeSize.y,tip.z/bakeSize.z,0));
                }
                texture.Apply(false,false);
                AssetDatabase.CreateAsset(texture,texturePath);
            }
            // Main-line data only. Do not create the retired demo material, prefab or scene objects.
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(mesh);
        }
    }
}