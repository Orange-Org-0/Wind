import bpy
import math
import os
from mathutils import Vector


OUTPUT_PATH = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "Models", "MultiLevelWindTestTree.fbx")
OBJECT_NAME = "MultiLevelWindTestTree"


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)


def add_chain(vertices, edges, points, start_index=None):
    previous = start_index
    new_indices = []
    for point in points:
        index = len(vertices)
        vertices.append(point)
        new_indices.append(index)
        if previous is not None:
            edges.append((previous, index))
        previous = index
    return new_indices


def build_tree_graph():
    vertices = []
    edges = []

    # Blender uses Z-up. FBX export converts this to Unity Y-up.
    trunk_points = [(0.0, 0.0, round(i * 0.2, 4)) for i in range(16)]
    trunk = add_chain(vertices, edges, trunk_points)

    # Lower branch: starts at Z=1.2 m and bends toward -X/+Y.
    lower_branch_points = [
        (-0.16, 0.03, 1.34),
        (-0.32, 0.07, 1.48),
        (-0.49, 0.11, 1.62),
        (-0.66, 0.15, 1.75),
        (-0.84, 0.19, 1.87),
        (-1.02, 0.23, 1.98),
    ]
    lower_branch = add_chain(vertices, edges, lower_branch_points, trunk[6])

    # Upper branch: starts at Z=1.8 m and bends toward +X/-Y.
    upper_branch_points = [
        (0.15, -0.03, 1.94),
        (0.31, -0.07, 2.08),
        (0.48, -0.11, 2.22),
        (0.65, -0.15, 2.35),
        (0.82, -0.19, 2.47),
        (0.99, -0.23, 2.58),
    ]
    upper_branch = add_chain(vertices, edges, upper_branch_points, trunk[9])

    return vertices, edges, trunk, lower_branch, upper_branch


def radius_for_height(z):
    # A readable test silhouette: sturdy root, smooth taper, non-zero tip.
    normalized = max(0.0, min(1.0, z / 3.0))
    return 0.16 + (0.07 - 0.16) * normalized


def create_mesh_object():
    vertices, edges, trunk, lower_branch, upper_branch = build_tree_graph()

    mesh = bpy.data.meshes.new(f"{OBJECT_NAME}_Mesh")
    mesh.from_pydata(vertices, edges, [])
    mesh.update()

    obj = bpy.data.objects.new(OBJECT_NAME, mesh)
    bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)

    skin = obj.modifiers.new(name="ConnectedBranchSkin", type="SKIN")
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=skin.name)

    # Skin data is created only after the first evaluation; rebuild from the graph
    # so radii can be assigned deterministically before final application.
    bpy.data.objects.remove(obj, do_unlink=True)
    mesh = bpy.data.meshes.new(f"{OBJECT_NAME}_Graph")
    mesh.from_pydata(vertices, edges, [])
    mesh.update()
    obj = bpy.data.objects.new(OBJECT_NAME, mesh)
    bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)

    skin = obj.modifiers.new(name="ConnectedBranchSkin", type="SKIN")
    bpy.context.view_layer.objects.active = obj
    bpy.context.view_layer.update()
    skin_data = mesh.skin_vertices[0].data

    for index, point in enumerate(vertices):
        radius = radius_for_height(point[2])
        skin_data[index].radius = (radius, radius)

    # Branches are intentionally thinner than the main trunk.
    for chain in (lower_branch, upper_branch):
        for order, index in enumerate(chain):
            taper = 1.0 - 0.45 * ((order + 1) / len(chain))
            radius = radius_for_height(vertices[index][2]) * 0.72 * taper
            skin_data[index].radius = (radius, radius)

    skin_data[trunk[0]].use_root = True
    bpy.ops.object.modifier_apply(modifier=skin.name)

    subdivision = obj.modifiers.new(name="SingleSubdivision", type="SUBSURF")
    subdivision.subdivision_type = "CATMULL_CLARK"
    subdivision.levels = 1
    subdivision.render_levels = 1
    bpy.ops.object.modifier_apply(modifier=subdivision.name)

    # Keep the root surface and object pivot exactly on Z=0.
    min_z = min(vertex.co.z for vertex in obj.data.vertices)
    for vertex in obj.data.vertices:
        vertex.co.z -= min_z

    obj.location = (0.0, 0.0, 0.0)
    obj.rotation_euler = (0.0, 0.0, 0.0)
    obj.scale = (1.0, 1.0, 1.0)

    # Simple UVs help the FBX remain useful for quick checker-texture tests.
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.normals_make_consistent(inside=False)
    bpy.ops.uv.smart_project(angle_limit=math.radians(66.0), island_margin=0.02)
    bpy.ops.object.mode_set(mode="OBJECT")

    for polygon in obj.data.polygons:
        polygon.use_smooth = True

    material = bpy.data.materials.new(name="WindTestNeutral")
    material.diffuse_color = (0.28, 0.58, 0.22, 1.0)
    obj.data.materials.append(material)

    # Documentation metadata only; no vertex-color convention is claimed here.
    obj["purpose"] = "Two-level GPU wind deformation test mesh"
    obj["root_local_blender"] = "(0, 0, 0)"
    obj["lower_branch_pivot_blender"] = str(vertices[trunk[6]])
    obj["upper_branch_pivot_blender"] = str(vertices[trunk[9]])
    obj["vertex_color_channels"] = "Unassigned (TBD)"

    return obj


def export_fbx(obj):
    os.makedirs(os.path.dirname(OUTPUT_PATH), exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
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
        use_mesh_modifiers=True,
        mesh_smooth_type="FACE",
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="AUTO",
    )


def main():
    clear_scene()
    obj = create_mesh_object()
    export_fbx(obj)
    print(
        f"EXPORT_OK path={OUTPUT_PATH} vertices={len(obj.data.vertices)} "
        f"polygons={len(obj.data.polygons)} dimensions={tuple(round(v, 4) for v in obj.dimensions)}"
    )


if __name__ == "__main__":
    main()
