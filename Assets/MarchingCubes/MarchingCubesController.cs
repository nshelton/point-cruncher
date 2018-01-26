using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Threading;
using System.Threading.Tasks;

public class MarchingCubesController : MonoBehaviour
{

    // public Queue<Action> JobQueue
    public struct Mesh
    {
        public Vector3[] verts;
        public int[] faces;
    }

    [SerializeField]
    public int m_gridDimXY;
    public int m_gridDimZ;

    [SerializeField]
    public Material m_meshMaterial;

    [SerializeField]
    public PavelKouril.MarchingCubesGPU.DensityFieldGenerator generator;

    [SerializeField]
    public DensityFieldFilter filter;

    [SerializeField]
    public PavelKouril.MarchingCubesGPU.MarchingCubes marchingCubes;

    private bool didRun = false;

    private GameObject AddMesh(UnityEngine.Mesh mesh, string name)
    {
        GameObject go = new GameObject();
        go.name = name;
        go.transform.SetParent(transform, false);
        go.transform.SetParent(null, true);

        var filter = go.AddComponent<MeshFilter>();

        filter.mesh = mesh;

        filter.mesh.RecalculateNormals();
        var renderer = go.AddComponent<MeshRenderer>();
        go.AddComponent<MeshCruncher>();

        renderer.material = m_meshMaterial;

        SaveMesh(filter.mesh, go, name);

        return go;
    }



    public void Callback(int iteration, int originalTris, int currentTris, int targetTris)
    {
        Debug.LogFormat("{0}, {1}, {2}, {3}", iteration, originalTris, currentTris, targetTris);
    }

    private UnityEngine.Mesh ConvertMesh(MeshDecimator.Mesh fatMesh)
    {
        // Merge Same Verts 
        Dictionary<MeshDecimator.Math.Vector3d, int> newVertIndexes = new Dictionary<MeshDecimator.Math.Vector3d, int>();
        List<MeshDecimator.Math.Vector3d> newVerts = new List<MeshDecimator.Math.Vector3d>();
        int[] newIndicies = new int[fatMesh.Indices.Length];

        for (int i = 0; i < fatMesh.Vertices.Length; i++)
        {
            if (!newVertIndexes.ContainsKey(fatMesh.Vertices[i]))
            {
                newVerts.Add(fatMesh.Vertices[i]);
                newVertIndexes[fatMesh.Vertices[i]] = newVerts.Count - 1;
            }
        }

        for (int i = 0; i < fatMesh.Indices.Length; i++)
        {
            MeshDecimator.Math.Vector3d v = fatMesh.Vertices[fatMesh.Indices[i]];
            newIndicies[i] = newVertIndexes[v];
        }

        MeshDecimator.Mesh mergedMesh = new MeshDecimator.Mesh(newVerts.ToArray(), newIndicies);

        UnityEngine.Mesh result = MeshDecimator.Unity.MeshDecimatorUtility.DecimateMeshBasic(mergedMesh, Matrix4x4.identity, 0.5f, true, Callback);

        result.RecalculateBounds();
        result.RecalculateNormals();

        // Vector2[] uv = Unwrapping.GeneratePerTriangleUV(result);
        // MeshUtility.SetPerTriangleUV2(result, uv);

        // Unwrapping.GenerateSecondaryUVSet(result);
        return result;
    }
    private UnityEngine.Mesh Reconstruct()
    {

        // (new Thread(() => {
        Debug.Log("Reconstruct   ");
        generator.CreateFieldCPU();
        Debug.Log("dONE cREATEfIELD");

        // })).Start(); 

        filter.Filter();
        Debug.Log("dONE Filter");

        marchingCubes.Mesh();
        Debug.Log("dONE mESHING");

        return ConvertMesh(marchingCubes.ExportMDMesh());
    }

    public static void SaveMesh(UnityEngine.Mesh mesh, GameObject obj, string name)
    {

        // string path = EditorUtility.SaveFilePanel("Save Separate Mesh Asset", "Assets/", name, "asset");
        // if (string.IsNullOrEmpty(path)) return;

        // path = FileUtil.GetProjectRelativePath(path);

        MeshUtility.Optimize(mesh);
        string fileLocation = "Assets/cache/meshes/" + name + ".prefab";
        AssetDatabase.CreateAsset(mesh, fileLocation);
        AssetDatabase.SaveAssets();

        string fileName = name;
        fileLocation = "Assets/cache/" + fileName + ".prefab";
        var emptyObj = PrefabUtility.CreateEmptyPrefab(fileLocation);

        PrefabUtility.ReplacePrefab(obj, emptyObj, ReplacePrefabOptions.ConnectToPrefab);
    }

    public void DoDaTing()
    {
        int dim = 1;
        Vector3 firstPos = marchingCubes.transform.position;
        for (int dz = 0; dz < 2; dz++)
            for (int dx = 0; dx < dim; dx++)
            {
                for (int dy = 0; dy < dim; dy++)
                {
                    var name = string.Format("{0}.{1}.{2}.{3}.{4}", System.DateTime.Now.Hour, System.DateTime.Now.Minute, dx, dy, dz);
                    Vector3 newPos = firstPos + new Vector3(dx, dz, dy) * transform.localScale.x * 0.9f;
                    marchingCubes.transform.position = newPos;

                    UnityEngine.Mesh rawMcMesh = Reconstruct();

                    AddMesh(rawMcMesh, name);
                }
            }
    }

    [ContextMenu("Run")]
    public void DoDaAsyncTing()
    {
        DoDaTing();
    }

    void Update()
    {
        if (!didRun && generator.initialized && filter.initialized && marchingCubes.initialized)
        {
            Debug.Log("loaded");
            didRun = true;
            DoDaTing();
        }
    }

    void OnDrawGizmos()
    {
        for (int z = 0; z < m_gridDimZ; z++)
        {
            for (int x = 0; x < m_gridDimXY; x++)
            {
                for (int y = 0; y < m_gridDimXY; y++)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireCube(
                        transform.position + transform.lossyScale.x * new Vector3(x, z, y),
                        transform.lossyScale);
                }
            }
        }

		Vector3 dim = new Vector3(m_gridDimXY, m_gridDimZ, m_gridDimXY);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(
            transform.position + (transform.lossyScale.x / 2) * (dim - Vector3.one),
            dim * transform.lossyScale.x);
    }
}
