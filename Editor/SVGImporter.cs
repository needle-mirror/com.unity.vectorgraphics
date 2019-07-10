using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.U2D;
using UnityEngine.Experimental.U2D;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine.Experimental.Rendering;

namespace Unity.VectorGraphics.Editor
{
    /// <summary>The SVG importer class.</summary>
    [Serializable]
    [ScriptedImporter(1, "svg")]
    public class SVGImporter : ScriptedImporter, ISpriteEditorDataProvider
    {
        internal static readonly string k_PackagePath = "Packages/com.unity.vectorgraphics";

        /// <summary>The number of pixels per Unity units.</summary>
        public float SvgPixelsPerUnit = 100.0f;

        /// <summary>Maximum resolution for gradient texture.</summary>
        public UInt16 GradientResolution = 128;

        /// <summary>The SVG sprite alignement.</summary>
        public VectorUtils.Alignment Alignment;

        /// <summary>The custom pivot, when alignement is "Custom".</summary>
        public Vector2 CustomPivot;

        /// <summary>Use advanced settings.</summary>
        public bool AdvancedMode = false;

        /// <summary>The predefined resolution used, when not in advanced mode.</summary>
        public int PredefinedResolutionIndex = 1;

        /// <summary>The target resolution on which this SVG is displayed.</summary>
        public int TargetResolution = 1080;

        /// <summary>An additional scale factor on the target resolution.</summary>
        public float ResolutionMultiplier = 1.0f;

        /// <summary>The uniform step distance used for tessellation.</summary>
        public float StepDistance = 10.0f;

        /// <summary>Number of samples evaluated on paths.</summary>
        public float SamplingStepDistance = 100.0f;

        /// <summary>Enables the "max coord deviation" constraint.</summary>
        public bool MaxCordDeviationEnabled = false;

        /// <summary>Distance on the cord to a straight line between two points after which more tessellation will be generated.</summary>
        public float MaxCordDeviation = 1.0f;

        /// <summary>Enables the "max tangent angle" constraint.</summary>
        public bool MaxTangentAngleEnabled = false;

        /// <summary>Max tangent angle (in degrees) after which more tessellation will be generated.</summary>
        public float MaxTangentAngle = 5.0f;

        [SerializeField]
        private SVGSpriteData m_SpriteData = new SVGSpriteData();
        internal SVGSpriteData GetSVGSpriteData() { return m_SpriteData; }

        internal enum PredefinedResolution
        {
            Res_2160p,
            Res_1080p,
            Res_720p,
            Res_480p,
            Custom
        }

        /// <summary>Imports an SVG asset</summary>
        /// <param name="ctx">The asset import context of the scripted importer</param>
        public override void OnImportAsset(AssetImportContext ctx)
        {
            SVGParser.SceneInfo sceneInfo;
            using (var stream = new StreamReader(ctx.assetPath))
                sceneInfo = SVGParser.ImportSVG(stream, 0, 1, (int)SvgPixelsPerUnit, (int)SvgPixelsPerUnit);

            if (sceneInfo.Scene == null || sceneInfo.Scene.Root == null)
                throw new Exception("Wowzers!");

            float stepDist = StepDistance;
            float samplingStepDist = SamplingStepDistance;
            float maxCord = MaxCordDeviationEnabled ? MaxCordDeviation : float.MaxValue;
            float maxTangent = MaxTangentAngleEnabled ? MaxTangentAngle : Mathf.PI * 0.5f;

            if (!AdvancedMode)
            {
                // Automatically compute sensible tessellation options from the
                // vector scene's bouding box and target resolution
                ComputeTessellationOptions(sceneInfo, TargetResolution, ResolutionMultiplier, out stepDist, out maxCord, out maxTangent);
            }

            var tessOptions = new VectorUtils.TessellationOptions();
            tessOptions.MaxCordDeviation = maxCord;
            tessOptions.MaxTanAngleDeviation = maxTangent;
            tessOptions.SamplingStepSize = 1.0f / (float)samplingStepDist;
            tessOptions.StepDistance = stepDist;

            var geometry = VectorUtils.TessellateScene(sceneInfo.Scene, tessOptions, sceneInfo.NodeOpacity);
            var sprite = VectorUtils.BuildSprite(geometry, SvgPixelsPerUnit, Alignment, CustomPivot, GradientResolution, true);

            OverridePhysicsShape(sprite);
            GenerateAsset(ctx, sprite);
        }

        private void OverridePhysicsShape(Sprite sprite)
        {
            var physicsDataProvider = (this as ISpriteEditorDataProvider).GetDataProvider<ISpritePhysicsOutlineDataProvider>();
            var outlines = physicsDataProvider.GetOutlines(m_SpriteData.SpriteRect.spriteID);

            int width;
            int height;
            TextureSizeForSpriteEditor(sprite, out width, out height);

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

            sprite.OverridePhysicsShape(outlines);
        }

