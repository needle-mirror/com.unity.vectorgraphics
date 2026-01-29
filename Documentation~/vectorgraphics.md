# About Vector Graphics

## Unity 6.3 and Later

Unity 6.3 includes a built-in Vector Graphics module with native support for importing SVG files and working with vector graphics. The built-in module supports:

- **UI Toolkit Vector Images**: Import SVG files for use in UI Toolkit.
- **Texture2D**: Import SVG files as Texture2D assets.
- **Vector Graphics APIs**: Full programmatic vector graphics API.

### When to install the Vector Graphics package (3.0.0-preview)

Install this package only if you require any of the following import types:

- **Sprite**: Import SVG files as Unity Sprites for use with Sprite Renderer.
- **uGUI**: Import SVG files as SVGImages for use with Unity's uGUI system.

If you only use UI Toolkit or Texture2D imports, the built-in module is sufficient, the built-in module is sufficient and the package is not required. Refer to [Work with vector graphics](xref:uie-work-with-vector-graphics) for more information.


## Earlier Unity versions

For Unity versions prior to Unity 6.3, use Vector Graphics package version `2.0.0-preview`, which provides the complete implementation, including SVG import and the Vector Graphics APIs.

## About this package

For Unity 6.3 and later, this package works on top of the built-in Vector Graphics module to provide additional import types:

- **Sprite Import**: Import SVG files as Unity Sprites for use with Sprite Renderer.
- **uGUI Import**: Import SVG files as UI Images for Unity's uGUI system.

The SVG importer follows a subset of the [SVG 1.1 specification](https://www.w3.org/TR/SVG11/). When importing SVG files as Sprites or uGUI images, the vector data is tessellated into triangles to generate the appropriate Unity assets.

For Unity versions prior to 6.3, this package provides the complete vector graphics implementation including SVG import and vector graphics APIs.

## Requirements and version guide

| Unity Version | Recommended Package Version | Notes |
|---------------|----------------------------|-------|
| Unity 6.3+ | Built-in module | For UI Toolkit Vector Images and Texture2D |
| Unity 6.3+ | `3.0.0-preview` | For uGUI and Sprite support |
| Unity 2018.1 - Unity 6.2 | `2.0.0-preview` | Full vector graphics support |

## Migrate from earlier versions

When upgrading a project from Unity versions prior to 6.3 to Unity 6.3 or later, the migration to the built-in Vector Graphics module happens automatically. Unity will:

- Upgrade the package version to `3.0.0-preview`.
- Migrate existing SVG assets to use the built-in Vector Graphics module.
- Preserve your existing vector graphics functionality.
