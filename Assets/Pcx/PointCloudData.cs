// Pcx - Point cloud importer & renderer for Unity
// https://github.com/keijiro/Pcx

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.IO;

namespace Pcx 
{
    /// A container class optimized for compute buffer.
    [CreateAssetMenu(fileName = "Data", menuName = "Pcx/PCData", order = 1)]

    public sealed class PointCloudData : ScriptableObject
    {
        #region Public properties

        /// Byte size of the point element.
        public const int elementSize = sizeof(float) * 4;

        /// Number of points.
        public int pointCount {
            get 
            {
                if (_pointData == null)
                    return 0;
                else 
                    return _pointData.Length; 
            }
        }

        /// Get access to the compute buffer that contains the point cloud.
        public ComputeBuffer computeBuffer {
            get {
                if (_pointBuffer == null)
                {
                    _pointBuffer = new ComputeBuffer(pointCount, elementSize);
                    _pointBuffer.SetData(_pointData);
                }
                return _pointBuffer;
            }
        }

        #endregion

        #region ScriptableObject implementation

        ComputeBuffer _pointBuffer;


        void OnDisable()
        {
            if (_pointBuffer != null)
            {
                _pointBuffer.Release();
                _pointBuffer = null;
            }
        }

        #endregion

        #region Serialized data members

        [System.Serializable]
        public struct Point
        {
            public Vector3 position;
            public uint color;
        }

        Point[] _pointData;

        #endregion

        #region Editor functions

        #if UNITY_EDITOR

        static uint EncodeColor(Color c)
        {
            const float kMaxBrightness = 16;

            var y = Mathf.Max(Mathf.Max(c.r, c.g), c.b);
            y = Mathf.Clamp(Mathf.Ceil(y * 255 / kMaxBrightness), 1, 255);

            var rgb = new Vector3(c.r, c.g, c.b);
            rgb *= 255 * 255 / (y * kMaxBrightness);

            return ((uint)rgb.x      ) |
                   ((uint)rgb.y <<  8) |
                   ((uint)rgb.z << 16) |
                   ((uint)y     << 24);
        }

        public void Initialize(List<Vector3> positions, List<Color32> colors)
        {
            _pointData = new Point[positions.Count];
            for (var i = 0; i < _pointData.Length; i++)
            {
                _pointData[i] = new Point {
                    position = positions[i],
                    color = EncodeColor(colors[i])
                };
            }
            EditorUtility.SetDirty(this);
        }

        public void Load()
        {
            string filename = EditorUtility.OpenFilePanel("title", "./", "");

            Debug.LogFormat("Loading {0}", filename);

            List<Vector3> positions = new List<Vector3>();
            List<Color32> colors =  new List<Color32>();

            string[] allLines = File.ReadAllLines(filename);
            Debug.LogFormat("Loading {0} lines ", allLines.Length);

            for (int i = 0; i < allLines.Length; i++)
            {
                String[] substrings = allLines[i].Split(' ');
                positions.Add(new Vector3(
                    float.Parse(substrings[0]), 
                    float.Parse(substrings[1]),
                    float.Parse(substrings[2])));

                colors.Add(new Color32(255, 255, 255, 255));
            }
         
            Initialize(positions, colors);
        }


        #endif

        #endregion
    }
}
