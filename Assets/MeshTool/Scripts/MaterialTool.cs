using UnityEditor;
using UnityEngine;

namespace Yiliang.Tools
{
    public class MaterialTool
    {
        public static bool Compare(Material a, Material b)
        {
            if (null == a || null == b || null == a.shader || null == b.shader)
                return false;

            if (a.shader.name != b.shader.name)
                return false;

            int shaderACount = ShaderUtil.GetPropertyCount(a.shader);
            int shaderBCount = ShaderUtil.GetPropertyCount(b.shader);

            if (shaderACount != shaderBCount)
                return false;

            for (int i = 0; i < shaderACount; ++i)
            {
                string aProperty = ShaderUtil.GetPropertyName(a.shader, i);
                string bProperty = ShaderUtil.GetPropertyName(b.shader, i);

                if (aProperty != bProperty)
                    return false;

                ShaderUtil.ShaderPropertyType aPropertyType = ShaderUtil.GetPropertyType(a.shader, i);
                ShaderUtil.ShaderPropertyType bPropertyType = ShaderUtil.GetPropertyType(b.shader, i);

                if (aPropertyType != bPropertyType)
                    return false;

                switch (aPropertyType)
                {
                    case ShaderUtil.ShaderPropertyType.Color:
                        {
                            Color colorA = a.GetColor(aProperty);
                            Color colorB = b.GetColor(bProperty);

                            if (!Mathf.Approximately(colorA.a, colorB.a) ||
                                !Mathf.Approximately(colorA.b, colorB.b) ||
                                !Mathf.Approximately(colorA.g, colorB.g) ||
                                !Mathf.Approximately(colorA.r, colorB.r))
                                return false;
                        }
                        break;
                    case ShaderUtil.ShaderPropertyType.Float:
                        {
                            float floatA = a.GetFloat(aProperty);
                            float floatB = b.GetFloat(bProperty);

                            if (!Mathf.Approximately(floatA, floatB))
                                return false;
                        }
                        break;
                    case ShaderUtil.ShaderPropertyType.Range:
                        {
                            float floatA = a.GetFloat(aProperty);
                            float floatB = b.GetFloat(bProperty);

                            if (!Mathf.Approximately(floatA, floatB))
                                return false;
                        }
                        break;
                    case ShaderUtil.ShaderPropertyType.Vector:
                        {
                            Vector3 vectorA = a.GetVector(aProperty);
                            Vector3 vectorB = b.GetVector(bProperty);

                            if (!Mathf.Approximately(vectorA.x, vectorB.x) ||
                                !Mathf.Approximately(vectorA.y, vectorB.y) ||
                                !Mathf.Approximately(vectorA.z, vectorB.z))
                                return false;
                        }
                        break;

                    case ShaderUtil.ShaderPropertyType.TexEnv:
                        {
                            Texture textureA = a.GetTexture(aProperty);
                            Texture textureB = b.GetTexture(bProperty);

                            if (null != textureA && null != textureB)
                            {
                                string textureAPath = AssetDatabase.GetAssetPath(textureA);
                                string textureBPath = AssetDatabase.GetAssetPath(textureB);

                                if (textureAPath != textureBPath)
                                    return false;
                            }
                            else
                            {
                                if (textureA != textureB)
                                    return false;
                            }
                        }
                        break;
                }
            }

            return true;
        }
    }
}
