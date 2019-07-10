using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Unity.Collections;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine.Experimental.U2D;
using UnityEngine.Experimental.Rendering;

namespace Unity.VectorGraphics.Editor
{
    [CustomEditor(typeof(SVGImporter))]
    [CanEditMultipleObjects]
    internal class SVGImporterEditor : ScriptedImporterEditor
    {
        private enum SettingsType
        {
            Basic,
            Advanced
        }

        private SerializedProperty m_PixelsPerUnit;
        private SerializedProperty m_GradientResolution;
        private SerializedProperty m_Alignment;
        private SerializedProperty m_CustomPivot;
        private SerializedProperty m_AdvancedMode;
        private SerializedProperty m_StepDistance;
        private SerializedProperty m_SamplingStepDistance;
        private SerializedProperty m_PredefinedResolutionIndex;
        private SerializedProperty m_TargetResolution;
        private SerializedProperty m_ResolutionMultiplier;
        private SerializedProperty m_MaxCordDeviationEnabled;
        private SerializedProperty m_MaxCordDeviation;
        private SerializedProperty m_MaxTangentAngleEnabled;
        private SerializedProperty m_MaxTangentAngle;

        private readonly GUIContent m_PixelsPerUnitText = new GUIContent("Pixels Per Unit", "How many pixels in the SVG correspond to one unit in the world.");
        private readonly GUIContent m_GradientResolutionText = new GUIContent("Gradient Resolution", "Size of each rasterized gradient in pixels. Higher values consume memory but result in more accurate gradients.");
        private readonly GUIContent m_AlignmentText = new GUIContent("Pivot", "Sprite pivot point in its local space.");
        private readonly GUIContent m_CustomPivotText = new GUIContent("Custom Pivot");
        private readonly GUIContent m_SettingsText = new GUIContent("Tessellation Settings");
        private readonly GUIContent m_TargetResolutionText = new GUIContent("Target Resolution", "Target resolution below which the sprite will not look tessellated.");
        private readonly GUIContent m_CustomTargetResolutionText = new GUIContent("Custom Target Resolution");
        private readonly GUIContent m_ResolutionMultiplierText = new GUIContent("Zoom Factor", "Target zoom factor for which the SVG asset should not look tessellated.");
        private readonly GUIContent m_StepDistanceText = new GUIContent("Step Distance", "Distance at which vertices will be generated along the paths. Lower values will result in a more dense tessellation.");
        private readonly GUIContent m_SamplingStepDistanceText = new GUIContent("Sampling Steps", "Number of samples evaluated on paths. Higher values give more accurate results (but takes longer).");
        private readonly GUIContent m_MaxCordDeviationEnabledText = new GUIContent("Max Cord Enabled", "Enables the \"max cord deviation\" tessellation test.");
        private readonly GUIContent m_MaxCordDeviationText = new GUIContent("Max Cord Deviation", "Distance on the cord to a straight line between two points after which more tessellation will be generated.");
        private readonly GUIContent m_MaxTangentAngleEnabledText = new GUIContent("Max Tangent Enabled", "Enables the \"max tangent angle\" tessellation test.");
        private readonly GUIContent m_MaxTangentAngleText = new GUIContent("Max Tangent Angle", "Max tangent angle (in degrees) after which more tessellation will be generated.");

        private readonly GUIContent[] m_AlignmentOptions = new GUIContent[]
        {
            new GUIContent("Center"),
            new GUIContent("Top Left"),
            new GUIContent("Top"),
            new GUIContent("Top Right"),
            new GUIContent("Left"),
            new GUIContent("Right"),
            new GUIContent("Bottom Left"),
            new GUIContent("Bottom"),
            new GUIContent("Bottom Right"),
            new GUIContent("SVG Origin"),
            new GUIContent("Custom")
        };

        private readonly GUIContent[] m_SettingOptions = new GUIContent[]
        {
            new GUIContent("Basic"),
            new GUIContent("Advanced")
        };

        private readonly GUIContent[] m_TargetResolutionOptions = new GUIContent[]
        {
            new GUIContent("2160p (4K)"),
            new GUIContent("1080p"),
            new GUIContent("720p"),
            new GUIContent("480p"),
            new GUIContent("Custom")
        };

