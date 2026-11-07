import bpy
import bmesh
import os


FBX_PATH = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "Models", "MultiLevelWindTestTree.fbx")


def connected_components(mesh):
    bm = bmesh.new()
    bm.from_mesh(mesh)
    unvisited = set(bm.verts)
    count = 0
    while unvisited:
        count += 1
        stack = [unvisited.pop()]
        while stack:
            vertex = stack.pop()
            for edge in vertex.link_edges:
                other = edge.other_vert(vertex)
                if other in unvisited:
                    unvisited.remove(other)
                    stack.append(other)
    bm.free()
    return count


bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=FBX_PATH)

mesh_objects = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
assert len(mesh_objects) == 1, f"Expected one mesh object, got {len(mesh_objects)}"

obj = mesh_objects[0]
components = connected_components(obj.data)
minimum_world_z = min((obj.matrix_world @ vertex.co).z for vertex in obj.data.vertices)

assert len(obj.data.vertices) == 24, f"Expected 24 vertices, got {len(obj.data.vertices)}"
assert len(obj.data.polygons) == 18, f"Expected 18 faces, got {len(obj.data.polygons)}"
assert components == 3, f"Expected 3 box islands in one Mesh, got {components}"
assert abs(minimum_world_z) < 0.0001, (
    f"Root surface is not at Blender world Z=0 after FBX round-trip: {minimum_world_z}"
)
assert tuple(round(v, 6) for v in obj.scale) == (1.0, 1.0, 1.0), obj.scale
assert os.path.getsize(FBX_PATH) > 0, "FBX is empty"

print(
    f"VALIDATION_OK objects={len(mesh_objects)} components={components} "
    f"vertices={len(obj.data.vertices)} polygons={len(obj.data.polygons)} "
    f"components={components} colors={len(obj.data.color_attributes)} "
    f"uv_sets={len(obj.data.uv_layers)} "
    f"location={tuple(round(v, 6) for v in obj.location)} "
    f"rotation={tuple(round(v, 6) for v in obj.rotation_euler)} "
    f"scale={tuple(round(v, 6) for v in obj.scale)} "
    f"min_world_z={minimum_world_z:.6f} bytes={os.path.getsize(FBX_PATH)}"
)
