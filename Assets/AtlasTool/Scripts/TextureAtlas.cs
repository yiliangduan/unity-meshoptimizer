using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class TextureAtlas : ScriptableObject {

    public Texture2D TargetTex;

    public Vector2 Offset;
    public Vector2 Scale;

    public string Name = "DefaultAtlas.png";

    public string Path = Application.dataPath + "/Res/Atlas/";

    public int Width = 1024;
    public int Height = 1024;

    public List<TextureAltasElement> ElementList = new List<TextureAltasElement>();

    /// <summary>
    /// 把内存数据刷到本地的Asset文件
    /// </summary>
    public void Flush()
    {
        CreateDir(Path);

        AssetDatabase.CreateAsset(this, Path + Name);
    }

    public void Pack()
    {
        // TODO
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

        ElementList.Add(new TextureAltasElement { Tex = texture });
    }

    public void RemoveTexture(Texture2D texture)
    {
        for (int i = ElementList.Count - 1; i >= 0; --i)
        {
            if (ElementList[i].Tex == texture)
            {
                ElementList.RemoveAt(i);
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
