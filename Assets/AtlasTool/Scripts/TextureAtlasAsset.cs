using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextureAtlasAsset : ScriptableObject {

    public int Width;
    public int Height;

    public Texture2D Atlas;

    public TextureAtlasElement[] Elements;
}
 