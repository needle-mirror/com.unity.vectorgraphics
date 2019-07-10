using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using System.Collections;
using System.Collections.Generic;
using Unity.VectorGraphics;

public class SVGParserTests
{
    [Test]
    public void ImportSVG_CreatesScene()
    {
        string svg =
            @"<svg xmlns=""http://www.w3.org/2000/svg"" width=""100"" height=""20"">
                <rect width=""100"" height=""20"" />
            </svg>";

        var sceneInfo = SVGParser.ImportSVG(new StringReader(svg));
        Assert.IsNotNull(sceneInfo.Scene);
        Assert.IsNotNull(sceneInfo.Scene.Root);
    }

    [Test]
    public void ImportSVG_SupportsRects()
    {
        string svg =
            @"<svg xmlns=""http://www.w3.org/2000/svg"" width=""100"" height=""20"">
                <rect x=""5"" y=""10"" width=""100"" height=""20"" />
            </svg>";

        var sceneInfo = SVGParser.ImportSVG(new StringReader(svg));
        var rect = sceneInfo.Scene.Root.Children[0].Drawables[0] as Rectangle;
        Assert.IsNotNull(rect);
        Assert.AreEqual(new Vector2(5.0f, 10.0f), rect.Position);
        Assert.AreEqual(new Vector2(100.0f, 20.0f), rect.Size);
        Assert.AreEqual(Vector2.zero, rect.RadiusTL);
        Assert.AreEqual(Vector2.zero, rect.RadiusTR);
        Assert.AreEqual(Vector2.zero, rect.RadiusBL);
        Assert.AreEqual(Vector2.zero, rect.RadiusBR);
    }

    [Test]
    public void ImportSVG_SupportsCircles()
    {
        string svg =
            @"<svg xmlns=""http://www.w3.org/2000/svg"" width=""100"" height=""20"">
                <circle cx=""50"" cy=""60"" r=""50"" />
            </svg>";

        var sceneInfo = SVGParser.ImportSVG(new StringReader(svg));

        // Circles/ellipses are stored as a rectangle with rounded corners
        var rect = sceneInfo.Scene.Root.Children[0].Drawables[0] as Rectangle;
        Assert.IsNotNull(rect);
        Assert.AreEqual(new Vector2(0.0f, 10.0f), rect.Position);
        Assert.AreEqual(new Vector2(100.0f, 100.0f), rect.Size);
        Assert.AreEqual(new Vector2(50.0f, 50.0f), rect.RadiusTL);
        Assert.AreEqual(new Vector2(50.0f, 50.0f), rect.RadiusTR);
        Assert.AreEqual(new Vector2(50.0f, 50.0f), rect.RadiusBL);
        Assert.AreEqual(new Vector2(50.0f, 50.0f), rect.RadiusBR);
    }

    [Test]
    public void ImportSVG_SupportsEllipses()
    {
        string svg =
            @"<svg xmlns=""http://www.w3.org/2000/svg"" width=""100"" height=""20"">
                <ellipse cx=""50"" cy=""60"" rx=""50"" ry=""60"" />
            </svg>";

        var sceneInfo = SVGParser.ImportSVG(new StringReader(svg));

        // Circles/ellipses are stored as a rectangle with rounded corners
        var rect = sceneInfo.Scene.Root.Children[0].Drawables[0] as Rectangle;
        Assert.IsNotNull(rect);
        Assert.AreEqual(new Vector2(0.0f, 0.0f), rect.Position);
        Assert.AreEqual(new Vector2(100.0f, 120.0f), rect.Size);
        Assert.AreEqual(new Vector2(50.0f, 60.0f), rect.RadiusTL);
        Assert.AreEqual(new Vector2(50.0f, 60.0f), rect.RadiusTR);
        Assert.AreEqual(new Vector2(50.0f, 60.0f), rect.RadiusBL);
        Assert.AreEqual(new Vector2(50.0f, 60.0f), rect.RadiusBR);
    }

