// Pcx - Point cloud importer & renderer for Unity
// https://github.com/keijiro/Pcx

Shader "Point Cloud/Disk"
{
    Properties
    {
        _Tint("Tint", Color) = (0.5, 0.5, 0.5, 1)
        _PointSize("Point Size", Float) = 0.05
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        BlendOp Add
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex Vertex
            #pragma geometry Geometry
            #pragma fragment Fragment
            #pragma multi_compile_fog
            #pragma multi_compile _ UNITY_COLORSPACE_GAMMA
            #pragma multi_compile _ _COMPUTE_BUFFER
            #include "Disk.cginc"
            ENDCG
        }

    }
    CustomEditor "Pcx.DiskMaterialInspector"
}
