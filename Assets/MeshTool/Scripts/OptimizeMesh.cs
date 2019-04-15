using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Elang.Tools
{
    /// <summary>
    /// 
    /// 使用优化Mesh的Material之前需要先合并Material的贴图.这一步是合并贴图之后替换Material使用的小图成图集，重写对于的UV操作
    /// 
    /// 1. 需要合并的MeshRenderer都使用了相同的Material，并且Material的属性相同
    /// 
    /// 2.需要合并的MeshRenderer使用了不同的Material，但是Shader相同并除了Shader所使用的Texture不同之外其他属性都相同。
    ///   这种情况先合并贴图，然后根据贴图再图集中的位置偏移重写MeshRenderer的UV，然后合并MeshRenderer
    /// </summary>
    public class OptimizeMesh : Editor
    {
        /// <summary>
        /// 准备合并的Mesh的数据
        /// </summary>
        public class MeshData
        {
            public int[] triangles;

            public Vector2[] uv;
            public Vector2[] uv2;

            public Vector3[] vertices;

            public Color[] colors;

            public Vector2[] normals;

            public Material material;

            public TexData texData;
        }

        public class TexData
        {
            public TextureAtlas Atlas;
            public TextureAtlasElement Element;
        }

        public static void CombineMesh()
        {
            GameObject activeGameObject = Selection.activeGameObject;

            if (null == activeGameObject)
            {
                Debug.LogError("Not select any object!");
                return;
            }

            List<MeshData> meshDataList = CollectMesh(activeGameObject);

            ReplaceTextureUseAtlas(meshDataList);

            DoCombine(meshDataList);
        }


        public static void DoCombine(List<MeshData> meshDataList)
        {

        }

        /// <summary>
        ///用Atlas替换掉Mesh使用的Material的小图
        /// </summary>
        /// <param name="meshDataList"></param>
        public static void ReplaceTextureUseAtlas(List<MeshData> meshDataList)
        {
            List<TextureAtlas> atlasAssetList = new List<TextureAtlas>();

            string[] assetFileArray = Directory.GetFiles(AtlasConfig.AssetDir, "*.asset", SearchOption.AllDirectories);
            for (int i=0; i<assetFileArray.Length; ++i)
            {
                TextureAtlas textureAtlasAsset = AssetDatabase.LoadAssetAtPath<TextureAtlas>(assetFileArray[i]);

                if (null != textureAtlasAsset)
                {
                    atlasAssetList.Add(textureAtlasAsset);
                }
            }

            for (int i=0; i< meshDataList.Count; ++i)
            {
                MeshData meshData = meshDataList[i];

                if (null != meshData)
                {
                    Texture2D texture = meshData.material.mainTexture as Texture2D;

                    for (int j=0; j< atlasAssetList.Count; ++j)
                    {
                        TextureAtlas textureAtlas = atlasAssetList[j];
                        if (null != textureAtlas)
                        {
                            TextureAtlasElement element = textureAtlas.GetElement(texture);

                            if (null != element)
                            {
                                meshData.texData = new TexData() { Atlas = textureAtlas, Element = element };
                            }
                            else
                            {
                                Debug.LogError("Texture not combine to atlas." + texture.name);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 收集Object所有的Mesh信息
        /// 注: SubMesh的Index和Material的Index是一致的。
        /// </summary>
        public static List<MeshData> CollectMesh(GameObject containMeshObject)
        {
            List<MeshData> meshDataList = new List<MeshData>();

            MeshRenderer[] meshRendererArray = containMeshObject.GetComponentsInChildren<MeshRenderer>();

            for (int i = 0; i < meshRendererArray.Length; ++i)
            {
                MeshRenderer meshRenderer = meshRendererArray[i];

                MeshFilter meshFilter = meshRenderer.GetComponent<MeshFilter>();
                if (null != meshFilter)
                {
                    Mesh mesh = meshFilter.mesh;

                    if (null != mesh)
                    {
                        if (mesh.subMeshCount == meshRenderer.sharedMaterials.Length)
                        {
                            for (int j = 0; j < mesh.subMeshCount; ++j)
                            {
                                MeshData meshData = GetMeshData(mesh, j);
                                meshData.material = meshRenderer.sharedMaterials[j];

                                meshDataList.Add(meshData);
                            }
                        }
                        else
                        {
                            Debug.Log("The number of sub mesh not equal to number of material. " + mesh.name);
                        }
                    }
                    else
                    {
                        Debug.LogError("Missing mesh file. " + meshFilter.name);
                    }
                }
                else
                {
                    Debug.LogError("Missing [MeshFilter] file. " + meshRenderer.gameObject.name);
                }
            }

            return meshDataList;
        }

        public static MeshData GetMeshData(Mesh mesh, int subMeshIndex)
        {
            //不重复的顶点索引
            List<int> subUniqueVertexIndexs = new List<int>();

            int[] triangles = mesh.GetTriangles(subMeshIndex);
            for (int i=0; i<triangles.Length; ++i)
            {
                if (!subUniqueVertexIndexs.Contains(triangles[i]))
                {
                    subUniqueVertexIndexs.Add(triangles[i]);
                }
            }

            //SubMesh的顶点
            List<Vector3> subVertices = new List<Vector3>();

            //SubMesh的UV
            List<Vector2> subUVs = new List<Vector2>();
            List<Vector2> subUV2s = new List<Vector2>();

            //SubMesh的颜色
            List<Color> subColors = new List<Color>();

            //法线
            List<Vector2> subNormals = new List<Vector2>();

            Vector3[] vertices = mesh.vertices;
            Vector2[] uvs = mesh.uv;
            Vector2[] uv2s = mesh.uv2;
            Color[] colors = mesh.colors;
            Vector3[] normals = mesh.normals;
            
            for (int i=0; i< subUniqueVertexIndexs.Count; ++i)
            {
                int vertexIndex = subUniqueVertexIndexs[i];
                if (vertexIndex >= 0 && vertexIndex < vertices.Length)
                {
                    subVertices.Add(vertices[vertexIndex]);
                }
                else
                {
                    Debug.LogError("Vertex index out of range. " + vertexIndex);
                }
     
    
                if (vertexIndex >= 0 && vertexIndex < uvs.Length)
                {
                    subUVs.Add(uvs[vertexIndex]);
                }
                else
                {
                    Debug.LogError("UV index out of range. " + vertexIndex);
                }

                //UV2没有是正常的
                if (null != uv2s && uv2s.Length > 0)
                {
                    if (vertexIndex >=0 && vertexIndex < uv2s.Length)
                    {
                        subUV2s.Add(uv2s[vertexIndex]);
                    }
                    else
                    {
                        Debug.LogError("UV2 index out of range. " + vertexIndex);
                    }
                }

                if (null != colors && colors.Length > 0)
                {
                    if (vertexIndex >= 0 && vertexIndex < colors.Length)
                    {
                        subColors.Add(colors[vertexIndex]);
                    }
                    else
                    {
                        Debug.LogError("Color index out of range. " + vertexIndex);
                    }
                }

                if (null != normals && normals.Length > 0)
                {
                    if (vertexIndex >= 0 && vertexIndex < normals.Length)
                    {
                        subNormals.Add(normals[vertexIndex]);
                    }
                    else
                    {
                        Debug.LogError("Normal index out of range. " + vertexIndex);
                    }
                }
            }

            MeshData meshData = new MeshData
            {
                triangles = mesh.GetTriangles(subMeshIndex),

                uv = subUVs.ToArray(),
                uv2 = subUV2s?.ToArray(),

                vertices = subVertices.ToArray(),

                colors = subColors?.ToArray(),

                normals = subNormals?.ToArray(),

            };

            return meshData;
        }
    }
}