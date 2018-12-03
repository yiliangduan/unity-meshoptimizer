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
                new TexturePacker().Pack(path, 1024, 1024);
            }
        }
    }
}
