using UnityEditor;
using UnityEngine;

namespace Elang.Tools
{
    [CustomEditor(typeof(TextureAtlas))]
    public class TextureAtlasEditor : Editor
    {
        private bool bShowElementTransform = true;

        public override void OnInspectorGUI()
        {
            GUILayout.BeginVertical();

            TextureAtlas textureAtlas = (TextureAtlas)target;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Atlas", GUILayout.Width(40));

            EditorGUILayout.ObjectField(textureAtlas.Atlas, typeof(Texture2D), false, GUILayout.Width(150));

            if (GUILayout.Button("Transform Visible", GUILayout.Width(120)))
            {
                bShowElementTransform = !bShowElementTransform;
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            using (EditorGUILayout.VerticalScope scope = new EditorGUILayout.VerticalScope(GUILayout.Width(EditorGUIUtility.currentViewWidth - 5)))
            {
                for (int i = 0; i < textureAtlas.ElementList.Count; ++i)
                {
                    GUILayout.Space(10);

                    TextureAtlasElement element = textureAtlas.ElementList[i];

                    element.Tex = EditorGUILayout.ObjectField(element.Tex, typeof(Texture2D), false, GUILayout.Width(180)) as Texture2D;

                    if (bShowElementTransform)
                    {
                        Vector2 offset = new Vector2(element.Offset.x, element.Offset.y);
                        EditorGUILayout.Vector2Field("Offset", offset);

                        Vector2 scale = new Vector2(element.Scale.x, element.Scale.y);
                        EditorGUILayout.Vector2Field("Scale", scale);
                    }
                }
            }

            GUILayout.EndVertical();
            GUILayout.Space(10);

            base.OnInspectorGUI();
        }
    }
}