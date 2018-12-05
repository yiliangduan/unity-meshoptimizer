using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class TexturePacker  {

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

        List<string> texFiles = new List<string>(pngFiles.Length+jpgFiles.Length);
        texFiles.AddRange(pngFiles);
        texFiles.AddRange(jpgFiles);

        List<Texture2D> textureList = new List<Texture2D>();

        for (int i=0; i< texFiles.Count; ++i)
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
            atlasName = dirParts[dirParts.Length-1].ToLower();
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

        List<Texture2D> transparentTexList = new List<Texture2D>();
        List<Texture2D> opaqueTexList = new List<Texture2D>();

        for (int i=0; i<textureList.Count; ++i)
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

        textureList.Sort((a, b)=> { return a.width * a.height - b.width * b.height;});

        List<TextureAtlas> atlasList = new List<TextureAtlas>();

        string fileName = string.Empty;

        while (textureList.Count > 0)
        {
            EditorUtility.DisplayProgressBar("", "Layout all texture to atlas ", (atlasIndex + 1) / textureCount);

            fileName = atlasName + "_" + atlasIndex;

            string assetPath = TextureAtlas.GetAssetPath(isTransparent, fileName);

            TextureAtlas atlas;

            if (File.Exists(assetPath))
            {
                atlas = AssetDatabase.LoadAssetAtPath<TextureAtlas>(assetPath);

                if (null == atlas)
                {
                    Debug.LogError("Load asset failed. " + assetPath);
                    return null;
                }

                atlas.Layout();

                //如果文件夹下的图片已经删除了，则把Asset中保存的该文件的信息也删除
                for (int i= atlas.ElementList.Count-1; i>0; --i)
                {
                    bool isFound = false;

                    for (int j=0; j< textureList.Count; ++j)
                    {
                        if (atlas.ElementList[i].Tex == textureList[j])
                        {
                            isFound = true;
                            break;
                        }
                    }

                    if (!isFound)
                    {
                        atlas.RemoveElementAt(i);
                    }
                }
            }
            else
            {
                atlas = ScriptableObject.CreateInstance<TextureAtlas>();
                atlas.Init(atlasWidth, atlasHeight, false, isTransparent, fileName);
            }

            atlasList.Add(atlas);
            atlasIndex++;

            //新增图片
            for (int i = textureList.Count - 1; i >= 0; --i)
            {
                TextureAtlasElement element = atlas.GetElement(textureList[i]);

                if (null != element)
                {
                    textureList.RemoveAt(i);
                }
                else
                { 
                    if (atlas.AddTexture(textureList[i]))
                    {
                        textureList.RemoveAt(i);
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
        for (int i = textureList.Count-1; i > 0; --i)
        {
            Texture2D texture = textureList[i];

            if (null == texture || texture.width > atlasWidth || texture.height > atlasHeight)
            {
                textureList.RemoveAt(i);
            }
        }
    }
}
