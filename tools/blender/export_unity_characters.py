"""Export visible, evaluated character geometry without touching source meshes.

Run from courier-girl-v07-hair.blend. Both characters are present in its scenes.
Curves and modifiers are baked; hidden old eyes/hair and studio objects are omitted.
"""
import json
from pathlib import Path
import bpy

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'unity/Assets/Art/Characters'


def export_character(scene_name, collection_name, folder, stem):
    scene = bpy.data.scenes[scene_name]
    source = bpy.data.collections[collection_name]
    bpy.context.window.scene = scene
    depsgraph = bpy.context.evaluated_depsgraph_get()
    vertices, faces, face_materials, smooth, materials = [], [], [], [], []
    for obj in source.objects:
        if obj.type not in {'MESH', 'CURVE'} or obj.hide_render or obj.hide_viewport:
            continue
        evaluated = obj.evaluated_get(depsgraph)
        data = evaluated.to_mesh()
        if data is None:
            continue
        offset = len(vertices)
        vertices.extend(tuple(obj.matrix_world @ v.co) for v in data.vertices)
        slots = []
        for mat in data.materials:
            if mat not in materials:
                materials.append(mat)
            slots.append(materials.index(mat))
        for poly in data.polygons:
            faces.append(tuple(offset+i for i in poly.vertices))
            face_materials.append(slots[poly.material_index] if slots else 0)
            smooth.append(poly.use_smooth)
        evaluated.to_mesh_clear()

    temporary = bpy.data.collections.new('UnityExportTemporary')
    scene.collection.children.link(temporary)
    mesh = bpy.data.meshes.new(stem)
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    for mat in materials:
        mesh.materials.append(mat)
    for poly, mat_index, is_smooth in zip(mesh.polygons, face_materials, smooth):
        poly.material_index = mat_index
        poly.use_smooth = is_smooth
    model = bpy.data.objects.new(stem, mesh)
    temporary.objects.link(model)
    destination = OUT / folder
    destination.mkdir(parents=True, exist_ok=True)
    try:
        bpy.ops.export_scene.fbx(
            filepath=str(destination / (stem+'.fbx')),
            use_selection=False, collection=temporary.name,
            object_types={'MESH'}, apply_unit_scale=True,
            apply_scale_options='FBX_SCALE_UNITS', axis_forward='-Z', axis_up='Y',
            bake_anim=False, add_leaf_bones=False, use_mesh_modifiers=True,
        )
        # Explicit shader colours keep import independent of FBX shader conversion.
        palette = []
        for mat in materials:
            shader = next((n for n in mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None) if mat.use_nodes else None
            color = list(shader.inputs['Base Color'].default_value) if shader else list(mat.diffuse_color)
            roughness = shader.inputs['Roughness'].default_value if shader else .6
            metallic = shader.inputs['Metallic'].default_value if shader else 0
            palette.append({'name': mat.name, 'color': color,
                            'roughness': roughness, 'metallic': metallic})
        mesh.calc_loop_triangles()
        info = {'sourceScene': scene_name, 'sourceCollection': collection_name,
                'vertices': len(vertices), 'triangles': len(mesh.loop_triangles),
                'materials': palette, 'animated': False,
                'forwardBlender': '-Y', 'fbxForward': '-Z', 'fbxUp': 'Y'}
        (destination / (stem+'.json')).write_text(json.dumps(info, indent=2)+'\n', encoding='utf-8')
        print(stem, len(vertices), 'vertices', len(mesh.loop_triangles), 'triangles', len(materials), 'materials')
    finally:
        bpy.data.objects.remove(model, do_unlink=True)
        bpy.data.meshes.remove(mesh)
        bpy.data.collections.remove(temporary)


previous_scene = bpy.context.window.scene
try:
    export_character('Orbita_Courier', 'Courier_Model', 'CourierGirl', 'CourierGirl')
    export_character('Orbita_SpaceOwl_v01', 'SpaceOwl_Model', 'SpaceOwl', 'SpaceOwl')
finally:
    bpy.context.window.scene = previous_scene
