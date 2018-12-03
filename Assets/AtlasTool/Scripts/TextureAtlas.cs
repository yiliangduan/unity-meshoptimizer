using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class TextureAtlas : ScriptableObject {

    #region 序列化的数据
    public Texture2D TargetTex;

    public Vector2 Offset;
    public Vector2 Scale;

    public List<TextureAtlasElement> ElementList = new List<TextureAtlasElement>();

    #endregion

    #region 
    private string mOutPutDir;

    private string mAssetPath;
    private string mAtlasPath;

    private MaxRectsBinPack mMaxRectsBinPack;

    private bool bDirty;

    private int mWidth;
    private int mHeight;

    private bool mAllowFlip;

    private MaxRectsBinPack.FreeRectChoiceHeuristic mPackStrategy= MaxRectsBinPack.FreeRectChoiceHeuristic.RectBestAreaFit;
    #endregion

    public TextureAtlas(int width, int height, bool allowFlip, string outputDir, int index)
    {
        mMaxRectsBinPack = new MaxRectsBinPack(width, height, allowFlip);

        mWidth = width;
        mHeight = height;

        mAllowFlip = allowFlip;

        mOutPutDir = outputDir;

        string assetDir = outputDir + "/Asset/";
        CreateDir(assetDir);

        mAssetPath = assetDir + index + ".asset";

        string atlasDir = outputDir + "/Atlas/";
        CreateDir(atlasDir);

        mAtlasPath = atlasDir + index + ".png";
    }

    public void Pack()
    {
        if (bDirty)
        {
            if (File.Exists(mAssetPath))
            {
                File.Delete(mAssetPath);
            }

            AssetDatabase.CreateAsset(this, mAssetPath);

            Texture2D atlas = new Texture2D(mWidth, mHeight, TextureFormat.RGBA32, false);

            for (int i=0; i<ElementList.Count; ++i)
            {
                TextureAtlasElement element = ElementList[i];

                if (null != element && null != element.Tex)
                {
                    Color[] colors = element.Tex.GetPixels();

                    atlas.SetPixels((int)element.Offset.x, (int)element.Offset.y, (int)(element.Tex.width*element.Scale.x), (int)(element.Tex.height*element.Scale.y), colors);
                }
            }

            bDirty = false;
        }
    }

    private void CreateDir(string dir)
    {
        if (Directory.Exists(dir))
            return;

        Directory.CreateDirectory(dir);
    }

    public bool AddTexture(Texture2D texture)
    {
        if (ExistTexture(texture))
        {
            Debug.LogError("Contains repeating texture! " + texture.name);
            return true;
        }

        bDirty = true;

        Rect rect = mMaxRectsBinPack.Insert(texture.width, texture.height, mPackStrategy);

        if (rect == Rect.zero)
        {
            return false;
        }
        else
        {
            TextureAtlasElement element = new TextureAtlasElement
            {
                Offset = new Vector2(rect.x, rect.y),
                Tex = texture,
                Scale = new Vector2(rect.width/texture.width, rect.height/texture.height)
            };

            ElementList.Add(element);

            return true;
        }
    }

    public void RemoveTexture(Texture2D texture)
    {
        for (int i = ElementList.Count - 1; i >= 0; --i)
        {
            if (ElementList[i].Tex == texture)
            {
                ElementList.RemoveAt(i);
                bDirty = true;
                break;
            }
        }
    }

    public bool ExistTexture(Texture2D texture)
    {
        for (int i = 0; i < ElementList.Count; ++i)
        {
            if (ElementList[i].Tex == texture)
            {
                return true;
            }
        }

        return false;
    }
}
