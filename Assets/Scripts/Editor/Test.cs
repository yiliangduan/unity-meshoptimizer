using Elang.Tools;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class Test : Editor {

    [MenuItem("Assets/合并贴图")]
	static void CombineTexture()
    {
        Object activeObject = Selection.activeObject;

        if (null != activeObject)
        {
            string path = AssetDatabase.GetAssetPath(activeObject);

            if (Directory.Exists(path))
            {
                string[] files = Directory.GetFiles(path, "*.png");

                if (files.Length > 0)
                {
                    TextureAtlas textureAtlas = new TextureAtlas();

                    for (int i = 0; i < files.Length; ++i)
                    {
                        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(files[i]);

                        if (null != texture)
                        {
                            textureAtlas.AddTexture(texture);
                        }
                    }

                    textureAtlas.Pack();
                }
            }
        }
    }
}
