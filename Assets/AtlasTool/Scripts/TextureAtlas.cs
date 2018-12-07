using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Elang.Tools
{
    public class TextureAtlas : ScriptableObject
    {
        [HideInInspector]
        public string AssetPath;

        [HideInInspector]
        public string AtlasPath;

        [HideInInspector]
        public bool IsTransparent;

        [HideInInspector]
        public bool AllowFlip;

        [HideInInspector]
        public int Width;

        [HideInInspector]
        public int Height;

        [HideInInspector]
        public Texture2D Atlas;

        [HideInInspector]
        public List<TextureAtlasElement> ElementList = new List<TextureAtlasElement>();

        private MaxRectsBinPack.FreeRectChoiceHeuristic mPackStrategy = MaxRectsBinPack.FreeRectChoiceHeuristic.RectContactPointRule;

        private MaxRectsBinPack mMaxRectsBinPack;

        private bool bDirty;

        public void Init(int width, int height, bool allowFlip, bool isTransparent, string atlasName)
        {
            mMaxRectsBinPack = new MaxRectsBinPack(width, height, allowFlip);

            Width = width;
            Height = height;

            AllowFlip = allowFlip;

            IsTransparent = isTransparent;

            AtlasPath = GetAtlasPath(isTransparent, atlasName);
            AssetPath = GetAssetPath(isTransparent, atlasName);
        }

        /// <summary>
        /// 在TextureAtlas从Asset实例化出来时，MaxRectsBinPack里面的数据是空的，这时把ElementList中的数据反排布到MaxRectsBinPack中
        /// </summary>
        public void Layout()
        {
            mMaxRectsBinPack = new MaxRectsBinPack(Width, Height, AllowFlip);

            for (int i = ElementList.Count-1; i >=0 ; --i)
            {
                TextureAtlasElement element = ElementList[i];

                if (null != element && null != element.Tex)
                {
                    mMaxRectsBinPack.Layout((int)element.Offset.x, (int)element.Offset.y, element.Size.x, element.Size.y);
                }
                else
                {
                    ElementList.RemoveAt(i);
                }
            }

            bDirty = true;
        }

        public void Pack()
        {
            if (bDirty)
            {
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

            File.WriteAllBytes(AtlasPath, atlas.EncodeToPNG());

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            AssetDatabase.SaveAssets();
        }

        private void WriteAsset()
        {
            Atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);

            if (!File.Exists(AssetPath))
            {
                AssetDatabase.CreateAsset(this, AssetPath);
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
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

            IntRect rect = mMaxRectsBinPack.Insert(texture.width, texture.height, mPackStrategy);

            if (rect == IntRect.zero)
            {
                return false;
            }
            else
            {
                TextureAtlasElement element = new TextureAtlasElement
                {
                    Offset = new IntVector2(rect.x, rect.y),
                    Tex = texture,
                    Scale = new IntVector2(rect.width / texture.width, rect.height / texture.height),
                    Size = new IntVector2(texture.width, texture.height)
                };

                ElementList.Add(element);

                return true;
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

        public void RemoveElementAt(int index)
        {
            if (index >= 0 && index < ElementList.Count)
            {
                TextureAtlasElement element = ElementList[index];

                ElementList.RemoveAt(index);
            }
            else
            {
                Debug.LogError("Invalid index." + index);
            }
        }

        public static string GetAssetPath(bool isTransparent, string assetName)
        {
            string assetPath;

            if (isTransparent)
            {
                assetName = string.IsNullOrEmpty(assetName) ? AtlasConfig.TransparentAssetNamePrefix + assetName : assetName;
                assetPath = AtlasConfig.TransparentAssetDir + "tp_" + assetName + ".asset";

                if (!Directory.Exists(AtlasConfig.TransparentAssetDir))
                {
                    Directory.CreateDirectory(AtlasConfig.TransparentAssetDir);
                }
            }
            else
            {
                assetName = string.IsNullOrEmpty(assetName) ? AtlasConfig.OpaqueAssetNamePrefix + assetName : assetName;
                assetPath = AtlasConfig.OpaqueAssetDir + "op_" + assetName + ".asset";

                if (!Directory.Exists(AtlasConfig.OpaqueAssetDir))
                {
                    Directory.CreateDirectory(AtlasConfig.OpaqueAssetDir);
                }
            }

            return assetPath;
        }

        public static string GetAtlasPath(bool isTransparent, string atlasName)
        {
            string atlasPath;

            if (isTransparent)
            {
                atlasName = string.IsNullOrEmpty(atlasName) ? AtlasConfig.TransparentAtlasNamePrefix + atlasName : atlasName;
                atlasPath = AtlasConfig.TransparentAtlasDir + "tp_" + atlasName + ".png";

                if (!Directory.Exists(AtlasConfig.TransparentAtlasDir))
                {
                    Directory.CreateDirectory(AtlasConfig.TransparentAtlasDir);
                }
            }
            else
            {
                atlasName = string.IsNullOrEmpty(atlasName) ? AtlasConfig.OpaqueAtlasnamePrefix + atlasName : atlasName;
                atlasPath = AtlasConfig.OpaqueAtlasDir + "op_" + atlasName + ".jpg";

                if (!Directory.Exists(AtlasConfig.OpaqueAtlasDir))
                {
                    Directory.CreateDirectory(AtlasConfig.OpaqueAtlasDir);
                }
            }

            return atlasPath;
        }

        public TextureAtlasElement GetElement(Texture2D texture)
        {
            for (int i = 0; i < ElementList.Count; ++i)
            {
                TextureAtlasElement element = ElementList[i];
                if (null != element)
                {
                    //防止出现同名但是图片对象不相同的情况(图片Unity格式不同，图片的TextureImporter的属性不同)，造成资源冗余
                    if (element.Tex.name == texture.name && element.Tex != texture)
                    {
                        Debug.LogError("There are texture with the same name. " + element.Tex.name);
                    }

                    if (element.Tex == texture)
                    {
                        return element;
                    }
                }
            }

            return null;
        }
    }
}