        public override void OnEnable()
        {
            m_PixelsPerUnit = serializedObject.FindProperty("SvgPixelsPerUnit");
            m_GradientResolution = serializedObject.FindProperty("GradientResolution");
            m_Alignment = serializedObject.FindProperty("Alignment");
            m_CustomPivot = serializedObject.FindProperty("CustomPivot");
            m_AdvancedMode = serializedObject.FindProperty("AdvancedMode");
            m_PredefinedResolutionIndex = serializedObject.FindProperty("PredefinedResolutionIndex");
            m_TargetResolution = serializedObject.FindProperty("TargetResolution");
            m_ResolutionMultiplier = serializedObject.FindProperty("ResolutionMultiplier");
            m_StepDistance = serializedObject.FindProperty("StepDistance");
            m_SamplingStepDistance = serializedObject.FindProperty("SamplingStepDistance");
            m_MaxCordDeviationEnabled = serializedObject.FindProperty("MaxCordDeviationEnabled");
            m_MaxCordDeviation = serializedObject.FindProperty("MaxCordDeviation");
            m_MaxTangentAngleEnabled = serializedObject.FindProperty("MaxTangentAngleEnabled");
            m_MaxTangentAngle = serializedObject.FindProperty("MaxTangentAngle");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(m_PixelsPerUnit, m_PixelsPerUnitText);
            EditorGUILayout.PropertyField(m_GradientResolution, m_GradientResolutionText);

            m_Alignment.intValue = EditorGUILayout.Popup(m_AlignmentText, m_Alignment.intValue, m_AlignmentOptions);

            if (m_Alignment.intValue == (int)VectorUtils.Alignment.Custom)
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(m_CustomPivot, m_CustomPivotText);
                GUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();

            var settingType = EditorGUILayout.Popup(m_SettingsText, m_AdvancedMode.boolValue ? 1 : 0, m_SettingOptions);
            m_AdvancedMode.boolValue = (settingType == 1);

            ++EditorGUI.indentLevel;

            if (m_AdvancedMode.boolValue)
            {
                EditorGUILayout.PropertyField(m_StepDistance, m_StepDistanceText);
                m_StepDistance.floatValue = Mathf.Max(0.0f, m_StepDistance.floatValue);

                EditorGUILayout.PropertyField(m_SamplingStepDistance, m_SamplingStepDistanceText);
                m_SamplingStepDistance.floatValue = Mathf.Clamp(m_SamplingStepDistance.floatValue, 3.0f, 1000.0f);

                m_MaxCordDeviationEnabled.boolValue = EditorGUILayout.Toggle(m_MaxCordDeviationEnabledText, m_MaxCordDeviationEnabled.boolValue);
                using (new EditorGUI.DisabledScope(!m_MaxCordDeviationEnabled.boolValue))
                {
                    EditorGUILayout.PropertyField(m_MaxCordDeviation, m_MaxCordDeviationText);
                    m_MaxCordDeviation.floatValue = Mathf.Max(0.0f, m_MaxCordDeviation.floatValue);
                }

                m_MaxTangentAngleEnabled.boolValue = EditorGUILayout.Toggle(m_MaxTangentAngleEnabledText, m_MaxTangentAngleEnabled.boolValue);
                using (new EditorGUI.DisabledScope(!m_MaxTangentAngleEnabled.boolValue))
                {
                    EditorGUILayout.PropertyField(m_MaxTangentAngle, m_MaxTangentAngleText);
                    m_MaxTangentAngle.floatValue = Mathf.Clamp(m_MaxTangentAngle.floatValue, 0.0f, 90.0f);
                }
            }
            else
            {
                m_PredefinedResolutionIndex.intValue = EditorGUILayout.Popup(m_TargetResolutionText, m_PredefinedResolutionIndex.intValue, m_TargetResolutionOptions);
                if (m_PredefinedResolutionIndex.intValue == (int)SVGImporter.PredefinedResolution.Custom)
                {
                    EditorGUILayout.PropertyField(m_TargetResolution, m_CustomTargetResolutionText);
                    m_TargetResolution.intValue = (int)Mathf.Max(1, m_TargetResolution.intValue);
                }
                else
                {
                    m_TargetResolution.intValue = TargetResolutionFromPredefinedValue((SVGImporter.PredefinedResolution) m_PredefinedResolutionIndex.intValue);
                }
                EditorGUILayout.PropertyField(m_ResolutionMultiplier, m_ResolutionMultiplierText);
                m_ResolutionMultiplier.floatValue = Mathf.Clamp(m_ResolutionMultiplier.floatValue, 1.0f, 100.0f);
            }

            --EditorGUI.indentLevel;

            EditorGUILayout.Space();

            GUILayout.BeginVertical();
            {
                var labelStyle = EditorStyles.wordWrappedLabel;
                labelStyle.fontStyle = FontStyle.Bold;
                labelStyle.alignment = TextAnchor.UpperCenter;
                GUILayout.Label(GetStatsString(), labelStyle);
            }
            GUILayout.EndVertical();

            EditorGUILayout.Space();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Sprite Editor"))
            {
                var spriteWindow = Type.GetType("UnityEditor.SpriteEditorWindow, UnityEditor", true);
                var getWindowMethod = spriteWindow.GetMethod("GetWindow", BindingFlags.Public | BindingFlags.Static);
                getWindowMethod.Invoke(null, null);
            }
            GUILayout.EndHorizontal();

            ApplyRevertGUI();
        }

