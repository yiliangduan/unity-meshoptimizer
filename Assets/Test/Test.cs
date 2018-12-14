using Elang.Tools;
using UnityEngine;

// Custom asset type that prefers binary serialization.
//
// Create a new asset file by going to "Asset/Create/Custom Data".
// If you open this new asset in a text editor, you can see how it
// is not affected by changing the project asset serialization mode.
//
[CreateAssetMenu]
[PreferBinarySerialization]
public class CustomData : ScriptableObject
{
    public float[] lotsOfFloatData = new[] { 1f, 2f, 3f };
    public byte[] lotsOfByteData = new byte[] { 4, 5, 6 };

    public IntVector2 position = new IntVector2(1, 5);
}