using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace PavelKouril.MarchingCubesGPU
{
    public class DensityFieldGenerator : MonoBehaviour
    {
        [SerializeField]
        public int Resolution;

        [SerializeField]
        public Pcx.PointCloudRenderer m_points;

        [SerializeField]
        public bool m_FillOcclusions;

        [SerializeField]
        private DensityFieldFilter output;

        private Texture3D densityTexture;

        [SerializeField]
        public RenderTexture densityRenderTexture;

        [SerializeField]
        private ComputeShader m_sampleCS;

    	private int kernelID;
    	private int clearKernel;

        private Color[] colors;
        public bool initialized = false;

        private void Start()
        {
            densityRenderTexture = CreateTexture();
            kernelID = m_sampleCS.FindKernel("PointSampler");
            clearKernel = m_sampleCS.FindKernel("Clear");

            output = GetComponent<DensityFieldFilter>();

            densityTexture = new Texture3D((int)Resolution, (int)Resolution, (int)Resolution, TextureFormat.RFloat, false);
            densityTexture.wrapMode = TextureWrapMode.Clamp;

            colors = new Color[(int)Resolution * (int)Resolution * (int)Resolution];

            for (int i = 0; i < colors.Length; i++) 
                colors[i] = Color.black;
            
            initialized = true;
        }

        RenderTexture CreateTexture()
        {
            RenderTexture tex = new RenderTexture(
            Resolution, Resolution, Resolution,
            RenderTextureFormat.RFloat, RenderTextureReadWrite.Default);

            tex.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            tex.volumeDepth = Resolution;
            tex.enableRandomWrite = true;

            return tex;
        }

        public void CreateField()
        {
            RenderTexture.active = densityRenderTexture;

            m_sampleCS.SetTexture(clearKernel, "_outputTexture", densityRenderTexture);
            m_sampleCS.Dispatch(clearKernel, Resolution/8, Resolution/8, Resolution/8);
            
            m_sampleCS.SetTexture(kernelID, "_outputTexture", densityRenderTexture);
            m_sampleCS.SetBuffer(kernelID, "_inputData", m_points.sourceData.computeBuffer);
            m_sampleCS.SetInt("_gridSize", Resolution);

            m_sampleCS.SetMatrix("_pointsTransform", m_points.transform.localToWorldMatrix);
            m_sampleCS.SetMatrix("_volumeTransformInv", transform.worldToLocalMatrix);

            int numInvocations = Mathf.CeilToInt(m_points.sourceData.computeBuffer.count / 128f);
            m_sampleCS.Dispatch(kernelID, numInvocations, 1, 1);

            RenderTexture.active = null;
            output.DensityTexture = densityRenderTexture;
        }

        [ContextMenu("Mesh")]
        public void CreateFieldCPU()
        {
            Pcx.PointCloudData.Point[] data = new Pcx.PointCloudData.Point[m_points.sourceData.pointCount];
            m_points.sourceData.computeBuffer.GetData(data);

            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.black;
            }

            int idx = 0;
            for (int i = 0; i < data.Length; i++)
            {
                Vector3 p = data[i].position;

                p = m_points.transform.TransformPoint(p);
                p = transform.InverseTransformPoint(p);

                if (Mathf.Abs(p.x) > 0.5 ||
                    Mathf.Abs(p.y) > 0.5 ||
                    Mathf.Abs(p.z) > 0.5)
                    continue;

                p += Vector3.one * 0.5f;

                int x = Mathf.FloorToInt(p.x * Resolution);
                int y = Mathf.FloorToInt(p.y * Resolution);
                int z = Mathf.FloorToInt(p.z * Resolution);

                idx = x + (int)Resolution * (y + (int)Resolution * z);
                colors[idx].r++;

                if (m_FillOcclusions)
                {
                    for (int ii = y; ii > 0; ii--)
                    {
                        idx = x + (int)Resolution * (ii + (int)Resolution * z);
                        colors[idx].r++; ;
                    }
                }
            }

            densityTexture.SetPixels(colors);
            densityTexture.Apply();
            output.DensityTexture = densityTexture;
        }

        private void CreateCircleField()
        {
            var idx = 0;
            float sx, sy, sz;
            float resol = ((int)Resolution - 2) / 2 * Mathf.Sin(0.25f * Time.time);

            for (var z = 0; z < (int)Resolution; ++z)
            {
                for (var y = 0; y < (int)Resolution; ++y)
                {
                    for (var x = 0; x < (int)Resolution; ++x, ++idx)
                    {
                        sx = x - (int)Resolution / 2;
                        sy = y - (int)Resolution / 2;
                        sz = z - (int)Resolution / 2;

                        var amount = (sx * sx + sy * sy + sz * sz) <= resol * resol ? 1 : 0;

                        colors[idx].r = amount;
                    }
                }
            }

            densityTexture.SetPixels(colors);
            densityTexture.Apply();

            // output.DensityTexture = densityTexture;
        }
    }
}