    [Test]
    public void ImportSVG_SupportsPaths()
    {
        string svg =
            @"<svg xmlns=""http://www.w3.org/2000/svg"" width=""100"" height=""20"">
                <path d=""M10,10L100,100"" />
            </svg>";

        var sceneInfo = SVGParser.ImportSVG(new StringReader(svg));

        var shape = sceneInfo.Scene.Root.Children[0].Drawables[0] as Shape;
        Assert.IsNotNull(shape);
        Assert.AreEqual(1, shape.Contours.Length);
        
        var segs = shape.Contours[0].Segments;
        Assert.AreEqual(2, segs.Length);
        Assert.AreEqual(new Vector2(10, 10), segs[0].P0);
        Assert.AreEqual(new Vector2(100, 100), segs[1].P0);
    }

    [Test]
    public void ImportSVG_SupportsLines()
    {
        string svg =
            @"<svg xmlns=""http://www.w3.org/2000/svg"" width=""100"" height=""20"">
                <line x1=""10"" y1=""10"" x2=""100"" y2=""100"" />
            </svg>";

        var sceneInfo = SVGParser.ImportSVG(new StringReader(svg));

        var path = sceneInfo.Scene.Root.Children[0].Drawables[0] as Unity.VectorGraphics.Path;
        Assert.IsNotNull(path);

        var segs = path.Contour.Segments;
        Assert.AreEqual(2, segs.Length);
        Assert.AreEqual(new Vector2(10, 10), segs[0].P0);
        Assert.AreEqual(new Vector2(100, 100), segs[1].P0);
    }

    [Test]
    public void ImportSVG_SupportsPolylines()
    {
        string svg =
            @"<svg xmlns=""http://www.w3.org/2000/svg"" width=""100"" height=""20"">
                <polyline points=""10,10,100,100"" />
            </svg>";

        var sceneInfo = SVGParser.ImportSVG(new StringReader(svg));

        var shape = sceneInfo.Scene.Root.Children[0].Drawables[0] as Shape;
        Assert.IsNotNull(shape);
        Assert.AreEqual(1, shape.Contours.Length);

        var segs = shape.Contours[0].Segments;
        Assert.AreEqual(2, segs.Length);
        Assert.AreEqual(new Vector2(10, 10), segs[0].P0);
        Assert.AreEqual(new Vector2(100, 100), segs[1].P0);
    }

    [Test]
    public void ImportSVG_SupportsPolygons()
    {
        string svg =
            @"<svg xmlns=""http://www.w3.org/2000/svg"" width=""100"" height=""20"">
                <polygon points=""10,10,100,100"" />
            </svg>";

        var sceneInfo = SVGParser.ImportSVG(new StringReader(svg));

        var shape = sceneInfo.Scene.Root.Children[0].Drawables[0] as Shape;
        Assert.IsNotNull(shape);
        Assert.AreEqual(1, shape.Contours.Length);

        var segs = shape.Contours[0].Segments;
        Assert.AreEqual(2, segs.Length);
        Assert.AreEqual(new Vector2(10, 10), segs[0].P0);
        Assert.AreEqual(new Vector2(100, 100), segs[1].P0);
    }

    [Test]
    public void ImportSVG_SupportsSolidFills()
    {
        string svg =
            @"<svg xmlns=""http://www.w3.org/2000/svg"" width=""100"" height=""20"">
                <rect x=""5"" y=""10"" width=""100"" height=""20"" fill=""red"" />
            </svg>";

        var sceneInfo = SVGParser.ImportSVG(new StringReader(svg));
        var rect = sceneInfo.Scene.Root.Children[0].Drawables[0] as Rectangle;
        var fill = rect.Fill as SolidFill;
        Assert.AreEqual(Color.red, fill.Color);
    }