        private void GenerateAsset(AssetImportContext ctx, Sprite sprite)
        {
            var name = System.IO.Path.GetFileNameWithoutExtension(ctx.assetPath);
            sprite.name = name + "Sprite";

            // Apply GUID from SpriteRect
            var so = new SerializedObject(sprite);
            so.FindProperty("m_SpriteID").stringValue = m_SpriteData.SpriteRect.spriteID.ToString();
            so.ApplyModifiedPropertiesWithoutUndo();

            ctx.AddObjectToAsset("sprite", sprite);

            Material mat = null;

            if (sprite.texture != null)
            {
                // When texture is present, use the VectorGradient shader
                mat = AssetDatabase.LoadMainAssetAtPath(k_PackagePath + "/Runtime/Materials/Unlit_VectorGradient.mat") as Material;
                sprite.texture.name = name + "Atlas";
            }
            else
            {
                mat = AssetDatabase.LoadMainAssetAtPath(k_PackagePath + "/Runtime/Materials/Unlit_Vector.mat") as Material;                
            }

            var gameObject = new GameObject(name);
            var spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.material = mat;

            if (sprite.texture != null)
                ctx.AddObjectToAsset("texAtlas", sprite.texture);

            ctx.AddObjectToAsset("gameObject", gameObject);
            ctx.SetMainObject(gameObject);
        }

        private void ComputeTessellationOptions(SVGParser.SceneInfo sceneInfo, int targetResolution, float multiplier, out float stepDist, out float maxCord, out float maxTangent)
        {
            var bbox = VectorUtils.ApproximateSceneNodeBounds(sceneInfo.Scene.Root);
            float maxDim = Mathf.Max(bbox.width, bbox.height);

            // The scene ratio gives a rough estimate of coverage % of the vector scene on the screen.
            // Higher values should result in a more dense tessellation.
            float sceneRatio = (targetResolution * multiplier) / maxDim;

            stepDist = float.MaxValue; // No need for uniform step distance
            maxCord = 0.5f / sceneRatio;
            maxTangent = 1.0f / sceneRatio;
        }

        internal static Sprite GetImportedSprite(string assetPath)
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            return GetImportedSprite(asset);
        }

        internal static Sprite GetImportedSprite(UnityEngine.Object asset)
        {
            var sprite = asset as Sprite;
            if (sprite == null)
            {
                var go = asset as GameObject;
                var sr = go != null ? go.GetComponent<SpriteRenderer>() : null;
                sprite = sr != null ? sr.sprite : null;
            }
            return sprite;
        }

        internal void TextureSizeForSpriteEditor(Sprite sprite, out int width, out int height)
        {
            var size = ((Vector2)sprite.bounds.size) * SvgPixelsPerUnit;
            width = (int)(size.x + 0.5f);
            height = (int)(size.y + 0.5f);
        }

        private static Material s_VectorMat = null;
        private static Material s_GradientMat = null;

        internal static Material GetSVGSpriteMaterial(Sprite sprite)
        {
            if (sprite == null)
                return null;
            
            Material mat = null;
            if (sprite.texture != null)
            {
                if (s_GradientMat == null)
                {
                    string gradientMatPath = "Packages/com.unity.vectorgraphics/Runtime/Materials/Unlit_VectorGradient.mat";
                    s_GradientMat = AssetDatabase.LoadMainAssetAtPath(gradientMatPath) as Material;
                }
                mat = new Material(s_GradientMat);
            }
            else
            {
                if (s_VectorMat == null)
                {
                    string vectorMatPath = "Packages/com.unity.vectorgraphics/Runtime/Materials/Unlit_Vector.mat";
                    s_VectorMat = AssetDatabase.LoadMainAssetAtPath(vectorMatPath) as Material;
                }
                mat = new Material(s_VectorMat);
            }
            return mat;
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
            get { return SvgPixelsPerUnit; }
        }
        
        /// <summary>Returns imported sprite</summary>
        public UnityEngine.Object targetObject
        {
            get { return GetImportedSprite(assetPath); }
        }

        /// <summary>Returns the sprite rectangles</summary>
        SpriteRect[] ISpriteEditorDataProvider.GetSpriteRects()
        {
            return new SpriteRect[] { m_SpriteData.SpriteRect };
        }

        /// <summary>Sets the sprite rectangles</summary>
        /// <param name="rects">The new sprite rectangles to use</param>
        void ISpriteEditorDataProvider.SetSpriteRects(SpriteRect[] rects)
        {
            if (rects.Length > 0)
                m_SpriteData.SpriteRect = rects[0];
        }

        /// <summary>Applies the modified SVG data</summary>
        void ISpriteEditorDataProvider.Apply()
        {
            var so = new SerializedObject(this);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Initializes the sprite editor data</summary>
        void ISpriteEditorDataProvider.InitSpriteEditorDataProvider()
        {
            var so = new SerializedObject(this);
            m_SpriteData.Load(so);
        }

        /// <summary>Gets the data provider for a given type</summary>
        T ISpriteEditorDataProvider.GetDataProvider<T>()
        {
            if (typeof(T) == typeof(ISpritePhysicsOutlineDataProvider))
            {
                return new SVGPhysicsOutlineDataProvider(this) as T;
            }
            if (typeof(T) == typeof(ITextureDataProvider))
            {
                return new SVGTextureDataProvider(this) as T;
            }
            else if (typeof(T) == typeof(ISpriteEditorDataProvider))
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
    }
}
