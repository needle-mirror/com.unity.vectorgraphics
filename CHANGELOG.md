## [1.0.0-preview.22] - 2018-11-16

### Fixed precision issues with dashed strokes
### Fixed modifying node hierarchy while iterating through it
### Fixed CSS data parsing
### Clearing temp render texture when expanding edges
### Proper support for styling in symbols
### Fixed instancing for gradient shader

## [1.0.0-preview.21] - 2018-10-23

### Added support for borders (slices) for textured sprites
### Fixed viewport clipping working when viewBox is applied
### Silenced obsolete warnings because of WWW usage
### Fixed dark outlines when rendering to texture
### Fixed alpha-blending in VectorGradient.shader
### SVGImageEditor is now fallback custom editor

## [1.0.0-preview.20] - 2018-09-26

### Fixed sprite value not being set in SVGImage's sprite property
### Removed "Per-Axis" texture wrap mode

## [1.0.0-preview.19] - 2018-09-24

### Using viewBox for relative coordinates, when available
### Fixed issue with gradient user-units when no viewBox is specified

## [1.0.0-preview.18] - 2018-09-21

### Improved texture import editor. Better basic tessellation defaults.
### Enabled GPU instancing, _RendererColor works out-of-the-box
### Made the auto-computed tessellation options less aggressive
### Allowing different width/height when importing to a texture
### Fixed flipped winding order when flipYAxis is false
### Moved external libraries to their own namespaces
### Moved the sprite stats over the preview
### Filling atlas with opaque black to help with SVG sprite picking
### Support for sample count (for import-to-texture)
### Fixed SVGImageEditor for 2018.1
### Setting DtdProcessing to ignore

## [1.0.0-preview.17] - 2018-09-13

### Support for sprite mesh type on textured sprites

## [1.0.0-preview.16] - 2018-09-13

### Fixed relative positioning with viewBox
### Multiple object editing improvements
### Fixed elliptical-arc-to error with large sweep angles
### Fixed polygon winding after transform
### Fixed <use> always overriding fill/stroke even when not set
### Fixed "ProhibitDtd" obsolete warning on .NET 4.x backend
### Node-by-id support
### Added SVGImage for Canvas UI
### Preserve viewport option
### Support for auto-generate physics outline
### Setting Closed=true closes the path connecting the last segment to the first instead of a straight line
### Fixed issues with symbols and patterns usage
### First iteration of "import to texture" feature
### Improved SVGOrigin and pivot support
### Deprecated Rectangle, Path, Filled and IDrawable. Only Shape remains.
### Added support for flipYAxis in FillMesh method
### Fixed support for empty 'd' elements
### Fixed issue when symbols are defined after <use>
### Fixed invalid SVG Origin when Y-axis is fipped
### Fixed sprite editor align/pivot to not interfere with SVG origin value
### Fixed missing Apply() after atlas generation

## [1.0.0-preview.15] - 2018-07-18

### Updated CHANGELOG.md

## [1.0.0-preview.14] - 2018-07-17

### Taking pixels-per-unit into account to compute tessellation settings
### Fixed rgb() color attributes not parsed properly
### Early exit when trying to tessellate paths without enough segments
### Fixed viewbox computation that were lost during tessellation
### Fixed namespace issues with 2018.3+
### Added QuadraticToCubic helper method
### Skip stroke tessellation if the width is 0

## [1.0.0-preview.13] - 2018-06-11

### Elements with display:none are not displayed anymore
### Showing imported sprites stats
### Fixed parse issue when loading an unsupported texture from the image tag

## [1.0.0-preview.12] - 2018-06-07

### Using culture invariant float parsing
### Fixed import error when using percentage sizes in svg tag

## [1.0.0-preview.11] - 2018-06-05

### Fixed some precision issues
### More conservative processing of 'none' for 'stroke-dasharray'
### Revert "Fixed handling of 'none' styles"

## [1.0.0-preview.10] - 2018-05-23

### Adjusting the triangle's winding order after scene tessellation

## [1.0.0-preview.9] - 2018-05-15

### Fixed handling of 'none' styles
### Renamed Third-Party Notices

## [1.0.0-preview.8] - 2018-05-05

### Support for multiple SVG editing
### Updated documentation after docs team revision

## [1.0.0-preview.7] - 2018-04-26

### Optimized path for convex shapes
### Fixed SVG StreamReader not being closed
### Fixed polyline corners

## [1.0.0-preview.6] - 2018-04-24

### Physics outline fixes and using preview texture for Sprite Editor, when available
### Improved sampling step distance tooltip text
### Removed skin-based animation tools

## [1.0.0-preview.5] - 2018-04-18

### Added Third-Party Notices
### Added MakeArc_MakesArcInClockwiseDirection test

## [1.0.0-preview.4] - 2018-04-13

### MakeArc now returns a BezierPathSegment[] instead of BezierSegment[].  Added BezierSegmentsToPath API.
### Using the new code naming conventions (CamelCase for properties)

## [1.0.0-preview.3] - 2018-04-09

### Exposed BuildRectangleContour API

## [1.0.0-preview.2] - 2018-04-05

### Moved SVGParser to Unity.VectorGraphics namespace

## [1.0.0-preview.1] - 2018-04-04

### Initial release