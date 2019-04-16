using UnityEngine;


namespace Yiliang.Tools
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