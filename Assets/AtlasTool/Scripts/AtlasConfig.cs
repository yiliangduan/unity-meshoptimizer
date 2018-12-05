using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class AtlasConfig  {

    private const string AssetDir = "Assets/Res/Asset/";
    public static string AtlasDir = "Assets/Res/Atlas/";

    public static string OpaqueAssetDir = AssetDir+"Opaque/";
    public static string TransparentAssetDir = AssetDir+ "Transparent/";

    public static string OpaqueAtlasDir = AtlasDir + "Opaque/";
    public static string TransparentAtlasDir = AtlasDir + "Transparent/";


    public static string OpaqueAssetNamePrefix = "opaque_asset_";
    public static string OpaqueAtlasnamePrefix = "opaque_atlas_";

    public static string TransparentAssetNamePrefix = "transparent_asset_";
    public static string TransparentAtlasNamePrefix = "transparent_atlas_";
}
