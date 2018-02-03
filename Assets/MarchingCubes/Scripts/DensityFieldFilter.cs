using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DensityFieldFilter : MonoBehaviour
{
	private enum FilterType
	{
		Dilate = 0,
		Erode = 1,
		GaussianX = 2,
		GaussianY = 3,
		GaussianZ = 4,
		Copy = 5
	}
    public Texture DensityTexture { get; set; }
    public RenderTexture FilteredTexture0 { get; set; }
    public RenderTexture FilteredTexture1 { get; set; }

    [SerializeField]
    public PavelKouril.MarchingCubesGPU.MarchingCubes Output;

    public ComputeShader FilterCS;
    private int kernelMC;
    public bool initialized = false;

    RenderTexture CreateTexture()
    {
        RenderTexture tex = new RenderTexture(
        Output.Resolution, Output.Resolution, Output.Resolution,
        RenderTextureFormat.RFloat, RenderTextureReadWrite.Default);

        tex.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        tex.volumeDepth = Output.Resolution;

        tex.enableRandomWrite = true;

        return tex;
    }
    void Start()
    {
        FilteredTexture0 = CreateTexture();
        FilteredTexture1 = CreateTexture();

        kernelMC = FilterCS.FindKernel("VolumeFilter");
        FilterCS.SetInt("_gridSize", Output.Resolution);

        initialized = true;
        Output.DensityTexture = FilteredTexture1;
    }

	RenderTexture m_lastBuffer;

    void RunFilter(FilterType mode)
    {
		Texture src;
		RenderTexture dst;

		 if (m_lastBuffer == null)
		 {
			src = DensityTexture;
			dst = FilteredTexture0;
		 }
		else // swap buffers
		{
			src = m_lastBuffer;
			dst = (m_lastBuffer == FilteredTexture0) ? FilteredTexture1 : FilteredTexture0;
		}

        FilterCS.SetTexture(kernelMC, "_densityTexture", src);
        FilterCS.SetTexture(kernelMC, "_densityTextureOutput", dst);
        FilterCS.SetInt("_morph", (int) mode);
        RenderTexture.active = dst;

        FilterCS.Dispatch(kernelMC, Output.Resolution / 8, Output.Resolution / 8, Output.Resolution / 8);

        RenderTexture.active = null;

		m_lastBuffer = dst;
    }

    [ContextMenu("Filter")]
    public void Filter( int erode, int dilate, float gaussian)
    {
		m_lastBuffer = null;

		RunFilter(FilterType.Copy);

        for (int i = 0; i < dilate; i++)
            RunFilter(FilterType.Dilate);


        for (int i = 0; i < erode; i++)
            RunFilter(FilterType.Erode);


        for (int i = 0; i < gaussian; i++)
        {
            RunFilter(FilterType.GaussianX);
            RunFilter(FilterType.GaussianY);
            RunFilter(FilterType.GaussianZ);
        }

        Output.DensityTexture = m_lastBuffer;
    }
}
