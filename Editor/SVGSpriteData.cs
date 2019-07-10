using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Unity.Collections;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.U2D;
using UnityEngine.Experimental.U2D;

namespace Unity.VectorGraphics.Editor
{
    [Serializable]
    internal class SVGSpriteData
    {
        public float TessellationDetail = 0.0f;
        public SpriteRect SpriteRect = new SpriteRect();
        public List<OutlineData> PhysicsOutlines = new List<OutlineData>();

        public void Load(SerializedObject so)
        {
            var importer = so.targetObject as SVGImporter;
            var sprite = SVGImporter.GetImportedSprite(importer.assetPath);
            if (sprite == null)
                return;

            SpriteRect.name = sprite.name;

            int targetWidth;
            int targetHeight;
            importer.TextureSizeForSpriteEditor(sprite, out targetWidth, out targetHeight);
            SpriteRect.rect = new Rect(0, 0, targetWidth, targetHeight);
            var textureSize = new Vector2(targetWidth, targetHeight);

            var baseSP = so.FindProperty("m_SpriteData");
            var spriteRectSP = baseSP.FindPropertyRelative("SpriteRect");
            SpriteRect.border = Vector4.zero;
            SpriteRect.pivot = sprite.pivot / textureSize;
            var guidSP = spriteRectSP.FindPropertyRelative("m_SpriteID");
            SpriteRect.spriteID = new GUID(guidSP.stringValue);
        }
    }

    [Serializable]
    internal struct OutlineData
    {
        public Vector2[] vertices;
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

        public Sprite GetImportedSprite()
        {
            return SVGImporter.GetImportedSprite(GetImporter().assetPath);
        }

        public Vector2 GetTextureSize()
        {
            int targetWidth;
            int targetHeight;
            GetImporter().TextureSizeForSpriteEditor(GetImportedSprite(), out targetWidth, out targetHeight);
            return new Vector2(targetWidth, targetHeight);
        }
    }

    internal class SVGTextureDataProvider : SVGDataProviderBase, ITextureDataProvider
    {
        private float m_TextureScale = 1.0f;

        public SVGTextureDataProvider(SVGImporter importer) : base(importer)
        { }

        public Texture2D texture {
            get
            {
                // The "previewTexture" property should be used, but since it was only introduced in 2018.2,
                // we fallback to the "texture" property, which works fine for textureless SVG sprites.
                // SVG sprite with textures/gradients will be displayed wrong in 2018.1.
                if (typeof(ITextureDataProvider).GetProperty("previewTexture") == null)
                {
                    return previewTexture;
                }
                return null;
            }
        }

        private Texture2D m_PreviewTexture;
        public Texture2D previewTexture {
            get
            {
                if (m_PreviewTexture == null)
                {
                    var sprite = GetImportedSprite();
                    var size = ((Vector2)sprite.bounds.size) * sprite.pixelsPerUnit;

                    const float kMinTextureSize = 2048.0f;
                    if (size.x < kMinTextureSize && size.y < kMinTextureSize)
                    {
                        var maxSize = Math.Max(size.x, size.y);
                        m_TextureScale = kMinTextureSize / maxSize;
                    }

                    m_PreviewTexture = VectorUtils.RenderSpriteToTexture2D(sprite, (int)(size.x * m_TextureScale), (int)(size.y * m_TextureScale), SVGImporter.GetSVGSpriteMaterial(sprite), 4);
                }
                return m_PreviewTexture;
            }
        }

        public void GetTextureActualWidthAndHeight(out int width, out int height)
        {
            GetImporter().TextureSizeForSpriteEditor(GetImportedSprite(), out width, out height);
        }

        private Texture2D m_ReadableTexture;
        public Texture2D GetReadableTexture2D()
        {
            if (m_ReadableTexture == null)
            {
                var sprite = GetImportedSprite();
                var size = ((Vector2)sprite.bounds.size) * sprite.pixelsPerUnit;
                m_ReadableTexture = VectorUtils.RenderSpriteToTexture2D(sprite, (int)size.x, (int)size.y, SVGImporter.GetSVGSpriteMaterial(sprite), 4);                
            }
            return m_ReadableTexture;
        }
    }

    internal class SVGPhysicsOutlineDataProvider : SVGDataProviderBase, ISpritePhysicsOutlineDataProvider
    {
        public SVGPhysicsOutlineDataProvider(SVGImporter importer) : base(importer)
        { }

        List<Vector2[]> ISpritePhysicsOutlineDataProvider.GetOutlines(GUID guid)
        {
            return GetSVGSpriteData().PhysicsOutlines.Select(x => x.vertices.ToArray()).ToList();
        }

        void ISpritePhysicsOutlineDataProvider.SetOutlines(GUID guid, List<Vector2[]> data)
        {
            GetSVGSpriteData().PhysicsOutlines = data.Select(x => new OutlineData() { vertices = x }).ToList();
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