    [Test]
    public void ImportSVG_SupportsLinearGradientFills()
    {
        string svg =
            @"<svg xmlns=""http://www.w3.org/2000/svg"" width=""100"" height=""20"">
                <defs>
                    <linearGradient id=""grad"">
                        <stop offset=""0%"" stop-color=""blue"" />
                        <stop offset=""100%"" stop-color=""red"" />
                    </linearGradient>
                </defs>
                <rect x=""5"" y=""10"" width=""100"" height=""20"" fill=""url(#grad)"" />
            </svg>";

        var sceneInfo = SVGParser.ImportSVG(new StringReader(svg));
        var rect = sceneInfo.Scene.Root.Children[0].Drawables[0] as Rectangle;
        var fill = rect.Fill as GradientFill;
        Assert.AreEqual(GradientFillType.Linear, fill.Type);
        Assert.AreEqual(2, fill.Stops.Length);
        Assert.AreEqual(0.0f, fill.Stops[0].StopPercentage);
        Assert.AreEqual(Color.blue, fill.Stops[0].Color);
        Assert.AreEqual(1.0f, fill.Stops[1].StopPercentage);
        Assert.AreEqual(Color.red, fill.Stops[1].Color);
    }

    [Test]
    public void ImportSVG_SupportsRadialGradientFills()
    {
        string svg =
            @"<svg xmlns=""http://www.w3.org/2000/svg"" width=""100"" height=""20"">
                <defs>
                    <radialGradient id=""grad"">
                        <stop offset=""0%"" stop-color=""blue"" />
                        <stop offset=""100%"" stop-color=""red"" />
                    </radialGradient>
                </defs>
                <rect x=""5"" y=""10"" width=""100"" height=""20"" fill=""url(#grad)"" />
            </svg>";

        var sceneInfo = SVGParser.ImportSVG(new StringReader(svg));
        var rect = sceneInfo.Scene.Root.Children[0].Drawables[0] as Rectangle;
        var fill = rect.Fill as GradientFill;
        Assert.AreEqual(GradientFillType.Radial, fill.Type);
        Assert.AreEqual(2, fill.Stops.Length);
        Assert.AreEqual(0.0f, fill.Stops[0].StopPercentage);
        Assert.AreEqual(Color.blue, fill.Stops[0].Color);
        Assert.AreEqual(1.0f, fill.Stops[1].StopPercentage);
        Assert.AreEqual(Color.red, fill.Stops[1].Color);
    }

    [Test]
    public void ImportSVG_SupportsFillOpacities()
    {
        string svg =
            @"<svg xmlns=""http://www.w3.org/2000/svg"" width=""100"" height=""20"">
                <rect x=""5"" y=""10"" width=""100"" height=""20"" fill=""red"" fill-opacity=""0.5"" />
            </svg>";

        var sceneInfo = SVGParser.ImportSVG(new StringReader(svg));
        var rect = sceneInfo.Scene.Root.Children[0].Drawables[0] as Rectangle;
        var fill = rect.Fill as SolidFill;
        Assert.AreEqual(0.5f, fill.Color.a);
    }

    [Test]
    public void ImportSVG_SupportsGroups()
    {
        string svg =
            @"<svg xmlns=""http://www.w3.org/2000/svg"" width=""100"" height=""20"">
                <g>
                    <rect x=""5"" y=""10"" width=""100"" height=""20"" />
                    <rect x=""5"" y=""50"" width=""100"" height=""20"" />
                </g>
            </svg>";

        var sceneInfo = SVGParser.ImportSVG(new StringReader(svg));
        Assert.AreEqual(1, sceneInfo.Scene.Root.Children.Count);

        var group = sceneInfo.Scene.Root.Children[0];
        Assert.AreEqual(2, group.Children.Count);
    }

    [Test]
    public void ImportSVG_RemovesElementsWithDisplayNone()
    {
        string svg = 
            @"<svg xmlns=""http://www.w3.org/2000/svg"" width=""100"" height=""20"">
                <rect width=""100"" height=""20"" display=""none"" />
            </svg>";

        var sceneInfo = SVGParser.ImportSVG(new StringReader(svg));
        Assert.AreEqual(0, sceneInfo.Scene.Root.Children.Count);
    }
}
