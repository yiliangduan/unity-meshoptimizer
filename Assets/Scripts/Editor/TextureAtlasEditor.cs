using UnityEditor;
using UnityEngine;

namespace Elang.Tools
{
    [CustomEditor(typeof(TextureAtlas))]
    public class TextureAtlasEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            GUILayout.BeginVertical();

            TextureAtlas textureAtlas = (TextureAtlas)target;

            GUILayout.BeginHorizontal();
            if(GUILayout.Button("Pack"))
            {
                textureAtlas.Pack();
            }

            if (GUILayout.Button("Flush"))
            {
                textureAtlas.Flush();
            }
            GUILayout.EndHorizontal();
      
            for (int i=0; i<textureAtlas.TextureInfoList.Count; ++i)
            {
                TextureInfo textureInfo = textureAtlas.TextureInfoList[i];

                GUILayout.BeginHorizontal();

                textureInfo.Tex = EditorGUILayout.ObjectField(textureInfo.Tex, typeof(Texture2D), false) as Texture2D;

                if (GUILayout.Button("Delete"))
                {
                    textureAtlas.RemoveTexture(textureInfo.Tex);

                    EditorUtility.SetDirty(textureAtlas);
                    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();

            base.OnInspectorGUI();
        }
    }
}