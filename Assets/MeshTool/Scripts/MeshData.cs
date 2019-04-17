using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Yiliang.Tools
{
    /// <summary>
    /// 准备合并的Mesh的数据
    /// </summary>
    public class MeshData
    {
        public int lightmapIndex;

        public int[] triangles;

        public Vector2[] uv;

        //可选
        public Vector2[] uv2;

        public Vector3[] vertices;

        //可选
        public Color[] colors;

        //可选
        public Vector3[] normals;

        public Material material;

        public TexData texData;

        public Color materialColor;
    }

    public class MeshNode
    {
        public Material material;

        public Mesh mesh;
    }
}