using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.VectorGraphics
{
#if UNITY_2019_3_OR_NEWER
    internal static partial class InternalBridge
    {
        // UnityEngine.UIElements.VectorImage is internal, so this method needs to be part of the internal bridge for now.

        internal struct VectorImageVertexBridge
        {
            public Vector3 position;
            public Color32 tint;
            public Vector2 uv;
            public uint settingIndex;
        }

        internal struct GradientSettingsBridge
        {
            public GradientTypeBridge gradientType;
            public AddressModeBridge addressMode;
            public Vector2 radialFocus;
            public RectInt location;
        }

        internal enum GradientTypeBridge
        {
            Linear = 0,
            Radial = 1
        }

        internal enum AddressModeBridge
        {
            Wrap = 0,
            Clamp = 1,
            Mirror = 2
        }

        internal static UnityEngine.Object MakeVectorImageAsset(IEnumerable<VectorImageVertexBridge> vertices, IEnumerable<UInt16> indices, Texture2D atlas, IEnumerable<GradientSettingsBridge> settings, Rect rect)
        {
            var vectorImage = ScriptableObject.CreateInstance<VectorImage>();
            vectorImage.vertices = vertices.Select(v => new VectorImageVertex() {
                    position = v.position,
                    tint = v.tint,
                    uv = v.uv,
                    settingIndex = v.settingIndex
                }).ToArray();
            vectorImage.indices = indices.ToArray();
            vectorImage.atlas = atlas;
            vectorImage.settings = settings.Select(s => new GradientSettings() {
                    gradientType = (GradientType)s.gradientType,
                    addressMode = (AddressMode)s.addressMode,
                    radialFocus = s.radialFocus,
                    location = s.location
                }).ToArray();
            vectorImage.size = rect.size;
            return vectorImage;
        }

        internal static bool GetDataFromVectorImage(UnityEngine.Object o, ref Vector2[] vertices, ref UInt16[] indices, ref Vector2[] uvs, ref Color[] colors, ref Vector2[] settingIndices, ref GradientSettingsBridge[] settings, ref Texture2D texture, ref Vector2 size)
        {
            var vi = o as VectorImage;
            if (vi == null)
                return false;
            
            vertices = vi.vertices.Select(v => (Vector2)v.position).ToArray();
            indices = vi.indices;
            uvs = vi.vertices.Select(v => v.uv).ToArray();
            colors = vi.vertices.Select(v => (Color)v.tint).ToArray();
            settingIndices = vi.atlas != null ? vi.vertices.Select(v => new Vector2(v.settingIndex, 0)).ToArray() : null;
            texture = vi.atlas;
            size = vi.size;

            settings = vi.settings.Select(s => new GradientSettingsBridge() {
                gradientType = (GradientTypeBridge)s.gradientType,
                addressMode = (AddressModeBridge)s.addressMode,
                radialFocus = s.radialFocus,
                location = s.location
            }).ToArray();

            return true;
        }
   }
#endif
}