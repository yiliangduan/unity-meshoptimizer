using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class TexturePacker  {

    private List<TextureAtlas> mAtlasList = new List<TextureAtlas>();

    private int mLimitWidth;
    private int mLimitHeight;

    public void Pack(string textureDir, int limitWidth, int limitHeight)
    {
        if (!Directory.Exists(textureDir))
        {
            Debug.LogError("Directory is not exist!");
            return;
        }

        string[] files = Directory.GetFiles(textureDir, "*.png");

        List<Texture2D> textureList = new List<Texture2D>();

        for (int i=0; i< files.Length; ++i)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(files[i]);

            if (null != texture)
            {
                textureList.Add(texture);
            }
        }

        Pack(textureList, limitWidth, limitHeight);
    }

    public void Pack(List<Texture2D> textureList, int limitWidth, int limitHeight)
    {
        mAtlasList.Clear();

        mLimitWidth = limitWidth;
        mLimitHeight = limitHeight;

        FilterSrcTexture(textureList);

        int atlasIndex = 0;

        TextureAtlas atlas = new TextureAtlas(limitWidth, limitHeight, false, Application.dataPath+"Res/", atlasIndex);

        for (int i=0; i< textureList.Count; ++i)
        {
            Texture2D element = textureList[i];

            if (null != element)
            {
                if(!atlas.AddTexture(element))
                {
                    atlas.Pack();

                    atlas = new TextureAtlas(limitWidth, limitHeight, false, Application.dataPath + "Res/", atlasIndex);
                    mAtlasList.Add(atlas);
                }
            }
        }

        atlas.Pack();
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
