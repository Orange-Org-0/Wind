"""Read-only topology, FBX and matching Tip texture checks. Run with Blender."""
import bpy
import json
import os
from collections import Counter
from mathutils import Vector
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
with open(os.path.join(ROOT, "Models", "HierarchyWindTree.json"), encoding="utf-8") as stream:
    data = json.load(stream)
vertices, records, indices = data["vertices"], data["records"], data["triangles"]
assert len(vertices) == len(records) == 84
assert len(indices) == 468 and all(0 <= i < 84 for i in indices)
assert sum(r["weight"] == 0 for r in records) == 68
for start in (68, 76):
    branch = records[start:start+8]
    assert len(branch) == 8 and all(r == branch[0] for r in branch)
    assert branch[0]["weight"] == 1
assert len(set(round(v["y"], 6) for v in vertices[:68])) == 17
assert min(v["y"] for v in vertices[:68]) == 0
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=os.path.join(ROOT, "Models", "HierarchyWindTree.fbx"), colors_type="LINEAR")
objects = [o for o in bpy.data.objects if o.type == "MESH"]
assert len(objects) == 1
mesh = objects[0].data
assert len(mesh.vertices) == 84 and len(mesh.polygons) == 78
assert len(mesh.color_attributes) == 1 and len(mesh.uv_layers) == 0
adjacency = {i: set() for i in range(len(mesh.vertices))}
for edge in mesh.edges:
    a, b = edge.vertices
    adjacency[a].add(b)
    adjacency[b].add(a)
components = []
remaining = set(adjacency)
while remaining:
    pending = [remaining.pop()]
    component = []
    while pending:
        index = pending.pop()
        component.append(index)
        for neighbor in adjacency[index]:
            if neighbor in remaining:
                remaining.remove(neighbor)
                pending.append(neighbor)
    components.append(component)
assert sorted(map(len, components)) == [8, 8, 68]
edge_uses = Counter()
for face in mesh.polygons:
    ids = list(face.vertices)
    for a, b in zip(ids, ids[1:] + ids[:1]):
        edge_uses[tuple(sorted((a, b)))] += 1
    center = sum((mesh.vertices[i].co for i in ids), Vector()) / len(ids)
    component = next(c for c in components if ids[0] in c)
    island_center = sum((mesh.vertices[i].co for i in component), Vector()) / len(component)
    assert face.normal.dot(center-island_center) > 0
assert all(count == 2 for count in edge_uses.values())
image = bpy.data.images.load(os.path.join(ROOT, "Textures", "HierarchyWindTree_Data.exr"), check_existing=False)
image.colorspace_settings.name = "Non-Color"
assert list(image.size) == [1, 84] and image.is_float
pixels = list(image.pixels)
for i, record in enumerate(records):
    expected = [record["tip"][k] for k in "xyz"] + [0]
    assert all(abs(a-b) < .003 for a, b in zip(pixels[i*4:i*4+4], expected))
print("PASS: FBX 84 vertices / 78 quads / islands 68+8+8; 17 trunk rings; closed outward faces; 84 matching JSON records and 1x84 Tip texels.")