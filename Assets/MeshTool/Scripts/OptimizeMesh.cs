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

            List<Dictionary<Material, List<MeshData>>> classifiedMeshDataList = ClassifyMeshData(meshDataList);

            List<MeshNode> combineMeshList = new List<MeshNode>();

            for (int i = 0; i < classifiedMeshDataList.Count; ++i)
            {
                Dictionary<Material, List<MeshData>>.Enumerator enumerator = classifiedMeshDataList[i].GetEnumerator();

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
                            combineMeshList.Add(new MeshNode{mesh=combineMesh, material=enumerator.Current.Key});
                        }
                        else
                        {
                            Debug.LogError("Combine mesh failed.");
                        }
                    }
                }
            }

            for (int i=0; i<combineMeshList.Count; ++i)
            {
                Mesh mesh = combineMeshList[i].mesh;
                Material material = combineMeshList[i].material;

                GameObject newObj = new GameObject
                {
                    name = mesh.name
                };

                newObj.transform.localScale = activeGameObject.transform.localScale;
                newObj.transform.localPosition = activeGameObject.transform.localPosition;
                newObj.transform.SetParent(activeGameObject.transform.parent);

                MeshRenderer meshRenderer = newObj.AddComponent<MeshRenderer>();
                if (null != meshRenderer)
                {
                    meshRenderer.material = material;
                }
                else
                {
                    Debug.Log("Add component to object failed.");
                }

                MeshFilter meshFilter = newObj.AddComponent<MeshFilter>();
                if (null != meshFilter)
                {
                    meshFilter.mesh = mesh;
                }
                else
                {
                    Debug.Log("Add component to object failed.");
                }
            }

            activeGameObject.gameObject.SetActive(false);
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

                int vertexLength = meshData.vertices.Length;

                vertexCount += vertexLength;
                triangleCount += meshData.triangles.Length;

                if (null == meshData.uv2 || meshData.uv2.Length != vertexLength)
                {
                    hasUV2Data = false;
                }

                if (null == meshData.normals || meshData.normals.Length != vertexLength)
                {
                    hasNormalData = false;
                }

                if (null == meshData.colors || meshData.colors.Length != vertexLength)
                {
                    hasColorData = false;
                }
            }


            Vector3[] vertices = new Vector3[vertexCount];
            int[] triangles = new int[triangleCount];

            Vector2[] uvs = new Vector2[vertexCount];
            Vector2[] uv2s = new Vector2[vertexCount];

            Color[] colors = new Color[vertexCount];

            Vector3[] normals = new Vector3[vertexCount];

            int vertexArrayIndex = 0;
            int triangleArrayIndex = 0;

            for (int i=0; i<meshDatas.Count; ++i)
            {
                MeshData meshData = meshDatas[i];

                if (null != meshData)
                {
                    meshData.vertices.CopyTo(vertices, vertexArrayIndex);

                    for (int j=0; j<meshData.triangles.Length; ++j)
                    {
                        triangles[triangleArrayIndex + j] = meshData.triangles[j] + vertexArrayIndex;
                    }

                    IntVector2 elementOffset = meshData.texData.Element.Offset;

                    TextureAtlas atlas = meshData.texData.Atlas;

                    int atlasWidth = atlas.Width;
                    int atlasHeight = atlas.Height;

                    Vector2 uvOffset = new Vector2(elementOffset.x/(float)atlasWidth, elementOffset.y/(float)atlasHeight);

                    float atlasRealWidth = atlas.Atlas.width;
                    float atlasRealHeight = atlas.Atlas.height;

                    Vector2 atlasScale = new Vector2(atlasRealWidth/ atlasWidth, atlasRealHeight/ atlasHeight);

                    IntVector2 texSize = meshData.texData.Element.Size;
                    Vector2 texLocalOffset = new Vector2(texSize.x/(float)atlasWidth, texSize.y/(float)atlasHeight);

                    for (int j=0; j<meshData.uv.Length; ++j)
                    {
                        uvs[vertexArrayIndex + j] = new Vector2(meshData.uv[j].x * texLocalOffset.x, meshData.uv[j].y * texLocalOffset.y) + 
                                                    new Vector2(uvOffset.x * atlasScale.x, uvOffset.y * atlasScale.y);
                    }

                    if (hasUV2Data)
                    {
                        meshData.uv2.CopyTo(uv2s, vertexArrayIndex);
                    }

                    if (hasColorData)
                    {
                        meshData.colors.CopyTo(colors, vertexArrayIndex);
                    }

                    if (hasNormalData)
                    {
                        meshData.normals.CopyTo(normals, vertexArrayIndex);
                    }

                    vertexArrayIndex += meshData.vertices.Length;
                    triangleArrayIndex += meshData.triangles.Length;
                }
            }

            combineMesh.vertices = vertices;
            combineMesh.SetTriangles(triangles, 0);
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
        public static List<Dictionary<Material, List<MeshData>>> ClassifyMeshData(List<MeshData> meshDataList)
        {
            List<Material> allMaterials = new List<Material>();

            if (Directory.Exists(MeshConfig.MaterialDir))
            {
                string[] materialFiles = Directory.GetFiles(MeshConfig.MaterialDir, "*.mat", SearchOption.AllDirectories);

                for (int i = 0; i < materialFiles.Length; ++i)
                {
                    allMaterials.Add(AssetDatabase.LoadAssetAtPath<Material>(materialFiles[i]));
                }
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

            List<Dictionary<Material, List<MeshData>>> meshNodeList = new List<Dictionary<Material, List<MeshData>>>();

            Dictionary<int, List<MeshData>>.Enumerator enumerator = meshDataDict.GetEnumerator();
            while (enumerator.MoveNext())
            {
                //相同的lightmapIndex下，根据material区分
                Dictionary<Material, List<MeshData>> meshNodeDict = new Dictionary<Material, List<MeshData>>();

                for (int i=0; i<enumerator.Current.Value.Count; ++i)
                {
                    MeshData meshData = enumerator.Current.Value[i];

                    Material newMaterial = GeneratorCombineMaterial(meshData, allMaterials);

                    if (null != newMaterial)
                    {
                        meshData.material = newMaterial;

                        List<MeshData> meshNodeElement;
                        if (!meshNodeDict.TryGetValue(newMaterial, out meshNodeElement))
                        {
                            meshNodeElement = new List<MeshData>();
                            meshNodeDict.Add(newMaterial, meshNodeElement);
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
            Material outMaterial = new Material(meshData.material);

            Texture2D atlas = meshData.texData.Atlas.Atlas;
            Vector2 atlasSize = new Vector2(meshData.texData.Atlas.Width, meshData.texData.Atlas.Height);

            outMaterial.mainTexture = atlas;
            outMaterial.mainTextureOffset = Vector2.zero;
            outMaterial.mainTextureScale = new Vector2(atlas.width / atlasSize.x, atlas.height / atlasSize.y);

            materials.Add(outMaterial);

            bool foundSameMaterial = false;
            for (int i = 0; i < materials.Count; ++i)
            {
                if (MaterialTool.Compare(outMaterial, materials[i]))
                {
                    outMaterial = materials[i];
                    foundSameMaterial = true;
                    break;
                }
            }

            if (!foundSameMaterial)
            {
                AssetDatabase.CreateAsset(outMaterial, GeneratorMaterialPath(meshData));
            }

            return outMaterial;
        }

        private static string GeneratorMaterialPath(MeshData meshData)
        {
            int sameNameMatIndex = 0;

            string texName = meshData.texData.Atlas.name + "_" + meshData.material.shader.name;
            texName = texName.ToLower();

            string materialPath = MeshConfig.MaterialDir + texName + "_" + sameNameMatIndex + ".mat";

            if (!Directory.Exists(MeshConfig.MaterialDir))
            {
                Directory.CreateDirectory(MeshConfig.MaterialDir);
            }

            while (File.Exists(materialPath))
            {
                sameNameMatIndex++;
                materialPath = MeshConfig.MaterialDir + texName + "_" + sameNameMatIndex + ".mat";
            }

            return materialPath;
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
                                break;
                            }
                        }
                    }

                    if (null == meshData.texData)
                    {
                        Debug.LogError("Texture not combine to atlas. " + texture.name);
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
                    Mesh mesh = meshFilter.sharedMesh;

                    if (null != mesh)
                    {
                        if (mesh.subMeshCount == meshRenderer.sharedMaterials.Length)
                        {
                            int lightmapIndex = meshRenderer.lightmapIndex;

                            for (int j = 0; j < mesh.subMeshCount; ++j)
                            {
                                Material material = meshRenderer.sharedMaterials[j];

                                MeshData meshData = GetSubMeshData(meshFilter, j);
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
        public static MeshData GetSubMeshData(MeshFilter meshFilter, int subMeshIndex)
        {
            Mesh mesh = meshFilter.sharedMesh;

            if (null == mesh)
            {
                Debug.LogError("Mesh missing.");
                return null;
            }

            int[] triangles = mesh.GetTriangles(subMeshIndex);

            Vector3[] vertices = mesh.vertices;

            Matrix4x4 matrix4x4 = meshFilter.gameObject.transform.localToWorldMatrix;

            for (int i=0; i< vertices.Length; ++i)
            {
                ////转换到世界坐标。因为涉及到多个Mesh合并，每个Mesh的vertices都是自己的标准化坐标，所以转到到世界坐标
                vertices[i] = matrix4x4.MultiplyPoint(vertices[i]);
            }

            MeshData meshData = new MeshData
            {
                triangles = triangles,

                uv = mesh.uv,
                uv2 = mesh.uv2,

                vertices = vertices,

                colors = mesh.colors,

                normals = mesh.normals,
            };

            return meshData;
        }
    }
}