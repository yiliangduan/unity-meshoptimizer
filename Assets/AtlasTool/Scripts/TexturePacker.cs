using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class TexturePacker  {

    public class Atlas
    {
        public int Width;

        public int Height;

        public List<TextureAltasElement> Elements;
    }

    private List<TextureAltasElement> mWillPackTextureList = new List<TextureAltasElement>();

    private int mWidthLimit;
    private int mHeighLimit;

    public void Pack(List<TextureAltasElement> TextureInfoList, int widthLimit, int heighLimit, string atlasName, string atlasFilePath)
    {
        mWillPackTextureList.Clear();

        mWidthLimit = widthLimit;
        mHeighLimit = heighLimit;

        FilterSrcTexture(TextureInfoList);

        List<Atlas> atlases = new List<Atlas>();

        while (mWillPackTextureList.Count > 0)
        {
            Atlas atlas = new Atlas();

            atlas.Width = widthLimit;
            atlas.Height = heighLimit;

            LayoutAtlas(mWillPackTextureList, atlas);

            float maxWidth = 0;
            float maxHeigh = 0;

            for (int i = 0; i < atlas.Elements.Count; ++i)
            {
                TextureAltasElement element = atlas.Elements[i];

                float tmpWidth = element.Offset.x + element.Tex.width;
                if (tmpWidth > maxWidth)
                    maxWidth = tmpWidth;

                float tmpHeight = element.Offset.y + element.Tex.height;
                if (tmpHeight > maxHeigh)
                    maxHeigh = tmpHeight;
            }

            if (maxWidth < atlas.Width)
            {
                if (maxWidth <= atlas.Width / 2)
                    atlas.Width = (int)maxWidth;
            }
            else
            {
                Debug.LogError("altas width fit error!");
            }

            if (maxHeigh < atlas.Height)
            {
                if (maxHeigh <= atlas.Height / 2)
                    atlas.Height = (int)maxHeigh;
            }
            else
            {
                Debug.LogError("atlas heigh fit error!");
            }

            atlases.Add(atlas);
        }

        if (atlases.Count > 0)
        {
            for (int i = 0; i < atlases.Count; ++i)
            {
                Atlas atlas = atlases[i];

                //TODO
            }
        }
        else
        {
            Debug.Log("Nothing in atlas!");
        }
    }

    private void LayoutAtlas(List<TextureAltasElement> textures, Atlas atlas)
    {
        if (null == textures || textures.Count <= 0)
        {
            Debug.LogError("Textures list is empty!");
            return;
        }

        List<TextureAltasElement> inAtlasTextureList = new List<TextureAltasElement>();

        textures.Sort((a, b) =>
        {

            int aMax = System.Math.Max(a.Tex.width, a.Tex.height);
            int bMax = System.Math.Max(b.Tex.width, b.Tex.height);

            return aMax - bMax;

        });

        for (int i = textures.Count - 1; i >= 0; --i)
        {
            TextureAltasElement tmpTexInfo = textures[i];

            bool result = FindBestLocationForTexture(tmpTexInfo, inAtlasTextureList, atlas);

            if (!result)
                break;

            textures.Remove(tmpTexInfo);

            atlas.Elements.Add(tmpTexInfo);
        }
    }

    private bool FindBestLocationForTexture(TextureAltasElement textureInfo, List<TextureAltasElement> atlasTextures, Atlas atlas)
    {
        Vector2 edgeLocation = Vector2.zero;

        for (int i = 0; i < atlasTextures.Count; ++i)
        {
            TextureAltasElement tmpTexInfo = atlasTextures[i];

            float tmpEdgex = tmpTexInfo.Offset.x + tmpTexInfo.Tex.width;
            float tmpEdgey = tmpTexInfo.Offset.y + tmpTexInfo.Tex.height;

            if (tmpEdgex + textureInfo.Tex.width <= atlas.Width &&
                tmpEdgey + textureInfo.Tex.height <= atlas.Height)
            {
                textureInfo.Offset = new Vector2(tmpEdgex + 1, tmpEdgey + 1);

                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 过滤Size比Atlas的Size还要大的Texture
    /// </summary>
    public void FilterSrcTexture(List<TextureAltasElement> TextureInfoList)
    {
        for (int i = 0; i < TextureInfoList.Count; ++i)
        {
            TextureAltasElement textureInfo = TextureInfoList[i];

            if (null == textureInfo.Tex)
                continue;

            if (textureInfo.Tex.width > mWidthLimit || textureInfo.Tex.height > mHeighLimit)
            {
                Debug.LogWarning(textureInfo.Tex.name + " is too large to fix in the atlas. Skipping!");
                continue;
            }

            mWillPackTextureList.Add(textureInfo);
        }
    }
}
