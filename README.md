# UEdConverter

Converts t3d brush files from Unreal Engine 1.5 to obj format and vice-versa. Allows to edit Unreal's brushes in regular 3D editors.

### What it does:
- It converts UEd brushes
- Both polygons and UV coordinates are preserved
### What it doesn't:
- No texture handling. You need to export/import textures separately and assign correct textures to polygons manually
- Program does not support map export/import as UEd uses CSG cuts and boolean operations and those are not precomputed in such export.

---

<img alt="preview" src="./preview.png">


## How to
 - Open executable
 - Click "File" and select source for conversion
 - Click "Destination" and select destination file for conversion
 - Click Convert

```diff
-!Conversion may brake some models. Overriding files is inadvisable!
```

## Examples:

### Cutting piece of map geometry

1. In editor create brush (right click on shape toolbox to select size) in shape and size you wish, and position it on level geometry you are interested in.

<img height="600px" src="./edit1.jpg" alt="creating brush" />

2. Select intersect option(1) and then export brush(3)

<img height="600px" src="./edit2.jpg" alt="intersect option and export of brush" />

4. Convert Files
5. Import into 3d program (here scale during import is set to 0.01)

<img height="400px" src="./edit3.jpg" alt="imported file in blender" />

### Exporting single brush

1. Select brush(on picture blue mesh), right-click and select "Polygons->To Brush"
2. Export as in prevoius example

<img height="500px" src="./edit4.jpg" alt="imported file in blender" />

