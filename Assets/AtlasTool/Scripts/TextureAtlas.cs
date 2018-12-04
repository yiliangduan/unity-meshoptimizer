using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class TextureAtlas : ScriptableObject {

    public List<TextureAtlasElement> ElementList = new List<TextureAtlasElement>();

    public int Width;
    public int Height;

    private string mOutPutDir;

    private string mAssetPath;
    private string mAtlasPath;

    private MaxRectsBinPack mMaxRectsBinPack;

    private bool bDirty;
    private bool mAllowFlip;

    private Texture2D mAtlas;

    private MaxRectsBinPack.FreeRectChoiceHeuristic mPackStrategy= MaxRectsBinPack.FreeRectChoiceHeuristic.RectBestShortSideFit;

    public void Init(int width, int height, bool allowFlip, string outputDir, int index)
    {
        mMaxRectsBinPack = new MaxRectsBinPack(width, height, allowFlip);

        Width = width;
        Height = height;

        mAllowFlip = allowFlip;

        mOutPutDir = outputDir;

        string assetDir = outputDir + "Asset/";
        CreateDir(assetDir);

        mAssetPath = assetDir + "atlas_" + index + ".asset";

        string atlasDir = outputDir + "Atlas/";
        CreateDir(atlasDir);

        mAtlasPath = atlasDir + "atlas_" +index + ".png";
    }

    public void Pack()
    {
        if (bDirty)
        {
            if (File.Exists(mAssetPath))
            {
                File.Delete(mAssetPath);
            }

            WriteTexture();

            WriteAsset();

            bDirty = false;
        }
    }

    private void WriteTexture()
    {
        Texture2D atlas = new Texture2D(Width, Height, TextureFormat.RGBA32, false);

        Color[] defaultColors = atlas.GetPixels();

        //设置默认的颜色为黑色

        Color defaultColor = new Color(0, 0, 0, 0);
        for (int i = 0; i < defaultColors.Length; ++i)
        {
            defaultColors[i] = defaultColor;
        }

        //把每张图片的像素写入atlas中.
        for (int i = 0; i < ElementList.Count; ++i)
        {
            TextureAtlasElement element = ElementList[i];

            if (null != element && null != element.Tex)
            {
                Color[] colors = element.Tex.GetPixels();

                int offsetX = (int)element.Offset.x;
                int offsetY = (int)element.Offset.y;

                for (int column = 0; column < element.Tex.width; ++column)
                {
                    for (int row = 0; row < element.Tex.height; ++row)
                    {
                        int atlasColorIndex = (column + offsetX) + (row + offsetY) * atlas.width;
                        int texColorIndex = column + row * element.Tex.width;

                        defaultColors[atlasColorIndex] = colors[texColorIndex];
                    }
                }
            }
        }

        atlas.SetPixels(defaultColors);
        atlas.Apply();

        mAtlas = atlas;

        File.WriteAllBytes(mAtlasPath, atlas.EncodeToPNG());
    }

    private void WriteAsset()
    {
        TextureAtlasAsset asset = ScriptableObject.CreateInstance<TextureAtlasAsset>();

        if (null != asset)
        {
            asset.Elements = ElementList.ToArray();
            asset.Atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(mAtlasPath);
            asset.Width = Width;
            asset.Height = Height;

            AssetDatabase.CreateAsset(asset, mAssetPath);
            AssetDatabase.SaveAssets();
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
