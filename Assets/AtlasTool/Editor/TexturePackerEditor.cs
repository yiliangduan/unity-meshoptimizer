using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class TexturePackerEditor
{
    [MenuItem("Assets/Pack Texture(All Realign)")]
    public static void CombineTexture()
    {
        Object activeObject = Selection.activeObject;

        if (null != activeObject)
        {
            string path = AssetDatabase.GetAssetPath(activeObject);

            if (Directory.Exists(path))
            {
                TexturePacker.Pack(path, AtlasConfig.AtlasDefaultWidth, AtlasConfig.AtlasDefaultHeight, true);
            }
        }
    }

    [MenuItem("Assets/Pack Texture", true)]
    public static bool CombineTextureCondition()
    {
        Object activeObject = Selection.activeObject;

        if (null != activeObject)
        {
            string path = AssetDatabase.GetAssetPath(activeObject);

            return Directory.Exists(path);
        }

        return false;
    }

    [MenuItem("Assets/Pack Texture")]
    public static void CombineTextureWithoutChangeOriginal()
    {
        Object activeObject = Selection.activeObject;

        if (null != activeObject)
        {
            string path = AssetDatabase.GetAssetPath(activeObject);

            if (Directory.Exists(path))
            {
                TexturePacker.Pack(path, AtlasConfig.AtlasDefaultWidth, AtlasConfig.AtlasDefaultHeight, false);
            }
        }
    }

    [MenuItem("Assets/Pack Texture", true)]
    public static bool CombineTextureWithoutChangeOriginalCondition()
    {
        Object activeObject = Selection.activeObject;

        if (null != activeObject)
        {
            string path = AssetDatabase.GetAssetPath(activeObject);

            return Directory.Exists(path);
        }

        return false;
    }
}
