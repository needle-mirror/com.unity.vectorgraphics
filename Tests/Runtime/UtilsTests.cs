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
        var node = new SceneNode() {
            Drawables = new List<IDrawable> {
                new Rectangle() {
                    Size = new Vector2(100, 50),
                    Fill = new SolidFill() { Color = Color.red }
                }
            }
        };
        var scene = new Scene() { Root = node };

        var options = new VectorUtils.TessellationOptions()
        {
            StepDistance = 10.0f,
            MaxCordDeviation = float.MaxValue,
            MaxTanAngleDeviation = Mathf.PI/2.0f,
            SamplingStepSize = 0.01f
        };

        return VectorUtils.TessellateScene(scene, options);
    }

    [Test]
    public void BuildSprite_CreatesFullyConstructedSprite()
    {
        var sprite = VectorUtils.BuildSprite(BuildGeoms(), 100.0f, VectorUtils.Alignment.BottomLeft, Vector2.zero, 128);
        Assert.NotNull(sprite);
        Assert.AreEqual((Vector2)sprite.bounds.min, Vector2.zero);
        Assert.AreEqual((Vector2)sprite.bounds.max, new Vector2(1.0f, 0.5f));
        Assert.AreEqual(4, sprite.vertices.Length);
        Sprite.Destroy(sprite);
    }

    [Test]
    public void FillMesh_FillsMeshFromGeometry()
    {
        var mesh = new Mesh();
        VectorUtils.FillMesh(mesh, BuildGeoms(), 100.0f);
        Assert.AreEqual((Vector2)mesh.bounds.min, Vector2.zero);
        Assert.AreEqual((Vector2)mesh.bounds.max, new Vector2(1.0f, 0.5f));
        Assert.AreEqual(4, mesh.vertices.Length);
        Mesh.Destroy(mesh);
    }

    [Test]
    public void RenderSpriteToTexture2D_CreatesTexture2DWithProperSize()
    {
        var mat = new Material(Shader.Find("Unlit/Vector"));
        var sprite = VectorUtils.BuildSprite(BuildGeoms(), 100.0f, VectorUtils.Alignment.BottomLeft, Vector2.zero, 128);
        var tex = VectorUtils.RenderSpriteToTexture2D(sprite, 100, 50, mat);
        Assert.NotNull(tex);
        Assert.AreEqual(100, tex.width);
        Assert.AreEqual(50, tex.height);
        Sprite.Destroy(sprite);
        Texture2D.Destroy(tex);
    }
}
