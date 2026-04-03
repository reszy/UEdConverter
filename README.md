# UEdConverter

Converts t3d brush files from Unreal Engine 1.5 to obj format and vice-versa. Allows to edit Unreal's brushes in regular 3D editors.

### What it does:
- It converts UEd brushes
- Both polygons and UV coordinates are preserved
### What it doesn't:
- No texture handling. You need to export/import textures separately and assign correct textures to polygons manually
- Program does not support map export/import as UEd uses CSG (boolean operations to create geometry) and those are not precomputed in such file.

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

## Texture support
Experimental texture support is provided. If file `texture_dict.txt` is provided textures manually applied should have correct scaling (assuming correct texture is applied).

### How to do it:
Put the file `texture_dict.txt` in the same directory as exe

### Example file:
```
Lantern2 64x64
pmbLight3 64x64
pmbLight3or 64x64
pmbfloor-E 128x128
```

### What next?
In the brush file there is no connection to the texture other than simplified name so the dictionary (texture name to its dimensions) is neccessary. An exctractor for these data is needed but the texture support will stay limited as I don't have capacity to further reverse engineer utx file format.