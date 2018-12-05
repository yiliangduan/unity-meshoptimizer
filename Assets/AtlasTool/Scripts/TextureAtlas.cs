using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class TextureAtlas : ScriptableObject {

    #region Asset
    [HideInInspector]
    public List<TextureAtlasElement> ElementList = new List<TextureAtlasElement>();

    [HideInInspector]
    public int Width;

    [HideInInspector]
    public int Height;

    [HideInInspector]
    public Texture2D Atlas;

    [HideInInspector]
    public bool IsTransparent;
    #endregion

    #region Data
    private string mAssetPath;
    private string mAtlasPath;

    private MaxRectsBinPack mMaxRectsBinPack;

    private bool bDirty;
    private bool mAllowFlip;

    private int mIndex;

    private MaxRectsBinPack.FreeRectChoiceHeuristic mPackStrategy= MaxRectsBinPack.FreeRectChoiceHeuristic.RectBestAreaFit;
    #endregion

    public void Init(int width, int height, bool allowFlip, int index, bool isTransparent)
    {
        mMaxRectsBinPack = new MaxRectsBinPack(width, height, allowFlip);

        Width = width;
        Height = height;

        mAllowFlip = allowFlip;

        IsTransparent = isTransparent;

        if (isTransparent)
        {
            mAssetPath = AtlasConfig.TransparentAssetDir + AtlasConfig.TransparentAssetNamePrefix + index + ".asset";

            if (!Directory.Exists(AtlasConfig.TransparentAssetDir))
            {
                Directory.CreateDirectory(AtlasConfig.TransparentAssetDir);
            }
        }
        else
        {
            mAssetPath = AtlasConfig.OpaqueAssetDir + AtlasConfig.OpaqueAssetNamePrefix + index + ".asset";

            if (!Directory.Exists(AtlasConfig.OpaqueAssetDir))
            {
                Directory.CreateDirectory(AtlasConfig.OpaqueAssetDir);
            }
        }

        if (isTransparent)
        {
            mAtlasPath = AtlasConfig.TransparentAtlasDir + AtlasConfig.TransparentAtlasNamePrefix + index + ".png";

            if (!Directory.Exists(AtlasConfig.TransparentAtlasDir))
            {
                Directory.CreateDirectory(AtlasConfig.TransparentAtlasDir);
            }
        }
        else
        {
            mAtlasPath = AtlasConfig.OpaqueAtlasDir + AtlasConfig.OpaqueAtlasnamePrefix + index + ".jpg";

            if (!Directory.Exists(AtlasConfig.OpaqueAtlasDir))
            {
                Directory.CreateDirectory(AtlasConfig.OpaqueAtlasDir);
            }
        }
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

        Atlas = atlas;

        File.WriteAllBytes(mAtlasPath, atlas.EncodeToPNG());

        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        AssetDatabase.SaveAssets();
    }

    private void WriteAsset()
    {
        Atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(mAtlasPath);
        
        AssetDatabase.CreateAsset(this, mAssetPath);
        AssetDatabase.SaveAssets();
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
