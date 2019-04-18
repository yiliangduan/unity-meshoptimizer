using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Yiliang.Tools
{
    public class TexturePacker
    {
        /// <summary>
        /// 合并贴图
        /// </summary>
        /// <param name="textureDir">合并的贴图的目录</param>
        /// <param name="atlasWidth">atlas的最大宽度</param>
        /// <param name="atlasHeight">atlas的最大高度</param>
        /// <param name="allRealign">是否全部重新排列，如果false，则之前合并了的贴图不会改变其相对排列位置，但是当前目录已经删除了的图片会被删除</param>
        public static void Pack(string textureDir, int atlasWidth, int atlasHeight, bool allReLayout)
        {
            if (!Directory.Exists(textureDir))
            {
                Debug.LogError("Directory is not exist!");
                return;
            }

            string[] pngFiles = Directory.GetFiles(textureDir, "*.png");
            string[] jpgFiles = Directory.GetFiles(textureDir, "*.jpg");

            List<string> texFiles = new List<string>(pngFiles.Length + jpgFiles.Length);
            texFiles.AddRange(pngFiles);
            texFiles.AddRange(jpgFiles);

            List<Texture2D> textureList = new List<Texture2D>();

            for (int i = 0; i < texFiles.Count; ++i)
            {
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texFiles[i]);

                if (null != texture)
                {
                    textureList.Add(texture);
                }
            }

            string atlasName;
            string[] dirParts = textureDir.Split('/');
            if (dirParts.Length > 0)
            {
                atlasName = dirParts[dirParts.Length - 1].ToLower();
            }
            else
            {
                atlasName = string.Empty;
            }

            Pack(textureList, atlasName, atlasWidth, atlasHeight, allReLayout);
        }

        /// <summary>
        /// 合并贴图
        /// </summary>
        /// <param name="textureDir">合并的贴图的目录</param>
        /// param name="atlasName">制定的atlas的文件名</param>
        /// <param name="atlasWidth">atlas的最大宽度</param>
        /// <param name="atlasHeight">atlas的最大高度</param>
        /// <param name="allRealign">是否全部重新排列，如果false，则之前合并了的贴图不会改变其相对排列位置，但是当前目录已经删除了的图片会被删除</param>
        public static void Pack(List<Texture2D> textureList, string atlasName, int atlasWidth, int atlasHeight, bool allReLayout)
        {
            FilterDontPackTexture(textureList, atlasWidth, atlasHeight);

            SetTextureReadable(textureList);

            List<Texture2D> transparentTexList = new List<Texture2D>();
            List<Texture2D> opaqueTexList = new List<Texture2D>();

            for (int i = 0; i < textureList.Count; ++i)
            {
                Texture2D texture = textureList[i];
                if (texture.alphaIsTransparency)
                {
                    transparentTexList.Add(texture);
                }
                else
                {
                    opaqueTexList.Add(texture);
                }
            }

            PackAtlas(transparentTexList, atlasName, true, atlasWidth, atlasHeight);

            PackAtlas(opaqueTexList, atlasName, false, atlasWidth, atlasHeight);
        }

        /// <summary>
        /// 合并贴图
        /// </summary>
        /// <param name="textureList"></param>
        /// <param name="atlasName"></param>
        /// <param name="isTransparent"></param>
        /// <param name="atlasWidth"></param>
        /// <param name="atlasHeight"></param>
        /// <returns></returns>
        private static List<TextureAtlas> PackAtlas(List<Texture2D> textureList, string atlasName, bool isTransparent, int atlasWidth, int atlasHeight)
        {
            int atlasIndex = 0;
            int textureCount = textureList.Count;

            textureList.Sort((a, b) => { return a.width * a.height - b.width * b.height; });

            List<TextureAtlas> atlasList = new List<TextureAtlas>();

            string fileName = atlasName + "_" + atlasIndex;
            string assetPath = TextureAtlas.GetAssetPath(isTransparent, fileName);

            while (File.Exists(assetPath))
            {
                TextureAtlas atlas = AssetDatabase.LoadAssetAtPath<TextureAtlas>(assetPath);

                if (null != atlas)
                {
                    atlas.Layout();
                    atlasList.Add(atlas);
                }

                atlasIndex++;

                fileName = atlasName + "_" + atlasIndex;
                assetPath = TextureAtlas.GetAssetPath(isTransparent, fileName);
            }

            List<Texture2D> newlyTextures = new List<Texture2D>();

            //找出新增的图片
            for (int i=0; i<textureList.Count; ++i)
            {
                bool found = false;

                for (int j=0; j<atlasList.Count; ++j)
                {
                    TextureAtlas atlas = atlasList[j];

                    if (null != atlas)
                    {
                        TextureAtlasElement element = atlas.GetElement(textureList[i]);
                        if (null != element)
                        {
                            found = true;
                            break;
                        }
                    }
                }

                if (!found)
                    newlyTextures.Add(textureList[i]);
            }

            //Asset中删除图片已经删除的记录
            for (int i=0; i<atlasList.Count; ++i)
            {
                TextureAtlas atlas = atlasList[i];

                for (int j=atlas.ElementList.Count-1; j>=0; --j)
                {
                    Texture2D elementTex = atlas.ElementList[j].Tex;

                    bool found = false;
                    for (int m=0; m<textureList.Count; ++m)
                    {
                        if(elementTex == textureList[m])
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        atlas.RemoveElementAt(j);
                    }
                }
            }

            //排列新增的图片
            for (int i=0; i<newlyTextures.Count; ++i)
            {
                Texture2D newlyTexture = newlyTextures[i];

                if (null != newlyTexture)
                {
                    bool added = false;

                    for (int j=0; j<atlasList.Count; ++j)
                    {
                        if(atlasList[j].AddTexture(newlyTexture))
                        {
                            added = true;
                            break;
                        }
                    }

                    if (!added)
                    {
                        fileName = atlasName + "_" + atlasList.Count;
                        assetPath = TextureAtlas.GetAssetPath(isTransparent, fileName);

                        TextureAtlas atlas = ScriptableObject.CreateInstance<TextureAtlas>();
                        if (null != atlas)
                        {
                            atlas.Init(atlasWidth, atlasHeight, false, isTransparent, fileName);
                            atlas.AddTexture(newlyTexture);

                            atlasList.Add(atlas);
                        }
                        else
                        {
                            Debug.Log("Create atlas instance failed.");
                        }
                    }
                }
            }

            for (int i = 0; i < atlasList.Count; ++i)
            {
                EditorUtility.DisplayProgressBar("", "Pack atlas ", (i + 1) / atlasList.Count);

                atlasList[i].Pack();
            }

            EditorUtility.ClearProgressBar();

            return atlasList;
        }

        /// <summary>
        /// 过滤不需要合并的贴图
        /// </summary>
        private static void FilterDontPackTexture(List<Texture2D> textureList, int atlasWidth, int atlasHeight)
        {
            for (int i = textureList.Count - 1; i > 0; --i)
            {
                Texture2D texture = textureList[i];

                if (null == texture || texture.width > atlasWidth || texture.height > atlasHeight)
                {
                    textureList.RemoveAt(i);
                }
            }
        }

        public static void SetTextureReadable(List<Texture2D> textureList)
        {
            int textureCount = textureList.Count;

            for (int i=0; i<textureList.Count; ++i)
            {
                EditorUtility.DisplayProgressBar("", "Set texture readable. ", (float)i / textureCount);

                string path = AssetDatabase.GetAssetPath(textureList[i]);

                TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(path);

                if (!importer.isReadable)
                {
                    importer.isReadable = true;
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
            }

            EditorUtility.ClearProgressBar();
        }
    }
}