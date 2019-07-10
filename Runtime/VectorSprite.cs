using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.U2D;
using UnityEngine.Experimental.Rendering;

namespace Unity.VectorGraphics
{
    public static partial class VectorUtils
    {
        /// <summary>The alignement of the sprite, to determine the location of the pivot.</summary>
        public enum Alignment
        {
            /// <summary>Center alignment.</summary>
            Center = 0,

            /// <summary>Top-left alignment.</summary>
            TopLeft = 1,

            /// <summary>Top-center alignment.</summary>
            TopCenter = 2,

            /// <summary>Top-right alignment.</summary>
            TopRight = 3,

            /// <summary>Left-center alignment.</summary>
            LeftCenter = 4,

            /// <summary>Right-center alignment.</summary>
            RightCenter = 5,

            /// <summary>Bottom-left alignment.</summary>
            BottomLeft = 6,

            /// <summary>Bottom-center alignment.</summary>
            BottomCenter = 7,

            /// <summary>Bottom-right alignment.</summary>
            BottomRight = 8,

            /// <summary>SVG origin alignment.</summary>
            /// <remarks>
            /// This will use the origin of the SVG document as the origin of the sprite.
            /// </remarks>
            SVGOrigin = 9,

            /// <summary>Custom alignment.</summary>
            /// <remarks>
            /// Uses a custom alignment that will be used when building the sprite using the <see cref="BuildSprite"/> method.
            /// </remarks>
            Custom = 10
        }

        /// <summary>Builds a sprite asset from a scene tessellation.</summary>
        /// <param name="geoms">The list of tessellated Geometry instances</param>
        /// <param name="svgPixelsPerUnit">How many SVG "pixels" map into a Unity unit</param>
        /// <param name="alignment">The position of the sprite origin</param>
        /// <param name="customPivot">If alignment is <see cref="Alignment.Custom"/>, customPivot is used to compute the sprite origin</param>
        /// <param name="gradientResolution">The maximum size of the texture holding gradient data</param>
        /// <param name="flipYAxis">True to have the positive Y axis to go downward.</param>
        /// <returns>A new Sprite containing the provided geometry. The Sprite may have a texture if the geometry has any texture and/or gradients</returns>
        public static Sprite BuildSprite(List<Geometry> geoms, float svgPixelsPerUnit, Alignment alignment, Vector2 customPivot, UInt16 gradientResolution, bool flipYAxis = false)
        {
            // Generate atlas
            var texAtlas = GenerateAtlasAndFillUVs(geoms, gradientResolution);

            List<Vector2> vertices;
            List<UInt16> indices;
            List<Color> colors;
            List<Vector2> uvs;
            List<Vector2> settingIndices;
            FillVertexChannels(geoms, 1.0f, texAtlas != null, out vertices, out indices, out colors, out uvs, out settingIndices);

            Texture2D texture = texAtlas != null ? texAtlas.Texture : null;
            var bbox = VectorUtils.RealignVerticesInBounds(vertices, flipYAxis);
            var rect = new Rect(0, 0, bbox.width, bbox.height);
            var pivot = GetPivot(alignment, customPivot, bbox);

            // The Sprite.Create(Rect, Vector2, float, Texture2D) method is internal. Using reflection
            // until it becomes public.
            var spriteCreateMethod = typeof(Sprite).GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic, Type.DefaultBinder, new Type[] { typeof(Rect), typeof(Vector2), typeof(float), typeof(Texture2D) }, null);
            var sprite = spriteCreateMethod.Invoke(null, new object[] { rect, pivot, svgPixelsPerUnit, texture }) as Sprite;

            sprite.OverrideGeometry(vertices.ToArray(), indices.ToArray());

