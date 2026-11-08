"""Run with Blender --background --python build_assets.py. Writes sibling assets only."""
import bpy
import json
import os
from mathutils import Vector

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
NAME = "HierarchyWindTree"
TRUNK_SEGMENTS = 16
BOXES = [((0, 0, 0), (0, 3, 0), .16),
         ((0, 1.20, 0), (-1.05, 2.05, .10), .11),
         ((0, 1.78, 0), (1.0, 2.62, -.12), .11)]

def main():
    vertices, faces, records = [], [], []
    for box_index, (p, t, width) in enumerate(BOXES):
        pivot, tip = Vector(p), Vector(t)
        direction = (tip - pivot).normalized()
        side = Vector((0, 0, 1)).cross(direction).normalized()
        depth = direction.cross(side).normalized()
        start = len(vertices)
        segments = TRUNK_SEGMENTS if box_index == 0 else 1
        for ring in range(segments + 1):
            center = pivot.lerp(tip, ring / segments)
            for a, b in ((-1, -1), (1, -1), (1, 1), (-1, 1)):
                v = center + a * width * side + b * width * depth
                vertices.append(tuple(v))
                records.append({"pivot": list(p), "tip": list(t),
                                "weight": 0 if box_index == 0 else 1})
        local_faces = [(0, 3, 2, 1)]
        last = segments * 4
        local_faces.append((last, last+1, last+2, last+3))
        for segment in range(segments):
            lo, hi = segment * 4, (segment + 1) * 4
            for corner in range(4):
                following = (corner + 1) % 4
                local_faces.append((lo+corner, lo+following, hi+following, hi+corner))
        # Orient each closed island independently; face counts now differ by island.
        box_center = (pivot + tip) * .5
        for face in local_faces:
            face = tuple(start + i for i in face)
            center = sum((Vector(vertices[i]) for i in face), Vector()) / 4
            a, b, c = [Vector(vertices[i]) for i in face[:3]]
            if (b-a).cross(c-a).dot(center-box_center) < 0:
                face = tuple(reversed(face))
            faces.append(face)
    triangles = []
    for a,b,c,d in faces:
        triangles.extend([a,b,c,a,c,d])
    for folder in ("Models", "Textures"):
        os.makedirs(os.path.join(ROOT, folder), exist_ok=True)
    manifest = {"vertices": [{"x":v[0],"y":v[1],"z":v[2]} for v in vertices],
                "triangles": triangles,
                "records": [{"pivot":dict(zip("xyz",r["pivot"])),
                             "tip":dict(zip("xyz",r["tip"])),"weight":r["weight"]} for r in records]}
    with open(os.path.join(ROOT,"Models",NAME+".json"), "w", encoding="utf-8") as stream:
        json.dump(manifest, stream, indent=2)
    # Blender Z-up coordinates; canonical JSON and texture are already Unity Y-up.
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    mesh = bpy.data.meshes.new(NAME)
    mesh.from_pydata([(x,-z,y) for x,y,z in vertices], [], faces)
    mesh.update()
    bounds_min = [min(v[k] for v in vertices) for k in range(3)]
    bounds_size = [max(max(v[k] for v in vertices)-bounds_min[k], .0001) for k in range(3)]
    colors = mesh.color_attributes.new(name="PivotWeight", type="FLOAT_COLOR", domain="CORNER")
    for loop in mesh.loops:
        r = records[loop.vertex_index]
        colors.data[loop.index].color = tuple((r["pivot"][k]-bounds_min[k])/bounds_size[k] for k in range(3)) + (r["weight"],)
    obj = bpy.data.objects.new(NAME, mesh)
    bpy.context.collection.objects.link(obj)
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.export_scene.fbx(filepath=os.path.join(ROOT,"Models",NAME+".fbx"),
        use_selection=True, object_types={"MESH"}, axis_forward="-Z", axis_up="Y",
        apply_unit_scale=True, apply_scale_options="FBX_SCALE_UNITS",
        mesh_smooth_type="FACE", colors_type="LINEAR", add_leaf_bones=False, bake_anim=False)
    image = bpy.data.images.new(NAME+"_Data", width=1, height=len(vertices),
                                alpha=True, float_buffer=True)
    image.colorspace_settings.name = "Non-Color"
    pixels = []
    for record in records:
        pixels.extend([(record["tip"][k] - bounds_min[k]) / bounds_size[k]
                       for k in range(3)] + [0])
    image.pixels.foreach_set(pixels)
    image.filepath_raw = os.path.join(ROOT,"Textures",NAME+"_Data.exr")
    image.file_format = "OPEN_EXR"
    image.save()
    source_folder = os.path.abspath(os.path.join(ROOT,"../..","SourceAssets","Wind","HierarchyWind"))
    os.makedirs(source_folder, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(source_folder,NAME+".blend"))
    print("Authored FBX, blend, canonical JSON, and floating-point data EXR.")

if __name__ == "__main__":
    main()
