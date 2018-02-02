using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DensityFieldFilter : MonoBehaviour
{

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

    void RunFilter(Texture src, RenderTexture dst, int type)
    {
        FilterCS.SetTexture(kernelMC, "_densityTexture", src);
        FilterCS.SetTexture(kernelMC, "_densityTextureOutput", dst);
        FilterCS.SetInt("_morph", type);
        RenderTexture.active = dst;

        FilterCS.Dispatch(kernelMC, Output.Resolution / 8, Output.Resolution / 8, Output.Resolution / 8);

        RenderTexture.active = null;
    }

    [ContextMenu("Filter")]
    public void Filter(bool bypass, int erode, int dilate, float gaussian)
    {

        // if ( bypass )
        // {
        // 	 Output.DensityTexture = DensityTexture;
        // }

        // Try to remove outliers by eroding once 
        RunFilter(DensityTexture, FilteredTexture0, 0);
        RunFilter(FilteredTexture0, FilteredTexture1, 0);
        RunFilter(FilteredTexture1, FilteredTexture0, 1);
        RunFilter(FilteredTexture0, FilteredTexture1, 1);


        // Dilate 4 cells
        // for (int i = 0; i < dilate-1; i++)
        // {
        //     RunFilter(FilteredTexture1, FilteredTexture0, 0);
        //     RunFilter(FilteredTexture0, FilteredTexture1, 0);
        // }


        // // Dilate 4 cells
        // for (int i = 0; i < erode; i++)
        // {
        //     RunFilter(FilteredTexture1, FilteredTexture0, 1);
        //     RunFilter(FilteredTexture0, FilteredTexture1, 1);
        // }


        // // // Blur XYZ
        RunFilter(FilteredTexture1, FilteredTexture0, 2);
        RunFilter(FilteredTexture0, FilteredTexture1, 3);
        RunFilter(FilteredTexture1, FilteredTexture0, 4);

        // RunFilter(FilteredTexture0, FilteredTexture1, 2);
        // RunFilter(FilteredTexture1, FilteredTexture0, 3);
        // RunFilter(FilteredTexture0, FilteredTexture1, 4);

        // RunFilter(FilteredTexture1, FilteredTexture0, 2);
        // RunFilter(FilteredTexture0, FilteredTexture1, 3);
        // RunFilter(FilteredTexture1, FilteredTexture0, 4);

        Output.DensityTexture = FilteredTexture0;


    }
}