            if (colors != null)
            {
                var colors32 = colors.Select(c => (Color32)c);
                using (var nativeColors = new NativeArray<Color32>(colors32.ToArray(), Allocator.Temp))
                    sprite.SetVertexAttribute<Color32>(VertexAttribute.Color, nativeColors);
            }
            if (uvs != null)
            {
                using (var nativeUVs = new NativeArray<Vector2>(uvs.ToArray(), Allocator.Temp))
                    sprite.SetVertexAttribute<Vector2>(VertexAttribute.TexCoord0, nativeUVs);
                using (var nativeSettingIndices = new NativeArray<Vector2>(settingIndices.ToArray(), Allocator.Temp))
                    sprite.SetVertexAttribute<Vector2>(VertexAttribute.TexCoord2, nativeSettingIndices);
            }

            return sprite;
        }

        /// <summary>Fills a mesh geometry from a scene tessellation.</summary>
        /// <param name="mesh">The mesh object to fill</param>
        /// <param name="geoms">The list of tessellated Geometry instances, generated by TessellateNodeHierarchy</param>
        /// <param name="svgPixelsPerUnit">How many SVG "pixels" map into a Unity unit</param>
        /// <param name="flipYAxis">Set to "true" to have the positive Y axis to go downward.</param>
        public static void FillMesh(Mesh mesh, List<Geometry> geoms, float svgPixelsPerUnit, bool flipYAxis = false)
        {
            bool hasUVs = (geoms.FirstOrDefault(g => g.UVs != null)) != null;

            // Generate atlas
            List<Vector2> vertices;
            List<UInt16> indices;
            List<Color> colors;
            List<Vector2> uvs;
            List<Vector2> settingIndices;
            FillVertexChannels(geoms, svgPixelsPerUnit, hasUVs, out vertices, out indices, out colors, out uvs, out  settingIndices);

            mesh.Clear();
            mesh.SetVertices(vertices.Select(v => (Vector3)v).ToList());
            mesh.SetTriangles(indices.Select(i => (int)i).ToArray(), 0);

            if (colors != null)
                mesh.SetColors(colors);

            if (uvs != null)
                mesh.SetUVs(0, uvs);
            if (settingIndices  != null)
                mesh.SetUVs(2, settingIndices);
        }

        private static void FillVertexChannels(List<Geometry> geoms, float pixelsPerUnit, bool hasUVs, out List<Vector2> vertices, out List<UInt16> indices, out List<Color> colors, out List<Vector2> uvs, out List<Vector2> settingIndices)
        {
            int totalVerts = 0, totalIndices = 0;
            foreach (var geom in geoms)
            {
                if (geom.Indices.Length != 0)
                {
                    totalIndices += geom.Indices.Length;
                    totalVerts += geom.Vertices.Length;
                }
            }

            vertices = new List<Vector2>(totalVerts);
            indices = new List<UInt16>(totalIndices);
            colors = new List<Color>(totalVerts);
            uvs = hasUVs ? new List<Vector2>(totalVerts) : null;
            settingIndices = hasUVs ? new List<Vector2>(totalVerts) : null;

            foreach (var geom in geoms)
            {
                int vertexCount = vertices.Count;
                indices.AddRange(geom.Indices.Select(x => (UInt16)(x + vertexCount)));
                vertices.AddRange(geom.Vertices.Select(x => (geom.WorldTransform * x) / pixelsPerUnit));
                colors.AddRange(Enumerable.Repeat(geom.Color, geom.Vertices.Length));
                System.Diagnostics.Debug.Assert(uvs == null || geom.UVs != null);
                if (uvs != null)
                {
                    uvs.AddRange(geom.UVs);
                    for (int i = 0; i < geom.UVs.Length; i++)
                        settingIndices.Add(new Vector2(geom.SettingIndex, 0));
                }
            }
        }

