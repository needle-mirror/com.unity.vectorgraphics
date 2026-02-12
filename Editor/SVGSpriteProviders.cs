using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Unity.Collections;
using UnityEngine;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using System.Runtime.CompilerServices;
using static UnityEditor.U2D.ScriptablePacker;

namespace Unity.VectorGraphics.Editor
{
    internal class SVGAssetsPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessAsset()
        {
            if (!assetPath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                return;

            var importer = assetImporter as SVGImporter;
            if (importer == null || importer.spriteProvider != null)
                return;

            var dataProviderFactories = new SpriteDataProviderFactories();
            dataProviderFactories.Init();
            var provider = dataProviderFactories.GetSpriteEditorDataProviderFromObject(importer);

            importer.spriteProvider = provider;
        }
    }

    internal class SVGDataProviderFactory :
                ISpriteDataProviderFactory<Texture2D>,
                ISpriteDataProviderFactory<GameObject>,
                ISpriteDataProviderFactory<SVGImporter>
    {
        public ISpriteEditorDataProvider CreateDataProvider(Texture2D obj)
        {
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(obj)) as SVGImporter;
            if (importer != null)
                return new SVGImporterDataProvider(importer);
            return null;
        }

        public ISpriteEditorDataProvider CreateDataProvider(GameObject obj)
        {
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(obj)) as SVGImporter;
            if (importer != null)
                return new SVGImporterDataProvider(importer);
            return null;
        }

        public ISpriteEditorDataProvider CreateDataProvider(SVGImporter obj)
        {
            return new SVGImporterDataProvider(obj);
        }
    }

    static class SVGDataUtils
    {
        public static void CopyToSpriteRect(SVGSpriteData data, SpriteRect spriteRect)
        {
            spriteRect.spriteID = data.SpriteGUID;
            spriteRect.name = data.SpriteName;
            spriteRect.rect = data.SpriteRect;
            spriteRect.border = data.SpriteBorder;
            spriteRect.alignment = data.SpriteAlignment;
            spriteRect.pivot = data.SpritePivot;
        }

        public static void CopyFromSpriteRect(SVGSpriteData data, SpriteRect spriteRect)
        {
            data.SpriteGUID = spriteRect.spriteID;
            data.SpriteName = spriteRect.name;
            data.SpriteRect = spriteRect.rect;
            data.SpriteBorder = spriteRect.border;
            data.SpriteAlignment = spriteRect.alignment;
            data.SpritePivot = spriteRect.pivot;
        }
    }

    [InitializeOnLoad]
    internal class SVGImporterDataProvider : ISpriteEditorDataProvider, ISpriteFrameEditCapability, ISVGSpritePhysicsProvider, ISVGImageProvider
    {
        private SVGImporter m_SVGImporter;

        private static float kDefaultPhysicsTessellationDetail = 0.25f;
        private static byte kDefaultSpritePhysicsAlphaTolerance = 200;

        static SVGImporterDataProvider()
        {
            // Let the VectorGraphics module know that the package is installed
            SVGImporter.s_IsVectorGraphicsPackageInstalled = true;
        }

        public SVGImporterDataProvider(SVGImporter importer)
        {
            m_SVGImporter = importer;
        }

        // ISVGImageProvider methods

        /// <summary>Returns the SVG importer</summary>
        UnityEngine.Object ISVGImageProvider.CreateSVGImageComponent(GameObject go, Sprite sprite, Material material, bool preserveAspect)
        {
            var svgImage = go.AddComponent<SVGImage>();
            svgImage.name = "svgImage";
            svgImage.sprite = sprite;
            svgImage.material = material;
            svgImage.preserveAspect = preserveAspect;
            return svgImage;
        }

        // ISVGSpritePhysicsProvider methods

        // Called by the VectorGraphics module to set the physics shape for a sprite.
        void ISVGSpritePhysicsProvider.SetPhysicsShape(Sprite sprite)
        {
            var physicsDataProvider = (this as ISpriteEditorDataProvider).GetDataProvider<ISpritePhysicsOutlineDataProvider>();
            var outlines = physicsDataProvider.GetOutlines(m_SVGImporter.GetSVGSpriteData().SpriteGUID);
            if (outlines.Count == 0)
            {
                var textureDataProvider = (this as ISpriteEditorDataProvider).GetDataProvider<ITextureDataProvider>();
                var tex = textureDataProvider.GetReadableTexture2D();

                outlines = InternalEditorBridge.GenerateOutline(tex, new Rect(0, 0, tex.width, tex.height), kDefaultPhysicsTessellationDetail, kDefaultSpritePhysicsAlphaTolerance, false);
                if (outlines == null || outlines.Count == 0)
                    return;
            }

            int width;
            int height;
            m_SVGImporter.TextureSizeForSpriteEditor(sprite, out width, out height);

            // Offset the outline inside the sprite
            foreach (var outline in outlines)
            {
                for (int i = 0; i < outline.Length; ++i)
                {
                    var v = outline[i];
                    v.x += width / 2.0f;
                    v.y += height / 2.0f;
                    outline[i] = v;
                }
            }

            sprite.OverridePhysicsShape(outlines.Where(o => o.Length > 2).ToArray());
        }

        // ISpriteEditorDataProvider methods

        /// <summary>Returns the sprite import mode</summary>
        /// <remarks>For SVG files, the import mode is always "single"</remarks>
        public SpriteImportMode spriteImportMode
        {
            get { return SpriteImportMode.Single; }
        }

        /// <summary>Returns pixels per unit of the imported SVG</summary>
        public float pixelsPerUnit
        {
            get { return m_SVGImporter.SvgPixelsPerUnit; }
        }

        /// <summary>Returns imported sprite</summary>
        public UnityEngine.Object targetObject
        {
            get { return SVGImporter.GetImportedSprite(m_SVGImporter.assetPath); }
        }

        /// <summary>Returns the sprite rectangles</summary>
        /// <returns>An array of the sprite rectangles</returns>
        SpriteRect[] ISpriteEditorDataProvider.GetSpriteRects()
        {
            var spriteRect = new SpriteRect();
            SVGDataUtils.CopyToSpriteRect(m_SVGImporter.GetSVGSpriteData(), spriteRect);

            return new SpriteRect[] { spriteRect };
        }

        /// <summary>Sets the sprite rectangles</summary>
        /// <param name="rects">The new sprite rectangles to use</param>
        void ISpriteEditorDataProvider.SetSpriteRects(SpriteRect[] rects)
        {
            if (rects.Length > 0)
            {
                var data = m_SVGImporter.GetSVGSpriteData();
                SVGDataUtils.CopyFromSpriteRect(data, rects[0]);
            }
        }

        /// <summary>Applies the modified SVG data</summary>
        void ISpriteEditorDataProvider.Apply()
        {
            var so = new SerializedObject(m_SVGImporter);
            m_SVGImporter.GetSVGSpriteData().Apply(so);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Initializes the sprite editor data</summary>
        void ISpriteEditorDataProvider.InitSpriteEditorDataProvider()
        {
            var data = m_SVGImporter.GetSVGSpriteData();
            var so = new SerializedObject(m_SVGImporter);
            m_SVGImporter.GetSVGSpriteData().Load(so);
        }

        /// <summary>Gets the data provider for a given type</summary>
        /// <typeparam name="T">The type of the data provider</typeparam>
        /// <returns>The data provider</returns>
        T ISpriteEditorDataProvider.GetDataProvider<T>()
        {
            if (typeof(T) == typeof(ISpritePhysicsOutlineDataProvider))
            {
                return new SVGPhysicsOutlineDataProvider(m_SVGImporter) as T;
            }
            if (typeof(T) == typeof(ITextureDataProvider))
            {
                return new SVGTextureDataProvider(m_SVGImporter) as T;
            }
            else if (typeof(T) == typeof(ISpriteEditorDataProvider))
            {
                return this as T;
            }
            else if (typeof(T) == typeof(ISpriteFrameEditCapability))
            {
                return this as T;
            }
            return null;
        }

        /// <summary>Gets the data provider for a given type</summary>
        /// <param name="type">The type</param>
        /// <returns>True if a data provider is available for the type, or false otherwise</returns>
        bool ISpriteEditorDataProvider.HasDataProvider(Type type)
        {
            if (type == typeof(ISpritePhysicsOutlineDataProvider) ||
                type == typeof(ITextureDataProvider))
            {
                return true;
            }
            return false;
        }

        // ISpriteFrameEditCapability methods

        EditCapability ISpriteFrameEditCapability.GetEditCapability()
        {
            if (m_SVGImporter.SvgType == SVGType.TexturedSprite)
                return new EditCapability(EEditCapability.EditPivot, EEditCapability.EditBorder);

            return EditCapability.defaultCapability;
        }

        void ISpriteFrameEditCapability.SetEditCapability(EditCapability editCapability)
        {
            // Not allowed to set EditCapability
        }
    }

    internal class SVGDataProviderBase
    {
        private SVGImporter m_Importer;

        public SVGDataProviderBase(SVGImporter importer)
        {
            m_Importer = importer;
        }

        public SVGSpriteData GetSVGSpriteData()
        {
            return m_Importer.GetSVGSpriteData();
        }

        public SVGImporter GetImporter()
        {
            return m_Importer;
        }

        public Sprite GetSprite()
        {
            var sprite = GetImporter().GetImportingSprite();
            if (sprite == null)
                sprite = SVGImporter.GetImportedSprite(GetImporter().assetPath);
            return sprite;
        }

        public Texture2D GetTexture2D()
        {
            var tex = GetImporter().GetImportingTexture2D();
            if (tex == null)
                tex = SVGImporter.GetImportedTexture2D(GetImporter().assetPath);
            return tex;
        }

        public Vector2 GetTextureSize()
        {
            int targetWidth;
            int targetHeight;
            GetImporter().TextureSizeForSpriteEditor(GetSprite(), out targetWidth, out targetHeight);
            return new Vector2(targetWidth, targetHeight);
        }
    }

    internal class SVGTextureDataProvider : SVGDataProviderBase, ITextureDataProvider
    {
        private float m_TextureScale = 1.0f;

        public SVGTextureDataProvider(SVGImporter importer) : base(importer)
        { }

        public Texture2D texture
        {
            get
            {
                if (GetImporter().SvgType == SVGType.TexturedSprite)
                {
                    return GetTexture2D();
                }

                return null;
            }
        }

        private Texture2D m_PreviewTexture;
        public Texture2D previewTexture
        {
            get
            {
                if (GetImporter().SvgType == SVGType.TexturedSprite)
                {
                    return texture;
                }

                if (m_PreviewTexture == null)
                {
                    var sprite = GetSprite();
                    if (sprite == null)
                        return null;

                    var size = ((Vector2)sprite.bounds.size) * sprite.pixelsPerUnit;

                    const float kMinTextureSize = 2048.0f;
                    if (size.x < kMinTextureSize && size.y < kMinTextureSize)
                    {
                        var maxSize = Math.Max(size.x, size.y);
                        m_TextureScale = kMinTextureSize / maxSize;
                    }

                    var mat = SVGImporter.GetSVGMaterial(sprite.texture != null);
                    m_PreviewTexture = VectorUtils.RenderSpriteToTexture2D(sprite, (int)(size.x * m_TextureScale), (int)(size.y * m_TextureScale), mat, 4);
                }
                return m_PreviewTexture;
            }
        }

        public void GetTextureActualWidthAndHeight(out int width, out int height)
        {
            width = 0;
            height = 0;

            if (GetImporter().SvgType == SVGType.VectorSprite || GetImporter().SvgType == SVGType.UISVGImage)
            {
                GetImporter().TextureSizeForSpriteEditor(GetSprite(), out width, out height);
            }
            else if (GetImporter().SvgType == SVGType.TexturedSprite)
            {
                var tex = GetTexture2D();
                width = tex.width;
                height = tex.height;
            }
        }

        private Texture2D m_ReadableTexture;
        public Texture2D GetReadableTexture2D()
        {
            if (m_ReadableTexture == null)
            {
                if (GetImporter().SvgType == SVGType.VectorSprite)
                {
                    var sprite = GetSprite();
                    var size = ((Vector2)sprite.bounds.size) * sprite.pixelsPerUnit;
                    var mat = SVGImporter.GetSVGMaterial(sprite.texture != null);

                    m_ReadableTexture = VectorUtils.RenderSpriteToTexture2D(sprite, (int)size.x, (int)size.y, mat, 4);
                }
                else
                {
                    return GetTexture2D();
                }
            }
            return m_ReadableTexture;
        }
    }

    [InitializeOnLoad]
    internal class SVGPhysicsOutlineDataProvider : SVGDataProviderBase, ISpritePhysicsOutlineDataProvider
    {
        public SVGPhysicsOutlineDataProvider(SVGImporter importer) : base(importer)
        { }

        List<Vector2[]> ISpritePhysicsOutlineDataProvider.GetOutlines(GUID guid)
        {
            if (GetSVGSpriteData().PhysicsOutlines.Count == 0)
            {
                // If no physics outline was set in the Sprite Editor, show the sprite's physics shape directly (if any)
                var sprite = GetSprite();
                if (sprite == null)
                    return null;

                var importer = GetImporter();
                int width = 0;
                int height = 0;
                importer.TextureSizeForSpriteEditor(sprite, out width, out height);
                var size = new Vector2(width, height);
                var offset = new Vector2(-width/2.0f, -height/2.0f);

                var storedShapes = new List<Vector2[]>(sprite.GetPhysicsShapeCount());
                var shape = new List<Vector2>();
                for (int i = 0; i < sprite.GetPhysicsShapeCount(); ++i)
                {
                    shape.Clear();
                    sprite.GetPhysicsShape(i, shape);
                    var bounds = VectorUtils.Bounds(shape);
                    for (int j = 0; j < shape.Count; ++j)
                    {
                        var p = shape[j];
                        p -= bounds.min;
                        p /= bounds.size;
                        p *= size;
                        p += offset;
                        shape[j] = p;
                    }
                    storedShapes.Add(shape.ToArray());
                }

                return storedShapes;
            }
            return GetSVGSpriteData().PhysicsOutlines.Select(x => x.Vertices.ToArray()).ToList();
        }

        void ISpritePhysicsOutlineDataProvider.SetOutlines(GUID guid, List<Vector2[]> data)
        {
            GetSVGSpriteData().PhysicsOutlines = data.Select(x => new OutlineData() { Vertices = x }).ToList();
        }

        float ISpritePhysicsOutlineDataProvider.GetTessellationDetail(GUID guid)
        {
            return GetSVGSpriteData().TessellationDetail;
        }

        void ISpritePhysicsOutlineDataProvider.SetTessellationDetail(GUID guid, float value)
        {
            GetSVGSpriteData().TessellationDetail = value;
        }
    }
}