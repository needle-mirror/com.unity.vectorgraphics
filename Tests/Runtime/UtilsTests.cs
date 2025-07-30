using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using Unity.VectorGraphics;

public class UtilsTests
{
    private static List<VectorUtils.Geometry> BuildGeoms()
    {
        var rect = new Shape();
        VectorUtils.MakeRectangleShape(rect, new Rect(0,0, 100, 50));
        rect.Fill = new SolidFill() { Color = Color.red };

        var node = new SceneNode() {
            Shapes = new List<Shape> { rect }
        };
        var scene = new Scene() { Root = node };

        var options = new VectorUtils.TessellationOptions()
        {
            StepDistance = float.MaxValue,
            MaxCordDeviation = float.MaxValue,
            MaxTanAngleDeviation = Mathf.PI/2.0f,
            SamplingStepSize = 0.01f
        };

        var geoms = VectorUtils.TessellateScene(scene, options);
        return geoms;
    }

    [Test]
    public void BuildSprite_CreatesFullyConstructedSprite()
    {
        var sprite = VectorUtils.BuildSprite(BuildGeoms(), 100.0f, VectorUtils.Alignment.BottomLeft, Vector2.zero, 128);
        Assert.NotNull(sprite);
        Assert.AreEqual((Vector2)sprite.bounds.min, Vector2.zero);
        Assert.AreEqual((Vector2)sprite.bounds.max, new Vector2(1.0f, 0.5f));
        Assert.AreEqual(5, sprite.vertices.Length);
        Sprite.Destroy(sprite);
    }
}
