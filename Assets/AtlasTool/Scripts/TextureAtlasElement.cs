using UnityEngine;


namespace Elang.Tools
{
    [System.Serializable]
    public class TextureAtlasElement
    {
        public Texture2D Tex;

        public IntVector2 Offset;
        public IntVector2 Scale;
        public IntVector2 Size;
    }
}