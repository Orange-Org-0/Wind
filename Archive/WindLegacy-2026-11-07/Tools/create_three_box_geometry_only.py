import bpy
import os
from mathutils import Vector

OUTPUT_PATH = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "Models", "MultiLevelWindTestTree.fbx")
OBJECT_NAME = "MultiLevelWindTestTree"

def add_box(vertices, faces, pivot, tip, half_width, half_depth):
    pivot, tip = Vector(pivot), Vector(tip)
    direction = (tip - pivot).normalized()
    side = Vector((0.0, 1.0, 0.0)).cross(direction).normalized()
    depth = direction.cross(side).normalized()
    start = len(vertices)
    for center in (pivot, tip):
        vertices.extend([
            tuple(center - side * half_width - depth * half_depth),
            tuple(center + side * half_width - depth * half_depth),
            tuple(center + side * half_width + depth * half_depth),
            tuple(center - side * half_width + depth * half_depth),
        ])
    faces.extend([
        (start+0, start+3, start+2, start+1),
        (start+4, start+5, start+6, start+7),
        (start+0, start+1, start+5, start+4),
        (start+1, start+2, start+6, start+5),
        (start+2, start+3, start+7, start+6),
        (start+3, start+0, start+4, start+7),
    ])

def main():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)

    vertices, faces = [], []
    add_box(vertices, faces, (0,0,0), (0,0,3), 0.16, 0.16)
    add_box(vertices, faces, (0,0,1.20), (-1.05,0.10,2.05), 0.11, 0.11)
    add_box(vertices, faces, (0,0,1.78), (1.00,-0.12,2.62), 0.11, 0.11)

    mesh = bpy.data.meshes.new(OBJECT_NAME + "_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(OBJECT_NAME, mesh)
    bpy.context.collection.objects.link(obj)
    obj["geometry"] = "Geometry only: 3 boxes, 8 vertices each, 24 total"
    obj["vertex_data"] = "No vertex colors; no baked Pivot/Tip/Weight; no UV payload"

    material = bpy.data.materials.new(name="WindTestNeutral")
    material.diffuse_color = (0.28, 0.58, 0.22, 1.0)
    mesh.materials.append(material)

    os.makedirs(os.path.dirname(OUTPUT_PATH), exist_ok=True)
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.export_scene.fbx(
        filepath=OUTPUT_PATH,
        use_selection=True,
        object_types={"MESH"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        axis_forward="-Z",
        axis_up="Y",
        mesh_smooth_type="FACE",
        add_leaf_bones=False,
        bake_anim=False,
    )
    print(
        f"EXPORT_OK geometry_only vertices={len(mesh.vertices)} "
        f"polygons={len(mesh.polygons)} colors={len(mesh.color_attributes)} "
        f"uv_sets={len(mesh.uv_layers)}"
    )

if __name__ == "__main__":
    main()
