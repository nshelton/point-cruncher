using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PavelKouril.MarchingCubesGPU
{
    public class MarchingCubes : MonoBehaviour
    {
        [SerializeField]
        public int Resolution;
        public Material mat;
        public ComputeShader MarchingCubesCS;

        public RenderTexture DensityTexture { get; set; }

        private int kernelMC;

        private ComputeBuffer appendVertexBuffer;
        private ComputeBuffer argBuffer;

        public bool initialized = false;
        public bool doRender = true;

        [SerializeField]
        public Material exportMaterial;
        private void Awake()
        {
            kernelMC = MarchingCubesCS.FindKernel("MarchingCubes");
        }

        private void Start()
        {
            appendVertexBuffer = new ComputeBuffer(
                (Resolution - 1) * (Resolution - 1) * (Resolution - 1) * 5,
                 sizeof(float) * 18,
                 ComputeBufferType.Append);

            argBuffer = new ComputeBuffer(4, sizeof(int), ComputeBufferType.IndirectArguments);

            MarchingCubesCS.SetInt("_gridSize", Resolution);
            MarchingCubesCS.SetFloat("_isoLevel", 0.5f);

            MarchingCubesCS.SetBuffer(kernelMC, "triangleRW", appendVertexBuffer);
            initialized = true;
        }


        [ContextMenu("Mesh")]
        public void Mesh()
        {
            if ( DensityTexture == null)
                return;
    
            MarchingCubesCS.SetTexture(kernelMC, "_densityTexture", DensityTexture);
            appendVertexBuffer.SetCounterValue(0);

            MarchingCubesCS.Dispatch(kernelMC, Resolution / 8, Resolution / 8, Resolution / 8);

            int[] args = new int[] { 0, 1, 0, 0 };
            argBuffer.SetData(args);

            ComputeBuffer.CopyCount(appendVertexBuffer, argBuffer, 0);

            argBuffer.GetData(args);
            args[0] *= 3;
            argBuffer.SetData(args);

            Debug.Log("Vertex count:" + args[0]);
        }


            
        private void SplitMesh(GameObject parent, Vector3[] verts, int[] tris, Vector3[] normals)
        {
            int MAX_VERTS = 60000;
            int nVerts = verts.Length;
            int nMeshes = Mathf.CeilToInt((float)verts.Length / MAX_VERTS);
        
            int vertsSoFar = 0;

            for ( int i = 0; i < nMeshes; i ++)
            {
                int numSubmeshVerts = (nVerts - vertsSoFar) > MAX_VERTS ? MAX_VERTS : (nVerts - vertsSoFar);
 
                GameObject child = new GameObject();
                child.transform.SetParent(parent.transform, false);
                MeshFilter filter = child.AddComponent<MeshFilter>();
                MeshRenderer renderer = child.AddComponent<MeshRenderer>();
                renderer.material = exportMaterial;

                Vector3[] p_sub = new Vector3[numSubmeshVerts];
                int[] t_sub = new int[numSubmeshVerts];
                Vector3[] n_sub = new Vector3[numSubmeshVerts];
                
                for ( int v = 0; v < numSubmeshVerts; v++)
                {
                    p_sub[v] = verts[v + i * MAX_VERTS];
                    t_sub[v] = tris[v + i * MAX_VERTS];
                    n_sub[v] = normals[v + i * MAX_VERTS];
                }
                vertsSoFar += numSubmeshVerts;

                filter.mesh.vertices = p_sub;
                filter.mesh.triangles = t_sub;
                filter.mesh.normals = n_sub;
            }
        }

        public MarchingCubesController.Mesh ExportMesh()
        {   

            int[] args = new int[] { 0, 1, 0, 0 };
            argBuffer.SetData(args);
            ComputeBuffer.CopyCount(appendVertexBuffer, argBuffer, 0);
            argBuffer.GetData(args);
            int numTris = args[0];
            int numVerts = args[0] * 3;

            // 18 floats per Tri
            // x,y,z,nx,ny,nz
            // x,y,z,nx,ny,nz
            // x,y,z,nx,ny,nz

            float[] dataCPU = new float[numTris * 18];

            appendVertexBuffer.GetData(dataCPU);
            MarchingCubesController.Mesh outmesh = new MarchingCubesController.Mesh();
            outmesh.faces = new int[numVerts];
            outmesh.verts = new Vector3[numVerts];

            for ( int i = 0; i < numVerts; i ++)
            {
                outmesh.faces[i] = i;
            }
            
            int vertIndex = 0;

            for ( int i = 0; i < numTris; i ++)
            {
                int addr = i * 18;

                outmesh.verts[vertIndex++] = new Vector3(dataCPU[addr++], dataCPU[addr++], dataCPU[addr++]);
                Vector3 n0 = new Vector3(dataCPU[addr++], dataCPU[addr++], dataCPU[addr++]);
                
                outmesh.verts[vertIndex++] = new Vector3(dataCPU[addr++], dataCPU[addr++], dataCPU[addr++]);
                Vector3 n1 = new Vector3(dataCPU[addr++], dataCPU[addr++], dataCPU[addr++]);
                
                outmesh.verts[vertIndex++] = new Vector3(dataCPU[addr++], dataCPU[addr++], dataCPU[addr++]);
                Vector3 n2 = new Vector3(dataCPU[addr++], dataCPU[addr++], dataCPU[addr++]);
            }

            doRender = false;
            return outmesh;
        }

        public MeshDecimator.Mesh ExportMDMesh()
        {   

            int[] args = new int[] { 0, 1, 0, 0 };
            argBuffer.SetData(args);
            ComputeBuffer.CopyCount(appendVertexBuffer, argBuffer, 0);
            argBuffer.GetData(args);
            int numTris = args[0];
            int numVerts = args[0] * 3;

            // 18 floats per Tri
            // x,y,z,nx,ny,nz
            // x,y,z,nx,ny,nz
            // x,y,z,nx,ny,nz

            float[] dataCPU = new float[numTris * 18];

            appendVertexBuffer.GetData(dataCPU);
            var faces = new int[numVerts];
            var verts = new MeshDecimator.Math.Vector3d[numVerts];
            
            int vertIndex = 0;

            for ( int i = 0; i < numTris; i ++)
            {
                int addr = i * 18;

                verts[vertIndex++] = new MeshDecimator.Math.Vector3d(dataCPU[addr++], dataCPU[addr++], dataCPU[addr++]);
                addr+=3;
                
                verts[vertIndex++] = new MeshDecimator.Math.Vector3d(dataCPU[addr++], dataCPU[addr++], dataCPU[addr++]);
                addr+=3;
                
                verts[vertIndex++] = new MeshDecimator.Math.Vector3d(dataCPU[addr++], dataCPU[addr++], dataCPU[addr++]);
                addr+=3;
            }

            for ( int i = 0; i < numVerts; i ++)
            {
                faces[i] = i;
            }

            MeshDecimator.Mesh outmesh = new MeshDecimator.Mesh(verts, faces);

            doRender = false;
            return outmesh;
        }

        
        private void OnRenderObject()
        {
            if ( doRender)
            {
                mat.SetPass(0);
                mat.SetBuffer("triangles", appendVertexBuffer);
                mat.SetMatrix("model", transform.localToWorldMatrix);
                Graphics.DrawProceduralIndirect(MeshTopology.Triangles, argBuffer);
            }

        }

        private void OnDestroy()
        {
            appendVertexBuffer.Release();
            argBuffer.Release();
        }

        void OnDrawGizmos() {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, transform.lossyScale);
        }
    }
}