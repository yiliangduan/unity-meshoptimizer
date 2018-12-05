using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class TexturePacker  {

    private int mLimitWidth;
    private int mLimitHeight;

    public void Pack(string textureDir, int limitWidth, int limitHeight)
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

        Pack(textureList, atlasName, limitWidth, limitHeight);
    }

    public void Pack(List<Texture2D> textureList, string atlasName, int limitWidth, int limitHeight)
    {
        mLimitWidth = limitWidth;
        mLimitHeight = limitHeight;

        FilterSrcTexture(textureList);

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

        PackAtlas(transparentTexList, atlasName, true);

        PackAtlas(opaqueTexList, atlasName, false);
    }

    private List<TextureAtlas> PackAtlas(List<Texture2D> textureList, string atlasName, bool isTransparent)
    {
        int atlasIndex = 0;
        int textureCount = textureList.Count;

        textureList.Sort((a, b)=> { return a.width * a.height - b.width * b.height;});

        List<TextureAtlas> atlasList = new List<TextureAtlas>();

        while (textureList.Count > 0)
        {
            EditorUtility.DisplayProgressBar("", "Layout all texture to atlas ", (atlasIndex + 1) / textureCount);

            TextureAtlas atlas = ScriptableObject.CreateInstance<TextureAtlas>();
            atlas.Init(mLimitWidth, mLimitHeight, false, isTransparent, atlasName+"_"+ atlasIndex);

            for (int i = textureList.Count - 1; i >= 0; --i)
            {
                if (atlas.AddTexture(textureList[i]))
                {
                    textureList.RemoveAt(i);
                }
            }

            atlasList.Add(atlas);
            atlasIndex++;
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
    /// 过滤Size比Atlas的Size还要大的Texture
    /// </summary>
    private void FilterSrcTexture(List<Texture2D> textureList)
    {
        for (int i = textureList.Count-1; i > 0; --i)
        {
            Texture2D texture = textureList[i];

            if (null == texture || texture.width > mLimitWidth || texture.height > mLimitHeight)
            {
                textureList.RemoveAt(i);
            }
        }
    }
}