        private int TargetResolutionFromPredefinedValue(SVGImporter.PredefinedResolution resolution)
        {
            switch (resolution)
            {
            case SVGImporter.PredefinedResolution.Res_2160p: return 2160;
            case SVGImporter.PredefinedResolution.Res_1080p: return 1080;
            case SVGImporter.PredefinedResolution.Res_720p:  return 720;
            case SVGImporter.PredefinedResolution.Res_480p:  return 480;
            default: return 1080;
            }
        }

        public override bool HasPreviewGUI()
        {
            return true;
        }

        public override Texture2D RenderStaticPreview(string assetPath, UnityEngine.Object[] subAssets, int width, int height)
        {
            var sprite = SVGImporter.GetImportedSprite(assetTarget);
            if (sprite == null)
                return null;

            return BuildPreviewTexture(sprite, width, height);
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            background.Draw(r, false, false, false, false);

            var sprite = SVGImporter.GetImportedSprite(assetTarget);
            if (sprite == null)
                return;

            float zoomLevel = Mathf.Min(r.width / sprite.rect.width, r.height / sprite.rect.height);
            Rect wantedRect = new Rect(r.x, r.y, sprite.rect.width * zoomLevel, sprite.rect.height * zoomLevel);
            wantedRect.center = r.center;

            var previewTex = BuildPreviewTexture(sprite, (int)wantedRect.width, (int)wantedRect.height);
            if (previewTex != null)
            {
                EditorGUI.DrawTextureTransparent(r, previewTex, ScaleMode.ScaleToFit);
                UnityEngine.Object.DestroyImmediate(previewTex);
            }
        }

        internal static Texture2D BuildPreviewTexture(Sprite sprite, int width, int height)
        {
            return VectorUtils.RenderSpriteToTexture2D(sprite, width, height, SVGImporter.GetSVGSpriteMaterial(sprite), 4);
        }

        private string GetStatsString()
        {
            var sprite = SVGImporter.GetImportedSprite(assetTarget);
            int vertexCount = sprite.vertices.Length;
            int indexCount = sprite.triangles.Length;

            var stats = "" + vertexCount + " Vertices (Pos";

            int vertexSize = sizeof(float) * 2;
            if (sprite.HasVertexAttribute(VertexAttribute.Color))
            {
                stats += ", Col";
                vertexSize += 4;
            }
            if (sprite.HasVertexAttribute(VertexAttribute.TexCoord0))
            {
                stats += ", TexCoord0";
                vertexSize += sizeof(float) * 2;
            }
            if (sprite.HasVertexAttribute(VertexAttribute.TexCoord1))
            {
                stats += ", TexCoord1";
                vertexSize += sizeof(float) * 2;
            }
            if (sprite.HasVertexAttribute(VertexAttribute.TexCoord2))
            {
                stats += ", TexCoord2";
                vertexSize += sizeof(float) * 2;
            }

            stats += ") " + HumanReadableSize(vertexSize * vertexCount + indexCount * 2);

            return stats;
        }

        private static string HumanReadableSize(int bytes)
        {
            var units = new string[] { "B", "KB", "MB", "GB", "TB" };

            int order = 0;
            while (bytes >= 2014 && order < units.Length-1) {
                ++order;
                bytes /= 1024;
            }

            if (order >=  units.Length)
                return "" + bytes;

            return String.Format("{0:0.#} {1}", bytes, units[order]);
        }
    }
}
