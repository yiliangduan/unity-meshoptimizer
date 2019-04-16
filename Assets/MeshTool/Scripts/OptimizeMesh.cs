using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Yiliang.Tools
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

            List<Dictionary<string, List<MeshData>>> classifiedMeshDataList = ClassifyMeshData(meshDataList);

            List<Mesh> combineMeshList = new List<Mesh>();

            for (int i = 0; i < classifiedMeshDataList.Count; ++i)
            {
                Dictionary<string, List<MeshData>>.Enumerator enumerator = classifiedMeshDataList[i].GetEnumerator();

                while (enumerator.MoveNext())
                {
                    List<MeshData> meshDatas = enumerator.Current.Value;

                    if (meshDatas.Count > 0)
                    {
                        bool hasLightmap = meshDatas[0].lightmapIndex >= 0;

                        string newMeshName = "combine_node_" + (hasLightmap?"_lightmap_" : "_no_lightmap_") + i;
                        Mesh combineMesh = DoCombine(meshDatas, newMeshName);
                        if (null != combineMesh)
                        {
                            combineMeshList.Add(combineMesh);
                        }
                        else
                        {
                            Debug.LogError("Combine mesh failed.");
                        }
                    }
                }
            }
        }

        public static Mesh DoCombine(List<MeshData> meshDatas, string newMeshName)
        {
            Mesh combineMesh = new Mesh
            {
                name = newMeshName
            };

            int vertexCount = 0;
            int triangleCount = 0;

            //只要有一个元素没有值，则设为没有值
            bool hasUV2Data = true;
            bool hasNormalData = true;
            bool hasColorData = true;

            for (int i=0; i<meshDatas.Count; ++i)
            {
                MeshData meshData = meshDatas[i];

                vertexCount += meshData.vertices.Length;
                triangleCount += meshData.triangles.Length;

                if (null == meshData.uv2)
                {
                    hasUV2Data = false;
                }

                if (null == meshData.normals)
                {
                    hasNormalData = false;
                }

                if (null == meshData.colors)
                {
                    hasColorData = false;
                }
            }

            Vector3[] vertices = new Vector3[vertexCount];
            int[] triangles = new int[triangleCount];

            Vector2[] uvs = new Vector2[vertexCount];
            Vector2[] uv2s = new Vector2[vertexCount];

            Color[] colors = new Color[vertexCount];

            Vector2[] normals = new Vector2[vertexCount];

            int vertexArrayIndex = 0;
            int triangleArrayIndex = 0;

            for (int i=0; i<meshDatas.Count; ++i)
            {
                MeshData meshData = meshDatas[i];

                if (null != meshData)
                {
                    vertices.CopyTo(meshData.vertices, vertexArrayIndex);
                    
                    triangles.CopyTo(meshData.triangles, triangleArrayIndex);
                    triangleArrayIndex += meshData.triangles.Length;

                    uvs.CopyTo(meshData.uv, vertexArrayIndex);

                    if (hasUV2Data)
                    {
                        uv2s.CopyTo(meshData.uv2, vertexArrayIndex);
                    }

                    if (hasColorData)
                    {
                        colors.CopyTo(meshData.colors, vertexArrayIndex);
                    }

                    if (hasNormalData)
                    {
                        colors.CopyTo(meshData.normals, vertexArrayIndex);
                    }

                    vertexArrayIndex += meshData.vertices.Length;
                }
            }

            combineMesh.vertices = vertices;
            combineMesh.triangles = triangles;
            combineMesh.uv = uvs;
            combineMesh.uv2 = uv2s;
            combineMesh.colors = colors;
            combineMesh.RecalculateNormals();// FIXME

            return combineMesh;
        }

        /// <summary>
        /// 归类
        /// </summary>
        /// <param name="meshDataList"></param>
        public static List<Dictionary<string, List<MeshData>>> ClassifyMeshData(List<MeshData> meshDataList)
        {
            string[] materialFiles = Directory.GetFiles(MeshConfig.MaterialDir, "*.mat", SearchOption.AllDirectories);
            List<Material> allMaterials = new List<Material>();
            for (int i=0; i< materialFiles.Length; ++i)
            {
                allMaterials.Add(AssetDatabase.LoadAssetAtPath <Material> (materialFiles[i]));
            }

            //根据lightmapIndex区分
            Dictionary<int, List<MeshData>> meshDataDict = new Dictionary<int, List<MeshData>>();

            for (int i=0; i<meshDataList.Count; ++i)
            {
                MeshData meshData = meshDataList[i];

                List<MeshData> meshDataElement;
                if(!meshDataDict.TryGetValue(meshData.lightmapIndex, out meshDataElement))
                {
                    meshDataElement = new List<MeshData>();
                    meshDataDict.Add(meshData.lightmapIndex, meshDataElement);
                }

                meshDataElement.Add(meshData);
            }

            List<Dictionary<string, List<MeshData>>> meshNodeList = new List<Dictionary<string, List<MeshData>>>();

            Dictionary<int, List<MeshData>>.Enumerator enumerator = meshDataDict.GetEnumerator();
            while (enumerator.MoveNext())
            {
                //相同的lightmapIndex下，根据material区分
                Dictionary<string, List<MeshData>> meshNodeDict = new Dictionary<string, List<MeshData>>();

                for (int i=0; i<enumerator.Current.Value.Count; ++i)
                {
                    MeshData meshData = enumerator.Current.Value[i];

                    Material newMaterial = GeneratorCombineMaterial(meshData, allMaterials);

                    if (null != newMaterial)
                    {
                        meshData.material = newMaterial;

                        List<MeshData> meshNodeElement;
                        if (!meshNodeDict.TryGetValue(newMaterial.name, out meshNodeElement))
                        {
                            meshNodeElement = new List<MeshData>();
                            meshNodeDict.Add(newMaterial.name, meshNodeElement);
                        }

                        meshNodeElement.Add(meshData);
                    }
                    else
                    {
                        Debug.LogError("Generator material failed."+meshData.material.name);
                    }
                }

                meshNodeList.Add(meshNodeDict);
            }

            return meshNodeList;
        }

        /// <summary>
        /// 生成合并之后的Material
        /// </summary>
        /// <param name="meshData"></param>
        /// <param name="materials"></param>
        /// <returns></returns>
        public static Material GeneratorCombineMaterial(MeshData meshData, List<Material> materials)
        {
            Material outMaterial = null;
            Material inMaterial = meshData.material;

            for (int i=0; i<materials.Count; ++i)
            {
                if (MaterialTool.CompareWithoutColor(inMaterial, materials[i]))
                {
                    outMaterial = materials[i];
                    break;
                }
            }

            if (null == outMaterial)
            {
                int sameNameMatIndex = 0;

                string texName = meshData.texData.Atlas.name + meshData.material.shader.name;
                texName = texName.ToLower();

                string materialPath = MeshConfig.MaterialDir + texName + "_" + sameNameMatIndex + ".mat";

                if (!Directory.Exists(MeshConfig.MaterialDir))
                {
                    Directory.CreateDirectory(MeshConfig.MaterialDir);
                }

                while (!File.Exists(materialPath))
                {
                    sameNameMatIndex++;
                    materialPath = MeshConfig.MaterialDir + texName + "_" + sameNameMatIndex + ".mat";
                }

                outMaterial = new Material(meshData.material);

                Texture2D atlas = meshData.texData.Atlas.Atlas;
                Vector2 atlasSize = new Vector2(meshData.texData.Atlas.Width, meshData.texData.Atlas.Height);

                outMaterial.mainTexture = atlas;
                outMaterial.mainTextureOffset = Vector2.zero;
                outMaterial.mainTextureScale = new Vector2(atlas.width/atlasSize.x, atlas.height/atlasSize.y);

                AssetDatabase.CreateAsset(outMaterial, materialPath);
            }

            return outMaterial;
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
                            int lightmapIndex = meshRenderer.lightmapIndex;

                            for (int j = 0; j < mesh.subMeshCount; ++j)
                            {
                                Material material = meshRenderer.sharedMaterials[j];

                                MeshData meshData = GetSubMeshData(mesh, j);
                                meshData.material = material;
                                meshData.lightmapIndex = lightmapIndex;
                                meshData.materialColor = material.color;
                          
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

        /// <summary>
        /// 获取SubMesh的数据
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="subMeshIndex"></param>
        /// <returns></returns>
        public static MeshData GetSubMeshData(Mesh mesh, int subMeshIndex)
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