        /// <summary>Draws a vector sprite using the provided material.</summary>
        /// <param name="sprite">The sprite to render</param>
        /// <param name="mat">The material used for rendering</param>
        public static void RenderSprite(Sprite sprite, Material mat) 
        {
            float spriteWidth = sprite.rect.width;
            float spriteHeight = sprite.rect.height;
            float pixelsToUnits = sprite.rect.width / sprite.bounds.size.x;

            var vertices = sprite.vertices;
            var uvs = sprite.uv;
            var triangles = sprite.triangles;
            var pivot = sprite.pivot;

            NativeSlice<Color32>? colors = null;
            if (sprite.HasVertexAttribute(VertexAttribute.Color))
                colors = sprite.GetVertexAttribute<Color32>(VertexAttribute.Color);

            NativeSlice<Vector2>? settings = null;
            if (sprite.HasVertexAttribute(VertexAttribute.TexCoord2))
                settings = sprite.GetVertexAttribute<Vector2>(VertexAttribute.TexCoord2);

            mat.SetTexture("_MainTex", sprite.texture);
            mat.SetPass(0);

            GL.Clear(true, true, new Color(0f, 0f, 0f, 0f));
            GL.PushMatrix();
            GL.LoadOrtho();
            GL.Color(new Color(1, 1, 1, 1));
            GL.Begin(GL.TRIANGLES);
            for (int i = 0; i < triangles.Length; ++i)
            {
                ushort index = triangles[i];
                Vector2 vertex = vertices[index];
                Vector2 uv = uvs[index];
                GL.TexCoord2(uv.x, uv.y);
                if (settings != null)
                {
                    var setting = settings.Value[index];
                    GL.MultiTexCoord2(2, setting.x, setting.y);
                }
                if (colors != null)
                {
                    GL.Color(colors.Value[index]);
                }
                GL.Vertex3((vertex.x * pixelsToUnits + pivot.x) / spriteWidth, (vertex.y * pixelsToUnits + pivot.y) / spriteHeight, 0);
            }
            GL.End();
            GL.PopMatrix();

            mat.SetTexture("_MainTex", null);
        }

        /// <summary>Renders a vector sprite to Texture2D.</summary>
        /// <param name="sprite">The sprite to render</param>
        /// <param name="width">The desired width of the resulting texture</param>
        /// <param name="height">The desired height of the resulting texture</param>
        /// <param name="mat">The material used to render the sprite</param>
        /// <param name="antiAliasing">The number of samples per pixel for anti-aliasing</param>
        /// <returns>A Texture2D object containing the rendered vector sprite</returns>
        public static Texture2D RenderSpriteToTexture2D(Sprite sprite, int width, int height, Material mat, int antiAliasing = 1)
        {
            if (width <= 0 || height <= 0)
                return null;

            var tex = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            tex.antiAliasing = antiAliasing;

            var oldActive = RenderTexture.active;
            RenderTexture.active = tex;

            RenderSprite(sprite, mat);

            Texture2D copy = new Texture2D(width, height, TextureFormat.RGBA32, false);
            copy.hideFlags = HideFlags.HideAndDontSave;
            copy.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            copy.Apply();

            RenderTexture.active = oldActive;
            tex.Release();

            RenderTexture.DestroyImmediate(tex);
            Material.DestroyImmediate(mat);

            return copy;
        }

        private static Vector2 GetPivot(Alignment alignment, Vector2 customPivot, Rect bbox)
        {
            switch (alignment)
            {
                case Alignment.Center: return new Vector2(0.5f, 0.5f);
                case Alignment.TopLeft: return new Vector2(0.0f, 1.0f);
                case Alignment.TopCenter: return new Vector2(0.5f, 1.0f);
                case Alignment.TopRight: return new Vector2(1.0f, 1.0f);
                case Alignment.LeftCenter: return new Vector2(0.0f, 0.5f);
                case Alignment.RightCenter: return new Vector2(1.0f, 0.5f);
                case Alignment.BottomLeft: return new Vector2(0.0f, 0.0f);
                case Alignment.BottomCenter: return new Vector2(0.5f, 0.0f);
                case Alignment.BottomRight: return new Vector2(1.0f, 0.0f);
                case Alignment.SVGOrigin: return -bbox.position / bbox.size;
                case Alignment.Custom: return customPivot;
            }
            return Vector2.zero;
        }
    }
}
