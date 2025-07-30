using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using System.Collections;
using System.Collections.Generic;
using Unity.VectorGraphics;

public class SpriteImportTests
{
    [Test]
    public void SVGImportsAsSprite_WhenPackageIsInstalled()
    {
        string svg =
            @"<svg xmlns=""http://www.w3.org/2000/svg"" width=""100"" height=""20"">
                <rect width=""100"" height=""20"" fill=""red"" />
            </svg>";

        // Save to a an asset
        string path = "Assets/TestVectorSprite.svg";
        File.WriteAllText(path, svg);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        Assert.IsNotNull(sprite, "Failed to import vector sprite");

        // Clean up
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.Refresh();
    }
}
