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
        public class CombineMeshData
        {
            public int[] Triangles;
            public Material Mat;
        }

        public static void CombineMesh()
        {
            GameObject activeGameObject = Selection.activeGameObject;

            if (null == activeGameObject)
            {
                Debug.LogError("Not select any object!");
                return;
            }

            List<CombineMeshData> combineMeshDataList = CollectMesh(activeGameObject);

            ReplaceTextureUseAtlas(combineMeshDataList);
        }

        /// <summary>
        ///用Atlas替换掉Mesh使用的Material的小图
        /// </summary>
        /// <param name="combineMeshDataList"></param>
        public static void ReplaceTextureUseAtlas(List<CombineMeshData> combineMeshDataList)
        {
            string[] pngFileArray = Directory.GetFiles(AtlasConfig.AtlasDir, "*.png", SearchOption.AllDirectories);
            string[] jpgFileArray = Directory.GetFiles(AtlasConfig.AtlasDir, "*.jpg", SearchOption.AllDirectories);

            List<string> texFileList = new List<string>(pngFileArray.Length + jpgFileArray.Length);
            texFileList.AddRange(pngFileArray);
            texFileList.AddRange(jpgFileArray);

            //TODO

        }

        /// <summary>
        /// 收集Object所有的Mesh信息
        /// 注: SubMesh的Index和Material的Index是一致的。
        /// </summary>
        public static List<CombineMeshData> CollectMesh(GameObject containMeshObject)
        {
            List<CombineMeshData> meshDataList = new List<CombineMeshData>();

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
                                int[] triangles = mesh.GetTriangles(j);

                                CombineMeshData meshData = new CombineMeshData
                                {
                                    Triangles = triangles,
                                    Mat = meshRenderer.sharedMaterials[j],
                                };

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
    }
}