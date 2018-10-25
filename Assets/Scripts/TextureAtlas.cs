using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Elang.Tools
{
    [System.Serializable]
    public class TextureInfo
    {
        public Texture2D Tex;
        public Vector2 Offset;
        public Vector2 Scale;
    }

    public class TextureAtlas : ScriptableObject
    {
        public string Name = "DefaultAtlas.png";

        public string Path = Application.dataPath + "/Res/Atlas/";

        public int Width = 1024;
        public int Height = 1024;

        public List<TextureInfo> TextureInfoList = new List<TextureInfo>();

        /// <summary>
        /// 把内存数据刷到本地的Asset文件
        /// </summary>
        public void Flush()
        {
            CreateDir(Path);

            AssetDatabase.CreateAsset(this, Path+Name);
        }

        /// <summary>
        /// 合并贴图
        /// </summary>
        public void Pack()
        {
            CreateDir(Path);

            new SimpleTexturePacker().Pack(TextureInfoList, Width, Height, Name, Path);
        }

        private void CreateDir(string dir)
        {
            if (Directory.Exists(dir))
                return;

            Directory.CreateDirectory(dir);
        }

        public void AddTexture(Texture2D texture)
        {
            if (ExistTexture(texture))
                return;

            TextureInfoList.Add(new TextureInfo { Tex=texture});
        }

        public void RemoveTexture(Texture2D texture)
        {
            for (int i=TextureInfoList.Count-1; i>=0; --i)
            {
                if (TextureInfoList[i].Tex == texture)
                {
                    TextureInfoList.RemoveAt(i);
                    break;
                }
            }
        }

        public bool ExistTexture(Texture2D texture)
        {
            for (int i = 0; i < TextureInfoList.Count; ++i)
            {
                if (TextureInfoList[i].Tex == texture)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
