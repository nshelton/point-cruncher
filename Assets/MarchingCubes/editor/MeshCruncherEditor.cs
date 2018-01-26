using UnityEngine;
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(MeshCruncher))]
public sealed class MeshCruncherEditor : Editor
{
    public override void OnInspectorGUI()
    {
        MeshCruncher m_tgt = (MeshCruncher)target;
        DrawDefaultInspector ();

        int percent = (int) (m_tgt.ratio * 100f);
        if(GUILayout.Button(string.Format("Decimate {0}%", percent)))
        {
            m_tgt.Decimate();
        }
    }    
}
