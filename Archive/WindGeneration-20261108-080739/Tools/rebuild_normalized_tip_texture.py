"""Blender --background --python this_file.py; only rewrites the data EXR.

The canonical JSON keeps raw object-space coordinates. RGB stores normalized Tip,
using exactly the same authored mesh bounds as Pivot in vertex colors. A is zero.
"""
import json
import os

import bpy


def main():
    root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    with open(os.path.join(root, "Models", "HierarchyWindTree.json"), encoding="utf-8") as stream:
        source = json.load(stream)
    vertices = source["vertices"]
    records = source["records"]
    assert vertices and len(vertices) == len(records), "Inconsistent source records"
    minimum = [min(v[k] for v in vertices) for k in "xyz"]
    size = [max(max(v[k] for v in vertices) - minimum[i], 0.0001)
            for i, k in enumerate("xyz")]
    pixels = []
    for record in records:
        encoded = [(record["tip"][k] - minimum[i]) / size[i]
                   for i, k in enumerate("xyz")]
        assert all(0 <= value <= 1 for value in encoded), "Tip outside authored bounds"
        pixels.extend(encoded + [0.0])

    path = os.path.join(root, "Textures", "HierarchyWindTree_Data.exr")
    image = bpy.data.images.new("NormalizedTipData", width=1, height=len(records),
                                alpha=True, float_buffer=True)
    image.colorspace_settings.name = "Non-Color"
    image.pixels.foreach_set(pixels)
    image.filepath_raw = path
    image.file_format = "OPEN_EXR"
    image.save()
    bpy.data.images.remove(image)

    # Reload the saved file and verify every row, including float-format quantization.
    actual = bpy.data.images.load(path, check_existing=False)
    actual.colorspace_settings.name = "Non-Color"
    assert tuple(actual.size) == (1, len(records)), "Unexpected texture dimensions"
    readback = list(actual.pixels)
    assert all(0 <= value <= 1 for value in readback), "Texture values outside 0..1"
    max_error = 0.0
    for row, record in enumerate(records):
        for axis, key in enumerate("xyz"):
            decoded = readback[row * 4 + axis] * size[axis] + minimum[axis]
            max_error = max(max_error, abs(decoded - record["tip"][key]))
        assert abs(readback[row * 4 + 3]) < 1e-6, "Reserved alpha must remain zero"
    assert max_error < 0.002, "Tip round-trip error too large"
    print("NORMALIZED_TIP_TEXTURE", tuple(actual.size),
          "range", (min(readback), max(readback)), "max_decode_error", max_error)
    bpy.data.images.remove(actual)


if __name__ == "__main__":
    main()
