using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshCruncher : MonoBehaviour {


	[SerializeField]
	[Range(0,1)]
	public float ratio = 1f;


	public void Callback(int iteration, int originalTris, int currentTris, int targetTris)
	{
		Debug.LogFormat("{0}, {1}, {2}, {3}", iteration, originalTris, currentTris, targetTris);
	}

	public void Decimate () {
		var filter = GetComponent<MeshFilter>();

		
		filter.mesh = MeshDecimator.Unity.MeshDecimatorUtility.DecimateMesh(filter.mesh, Matrix4x4.identity, ratio, true, Callback);
	}
}
