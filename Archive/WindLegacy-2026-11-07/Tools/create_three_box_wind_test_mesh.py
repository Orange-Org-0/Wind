import bpy
import os
from mathutils import Vector

OUTPUT_PATH = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "Models", "MultiLevelWindTestTree.fbx")
OBJECT_NAME = "MultiLevelWindTestTree"

def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)

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
    return list(range(start, start + 8))

def normalize_position(position, bounds_min, bounds_size):
    return tuple((position[i]-bounds_min[i])/bounds_size[i] if bounds_size[i] else 0.0 for i in range(3))

def create_object():
    vertices, faces = [], []
    main_pivot, main_tip = (0,0,0), (0,0,3)
    left_pivot, left_tip = (0,0,1.20), (-1.05,0.10,2.05)
    right_pivot, right_tip = (0,0,1.78), (1.00,-0.12,2.62)
    main = add_box(vertices, faces, main_pivot, main_tip, 0.16, 0.16)
    left = add_box(vertices, faces, left_pivot, left_tip, 0.11, 0.11)
    right = add_box(vertices, faces, right_pivot, right_tip, 0.11, 0.11)

    mesh = bpy.data.meshes.new(OBJECT_NAME + "_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    for polygon in mesh.polygons:
        polygon.use_smooth = True
    obj = bpy.data.objects.new(OBJECT_NAME, mesh)
    bpy.context.collection.objects.link(obj)

    bounds_min = tuple(min(v[i] for v in vertices) for i in range(3))
    bounds_max = tuple(max(v[i] for v in vertices) for i in range(3))
    bounds_size = tuple(bounds_max[i]-bounds_min[i] for i in range(3))

    pivot_weight = mesh.color_attributes.new(name="PivotWeight", type="FLOAT_COLOR", domain="POINT")
    tip_mask = mesh.color_attributes.new(name="TipMask", type="FLOAT_COLOR", domain="POINT")
    mesh.color_attributes.active_color_index = mesh.color_attributes.find("PivotWeight")

    cube_data = [
        (main, main_pivot, main_tip, 0.0),
        (left, left_pivot, left_tip, 1.0),
        (right, right_pivot, right_tip, 1.0),
    ]
    for indices, pivot, tip, enabled in cube_data:
        pivot_n = normalize_position(pivot, bounds_min, bounds_size)
        tip_n = normalize_position(tip, bounds_min, bounds_size)
        for local_i, vertex_i in enumerate(indices):
            weight = (0.0 if local_i < 4 else 1.0) * enabled
            pivot_weight.data[vertex_i].color = (*pivot_n, weight)
            tip_mask.data[vertex_i].color = (*tip_n, enabled)

    # Unity supports one Color semantic. TipMask therefore has a reliable UV fallback.
    uv0 = mesh.uv_layers.new(name="UV0_Material")
    mesh.uv_layers.new(name="UV1_LightmapReserved")
    uv2 = mesh.uv_layers.new(name="UV2_TipXY")
    uv3 = mesh.uv_layers.new(name="UV3_TipZMask")
    vertex_to_tip = {}
    for indices, _pivot, tip, enabled in cube_data:
        tip_n = normalize_position(tip, bounds_min, bounds_size)
        for vertex_i in indices:
            vertex_to_tip[vertex_i] = (tip_n, enabled)
    for poly in mesh.polygons:
        for loop_i in poly.loop_indices:
            vertex_i = mesh.loops[loop_i].vertex_index
            tip_n, enabled = vertex_to_tip[vertex_i]
            position_n = normalize_position(vertices[vertex_i], bounds_min, bounds_size)
            # One stable UV value per source vertex prevents Unity from splitting
            # the eight cube corners at per-face UV seams.
            uv0.data[loop_i].uv = (position_n[0], position_n[2])
            uv2.data[loop_i].uv = (tip_n[0], tip_n[1])
            uv3.data[loop_i].uv = (tip_n[2], enabled)

    material = bpy.data.materials.new(name="WindTestNeutral")
    material.diffuse_color = (0.28, 0.58, 0.22, 1.0)
    mesh.materials.append(material)
    obj["geometry"] = "Three intersecting boxes; 8 vertices each; 24 total"
    obj["unity_color"] = "PivotWeight: normalized PivotOS RGB, local weight A"
    obj["dcc_extra_color"] = "TipMask: normalized TipOS RGB, enabled A"
    obj["unity_uv_fallback"] = "UV2=TipOS.xy; UV3=(TipOS.z, enabled)"
    obj["bounds_min_blender"] = str(bounds_min)
    obj["bounds_size_blender"] = str(bounds_size)
    return obj

def main():
    clear_scene()
    obj = create_object()
    os.makedirs(os.path.dirname(OUTPUT_PATH), exist_ok=True)
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.export_scene.fbx(
        filepath=OUTPUT_PATH, use_selection=True, object_types={"MESH"},
        apply_unit_scale=True, apply_scale_options="FBX_SCALE_UNITS",
        axis_forward="-Z", axis_up="Y", colors_type="LINEAR",
        mesh_smooth_type="FACE", add_leaf_bones=False, bake_anim=False,
    )
    print(f"EXPORT_OK vertices={len(obj.data.vertices)} polygons={len(obj.data.polygons)} colors={len(obj.data.color_attributes)} uv_sets={len(obj.data.uv_layers)}")

if __name__ == "__main__":
    main()
