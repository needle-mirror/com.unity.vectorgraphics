using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.VectorGraphics
{
    internal static partial class InternalBridge
    {
        // Sprite.Create(Rect, Vector2, float, Texture2D) is internal
        internal static Sprite CreateSprite(Rect rect, Vector2 pivot, float svgPixelsPerUnit, Texture2D texture)
        {
            return Sprite.Create(rect, pivot, svgPixelsPerUnit, texture);
        }
   }
}