"""Original warm, stylized furniture based on the user's approved photo references.

Creates a separate Blender scene, editable component meshes and six Unity FBXs.
No photographs, downloaded meshes or third-party textures enter the game.
"""
import json
import math
import random
from pathlib import Path
import bpy
import bmesh
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'unity/Assets/Art/Furniture'
ART = ROOT / 'art/models/cabin-furniture'
ART.mkdir(parents=True, exist_ok=True)
scene = bpy.data.scenes.new('Orbita_CabinFurniture')
bpy.context.window.scene = scene
scene.unit_settings.system = 'METRIC'
assets = {}
active = None


def mat(name, color, rough=.75, metal=0):
    material = bpy.data.materials.get('Cabin_'+name) or bpy.data.materials.new('Cabin_'+name)
    material.use_nodes = True
    node = next(n for n in material.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    node.inputs['Base Color'].default_value = (*color, 1)
    node.inputs['Roughness'].default_value = rough
    node.inputs['Metallic'].default_value = metal
    material.diffuse_color = (*color, 1)
    return material


WOOD = mat('HoneyWood', (.30, .135, .055))
EDGE = mat('WoodEdge', (.40, .205, .095))
DARK = mat('WoodShadow', (.12, .052, .025))
LINEN = mat('OatmealLinen', (.69, .59, .43), .95)
CREAM = mat('CreamPillow', (.86, .77, .59), .95)
DUVET = mat('SageDuvet', (.27, .37, .27), .96)
SEAM = mat('LinenSeam', (.43, .49, .34), .98)
TERRA = mat('TerracottaUpholstery', (.53, .17, .075), .93)
TERRA_LIGHT = mat('TerracottaCushion', (.65, .25, .12), .95)
RUST = mat('RustPiping', (.34, .095, .040), .95)
BRASS = mat('Brass', (.50, .32, .105), .45, .55)
POT = mat('ClayPot', (.48, .20, .09), .88)
POT_EDGE = mat('ClayRim', (.60, .29, .14), .88)
SOIL = mat('PottingSoil', (.055, .030, .016), 1)
GREEN = mat('LeafGreen', (.080, .23, .085), .66)
LIGHT_GREEN = mat('LeafLight', (.19, .35, .12), .70)
LEAF_DARK = mat('LeafDark', (.035, .115, .050), .76)
YELLOW = mat('LeafGoldEdge', (.46, .48, .14), .8)
PAGES = mat('BookPages', (.71, .64, .49), .95)
BOOKS = [mat('Book_'+name, color) for name, color in [
    ('Sage', (.20, .32, .24)), ('Rust', (.44, .16, .08)),
    ('Teal', (.10, .25, .26)), ('Cream', (.61, .50, .33)),
    ('Plum', (.27, .15, .21)), ('Blue', (.16, .23, .32)),
]]


def start(name):
    global active
    active = bpy.data.collections.new(name)
    scene.collection.children.link(active)
    assets[name] = active


def mesh(name, verts, faces, material, smooth=False):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.update()
    bm = bmesh.new()
    bm.from_mesh(data)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(data)
    bm.free()
    obj = bpy.data.objects.new(name, data)
    active.objects.link(obj)
    data.materials.append(material)
    for polygon in data.polygons:
        polygon.use_smooth = smooth
    return obj


def box(name, position, size, material, radius=.025, rotation=(0, 0, 0)):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1)
    for vertex in bm.verts:
        vertex.co = Vector((vertex.co.x*size[0], vertex.co.y*size[1], vertex.co.z*size[2]))
    bm.normal_update()
    if radius:
        segments = 1 if name.startswith('Book_') else 3
        bmesh.ops.bevel(bm, geom=list(bm.edges), offset=min(radius, min(size)*.42),
                        segments=segments, affect='EDGES', profile=.5, clamp_overlap=True)
    data = bpy.data.meshes.new(name)
    bm.to_mesh(data)
    bm.free()
    obj = bpy.data.objects.new(name, data)
    active.objects.link(obj)
    obj.location = position
    obj.rotation_euler = rotation
    data.materials.append(material)
    return obj


def lathe(name, profile, material, segments=20, center=(0, 0, 0)):
    vertices = []
    for radius, z in profile:
        for i in range(segments):
            a = math.tau*i/segments
            vertices.append((center[0]+radius*math.cos(a), center[1]+radius*math.sin(a), center[2]+z))
    faces = []
    for row in range(len(profile)-1):
        for j in range(segments):
            a = row*segments+j
            b = row*segments+(j+1)%segments
            faces.append((a, b, b+segments, a+segments))
    faces.extend([tuple(reversed(range(segments))), tuple(range((len(profile)-1)*segments, len(profile)*segments))])
    return mesh(name, vertices, faces, material)


def rod(name, a, b, radius, material, segments=8):
    a, b = Vector(a), Vector(b)
    axis = (b-a).normalized()
    side = axis.cross(Vector((0, 0, 1)))
    if side.length < .001:
        side = axis.cross(Vector((0, 1, 0)))
    side.normalize()
    other = axis.cross(side).normalized()
    vertices = [p+radius*(side*math.cos(math.tau*i/segments)+other*math.sin(math.tau*i/segments))
                for p in (a, b) for i in range(segments)]
    faces = [(i, (i+1)%segments, (i+1)%segments+segments, i+segments) for i in range(segments)]
    faces.extend([tuple(reversed(range(segments))), tuple(range(segments, segments*2))])
    return mesh(name, vertices, faces, material, True)


def piping(name, points, material, radius=.008):
    for i in range(len(points)-1):
        rod(name+'_%02d' % i, points[i], points[i+1], radius, material, 6)


start('CabinBed')
# A wooden sleeping bay, drawers below, separate mattress and gently folded duvet.
box('Bed_Platform', (0, 0, .27), (1.84, 2.85, .42), WOOD, .065)
box('Bed_Plinth', (0, .02, .075), (1.62, 2.62, .15), DARK)
for side in (-1, 1):
    for y in (-.78, .12, .99):
        box('Storage_Drawer', (side*.925, y, .28), (.035, .82, .27), EDGE, .012)
        rod('Drawer_Handle', (side*.951, y-.11, .30), (side*.951, y+.11, .30), .017, BRASS)
box('Headboard_Frame', (0, 1.41, .98), (1.93, .13, 1.14), WOOD, .07)
for x in range(9):
    box('Headboard_Slat', ((x-4)*.205, 1.325, 1.02), (.185, .045, .94), EDGE, .016)
box('Headboard_Cap', (0, 1.41, 1.515), (2.01, .20, .07), EDGE, .03)
box('Mattress', (0, -.03, .57), (1.77, 2.68, .31), LINEN, .13)
# Dense rows give the cover an actual curved surface, with a rolled top edge.
vertices, faces = [], []
for i in range(27):
    y = -1.39+1.91*i/26
    for j in range(19):
        x = -.92+1.84*j/18
        drop = .20*(abs(x)/.92)**8
        z = .77-drop+.018*math.cos(x*12+y*2)+.035*math.exp(-((y-.45)/.11)**2)
        vertices.append((x, y, z))
for i in range(26):
    for j in range(18):
        a = i*19+j
        faces.append((a, a+1, a+20, a+19))
cover = mesh('Duvet_Draped', vertices, faces, DUVET, True)
solid = cover.modifiers.new('ClothThickness', 'SOLIDIFY')
solid.thickness = .025
box('Folded_Linen_Edge', (0, .44, .80), (1.73, .27, .085), LINEN, .04)
for x in (-.43, .43):
    pillow = box('Plump_Pillow', (x, .93, .82), (.78, .62, .22), CREAM, .10,
                 (math.radians(7), 0, x*.08))
    piping('Pillow_Seam', [(x-.29, .68, .81), (x+.29, .68, .81)], LINEN, .006)
for y in (-1.15, -.89):
    piping('Duvet_Stitch', [(-.84, y, .70), (0, y, .79), (.84, y, .70)], SEAM, .004)


start('WoodBookshelf')
for x in (-1.53, 1.53):
    box('Bookcase_Side', (x, .02, 1.28), (.13, .49, 2.13), WOOD, .035)
    for y in (-.15, .15):
        box('Bookcase_Foot', (x*.94, y, .12), (.15, .17, .24), DARK, .025)
box('Bookcase_Back', (0, .255, 1.28), (3.08, .055, 2.09), DARK, .012)
for z in (.28, .78, 1.28, 1.78, 2.30):
    box('Solid_Wood_Shelf', (0, 0, z), (3.20, .57, .10), EDGE, .025)
rng = random.Random(31)
for row, z in enumerate((.34, .84, 1.34, 1.84)):
    x = -1.40
    while x < 1.25:
        w = rng.uniform(.10, .19)
        h = rng.uniform(.29, .40)
        color = BOOKS[rng.randrange(len(BOOKS))]
        box('Book_Cover', (x+w/2, -.005, z+h/2), (w, .35, h), color, .009)
        box('Book_Page_Block', (x+w/2, -.015, z+h/2), (w*.77, .32, h-.028), PAGES, .003)
        box('Book_Spine', (x+w/2, -.191, z+h/2), (w+.004, .025, h), color, .007)
        for dz in (.065, h-.055):
            box('Book_Spine_Band', (x+w/2, -.207, z+dz), (w*.68, .005, .012), BRASS, .002)
        x += w+rng.uniform(.018, .042)


start('TerracottaSofa')
for x in (-1.49, 1.49):
    for y in (-.46, .46):
        rod('Oak_Sofa_Leg', (x*1.035, y*1.07, .02), (x, y, .33), .075, EDGE, 10)
box('Sofa_Base', (0, .01, .37), (3.67, 1.38, .35), TERRA, .16)
# Back and arms form a continuous wraparound curve with a rounded cross-section.
path = [(-1.72, -.45), (-1.79, -.12), (-1.73, .28), (-1.49, .50),
        (-.94, .56), (0, .57), (.94, .56), (1.49, .50),
        (1.73, .28), (1.79, -.12), (1.72, -.45)]
vertices = []
for i, (x, y) in enumerate(path):
    before = Vector(path[max(0, i-1)])
    after = Vector(path[min(len(path)-1, i+1)])
    tangent = (after-before).normalized()
    normal = Vector((-tangent.y, tangent.x))
    for j in range(16):
        a = math.tau*j/16
        vertices.append((x+normal.x*.17*math.cos(a), y+normal.y*.17*math.cos(a), .77+.33*math.sin(a)))
faces = []
for i in range(len(path)-1):
    for j in range(16):
        a = i*16+j
        b = i*16+(j+1)%16
        faces.append((a, b, b+16, a+16))
faces.extend([tuple(reversed(range(16))), tuple(range((len(path)-1)*16, len(path)*16))])
back = mesh('Curved_Wraparound_Back', vertices, faces, TERRA, True)
sub = back.modifiers.new('SoftContinuousUpholstery', 'SUBSURF')
sub.levels = 2
for x in (-1.02, 0, 1.02):
    box('Seat_Cushion', (x, -.13, .59), (.99, 1.02, .23), TERRA_LIGHT, .10)
    piping('Seat_Piping', [(x-.40, -.64, .585), (x+.40, -.64, .585)], RUST, .007)
for side in (-1, 1):
    box('Loose_Sofa_Pillow', (side*1.19, .22, .85), (.54, .24, .48), LINEN, .10,
        (math.radians(-12), math.radians(side*9), math.radians(side*11)))


def pot(radius, height):
    lathe('Terracotta_Pot', [(radius*.72, .02), (radius*.75, .045),
                            (radius, height-.035), (radius*.99, height),
                            (radius*.87, height), (radius*.85, height-.065)], POT)
    lathe('Thick_Pot_Rim', [(radius*.97, height-.075), (radius*1.045, height-.06),
                           (radius*1.055, height-.012), (radius*.99, height+.003)], POT_EDGE)
    lathe('Pot_Saucer', [(radius*.88, 0), (radius*1.08, .01), (radius*1.10, .04),
                        (radius*1.02, .055)], POT_EDGE)
    lathe('Visible_Soil', [(radius*.86, height-.048), (radius*.86, height-.046)], SOIL)


def leaf(name, base, tip, width, material, split=False, heart=False):
    base, tip = Vector(base), Vector(tip)
    forward = tip-base
    across = forward.cross(Vector((0, 0, 1)))
    if across.length < .01:
        across = Vector((1, 0, 0))
    across.normalize()
    normal = across.cross(forward).normalized()
    if normal.z < 0:
        normal = -normal
    rows = 20
    vertices, faces = [], []
    for i in range(rows+1):
        t = i/rows
        breadth = math.sin(math.pi*t)**.72
        if split and .20 < t < .86:
            breadth *= 1-.71*max(0, math.cos((t-.27)*math.tau*4))**8
        if heart:
            breadth *= (1.25-.5*t)
        for side in (-1, 0, 1):
            p = base+forward*t+across*(side*width*breadth)
            p += normal*(.038*math.sin(math.pi*t)*(1-.65*abs(side)))
            vertices.append(p)
    for i in range(rows):
        for side in range(2):
            a = i*3+side
            faces.append((a, a+1, a+4, a+3))
    obj = mesh(name, vertices, faces, material, True)
    solid = obj.modifiers.new('LeafThickness', 'SOLIDIFY')
    solid.thickness = .004
    rod(name+'_Midrib', base, tip, .004, LIGHT_GREEN, 5)


start('Monstera')
pot(.245, .40)
for i, (a, reach, z) in enumerate([(0, .53, 1.04), (.95, .49, 1.23),
                                 (2.05, .51, 1.07), (3.05, .43, 1.53),
                                 (4.2, .57, .94), (5.15, .47, 1.35), (5.9, .35, 1.10)]):
    direction = Vector((math.cos(a), math.sin(a), 0))
    base = direction*(reach*.31)+Vector((0, 0, z-.20))
    tip = direction*reach+Vector((0, 0, z))
    rod('Monstera_Stem', (0, 0, .35), base, .013, GREEN)
    leaf('Split_Monstera_Leaf', base, tip, .23, GREEN if i%2 else LIGHT_GREEN, split=True)


start('SnakePlant')
pot(.205, .34)
for i in range(9):
    a = math.tau*i/9
    reach = .10+.035*(i%3)
    z = .90+.13*(i%3)
    base = Vector((math.cos(a)*.06, math.sin(a)*.06, .29))
    tip = Vector((math.cos(a)*reach, math.sin(a)*reach, z))
    across = Vector((-math.sin(a), math.cos(a), 0))
    vertices, faces = [], []
    for row in range(17):
        t = row/16
        center = base.lerp(tip, t)+Vector((math.cos(a), math.sin(a), 0))*(.045*math.sin(t*math.pi))
        width = .045*math.sin(math.pi*(.12+.88*t))**.6
        for j in range(5):
            u = (j-2)/2
            vertices.append(center+across*(width*u)+Vector((math.cos(a), math.sin(a), 0))*(.013*(1-u*u)))
    for row in range(16):
        for j in range(4):
            idx = row*5+j
            faces.append((idx, idx+1, idx+6, idx+5))
    obj = mesh('Upright_Snake_Leaf', vertices, faces, GREEN)
    obj.data.materials.append(YELLOW)
    obj.data.materials.append(LEAF_DARK)
    for polygon in obj.data.polygons:
        j = polygon.index%4
        polygon.material_index = 1 if j in (0, 3) else (2 if (polygon.index//4)%3 == 0 else 0)
    solid = obj.modifiers.new('LeafThickness', 'SOLIDIFY')
    solid.thickness = .004


start('Pothos')
pot(.20, .32)
for branch in range(5):
    a = branch*math.tau/5
    points = []
    for i in range(8):
        t = i/7
        r = .035+.30*t
        z = .35+.30*math.sin(t*math.pi)-.21*t
        p = Vector((r*math.cos(a+t*.5), r*math.sin(a+t*.5), z))
        points.append(p)
        if i:
            angle = a+(.85 if i%2 else -.85)
            tip = p+Vector((.17*math.cos(angle), .17*math.sin(angle), .06))
            leaf('Heart_Pothos_Leaf', p, tip, .095, LIGHT_GREEN if (i+branch)%3 else GREEN, heart=True)
    piping('Trailing_Pothos_Stem', points, GREEN, .008)


def export_asset(name, collection):
    bpy.context.view_layer.update()
    depsgraph = bpy.context.evaluated_depsgraph_get()
    vertices, faces, smooth, indices, materials = [], [], [], [], []
    for obj in collection.objects:
        evaluated = obj.evaluated_get(depsgraph)
        data = evaluated.to_mesh()
        offset = len(vertices)
        vertices.extend(tuple(obj.matrix_world @ vertex.co) for vertex in data.vertices)
        slots = []
        for material in data.materials:
            if material not in materials:
                materials.append(material)
            slots.append(materials.index(material))
        for polygon in data.polygons:
            faces.append(tuple(offset+i for i in polygon.vertices))
            indices.append(slots[polygon.material_index])
            smooth.append(polygon.use_smooth)
        evaluated.to_mesh_clear()
    temporary = bpy.data.collections.new('Furniture_Export_Temporary')
    scene.collection.children.link(temporary)
    data = bpy.data.meshes.new(name)
    data.from_pydata(vertices, [], faces)
    for material in materials:
        data.materials.append(material)
    for polygon, index, is_smooth in zip(data.polygons, indices, smooth):
        polygon.material_index = index
        polygon.use_smooth = is_smooth
    obj = bpy.data.objects.new(name, data)
    temporary.objects.link(obj)
    folder = OUT/name
    folder.mkdir(parents=True, exist_ok=True)
    try:
        bpy.ops.export_scene.fbx(filepath=str(folder/(name+'.fbx')), use_selection=False,
            collection=temporary.name, object_types={'MESH'}, apply_unit_scale=True,
            apply_scale_options='FBX_SCALE_UNITS', axis_forward='-Z', axis_up='Y',
            bake_anim=False, add_leaf_bones=False)
        palette = []
        for material in materials:
            shader = next(n for n in material.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
            palette.append({'name': material.name, 'color': list(shader.inputs['Base Color'].default_value),
                            'roughness': shader.inputs['Roughness'].default_value,
                            'metallic': shader.inputs['Metallic'].default_value})
        data.calc_loop_triangles()
        stats = {'name': name, 'triangles': len(data.loop_triangles), 'materials': palette,
                 'source': 'Original procedural model; photos used only as references'}
        (folder/(name+'.json')).write_text(json.dumps(stats, indent=2)+'\n', encoding='utf-8')
        print(name, len(vertices), 'vertices', stats['triangles'], 'triangles')
    finally:
        bpy.data.objects.remove(obj, do_unlink=True)
        bpy.data.meshes.remove(data)
        bpy.data.collections.remove(temporary)


for name, collection in assets.items():
    export_asset(name, collection)

# Keep the editable objects in six collections at their original modelling origins.
# A gallery preview uses collection instances, without moving the export sources.
for collection in assets.values():
    collection.hide_render = True
gallery = bpy.data.collections.new('Furniture_Gallery')
scene.collection.children.link(gallery)
for name, location in [('CabinBed', (-3.4, .9, 0)), ('WoodBookshelf', (0, 1.5, 0)),
                       ('TerracottaSofa', (3.6, 1.1, 0)), ('Monstera', (2.8, -1.2, 0)),
                       ('SnakePlant', (.8, -1.1, 0)), ('Pothos', (-1.1, -1.4, 0))]:
    instance = bpy.data.objects.new('Gallery_'+name, None)
    instance.instance_type = 'COLLECTION'
    instance.instance_collection = assets[name]
    gallery.objects.link(instance)
    instance.location = location

# Excluding the modelling collections from this view layer keeps only the gallery.
for child in bpy.context.view_layer.layer_collection.children:
    if child.collection in assets.values():
        child.exclude = True
        child.collection.hide_render = False

floor = mesh('Gallery_Floor', [(-50, -50, -.02), (50, -50, -.02), (50, 50, -.02), (-50, 50, -.02)], [(0, 1, 2, 3)], mat('StudioBackdrop', (.18, .14, .10)))
for col in list(floor.users_collection):
    col.objects.unlink(floor)
gallery.objects.link(floor)


def aim(obj, target):
    obj.rotation_euler = (Vector(target)-obj.location).to_track_quat('-Z', 'Y').to_euler()


camera_data = bpy.data.cameras.new('Furniture_Gallery_Camera')
camera = bpy.data.objects.new('Furniture_Gallery_Camera', camera_data)
gallery.objects.link(camera)
camera.location = (7, -13, 10)
aim(camera, (0, .2, .9))
camera_data.type = 'ORTHO'
camera_data.ortho_scale = 12.6
scene.camera = camera
for name, location, energy, size in [('Key', (-4, -5, 9), 1700, 7), ('Fill', (5, -2, 6), 1100, 6), ('Rim', (0, 5, 8), 1800, 5)]:
    light_data = bpy.data.lights.new('Furniture_'+name, 'AREA')
    light_data.energy = energy
    light_data.shape = 'DISK'
    light_data.size = size
    light_data.color = (1, .85, .65)
    light = bpy.data.objects.new('Furniture_'+name, light_data)
    gallery.objects.link(light)
    light.location = location
    aim(light, (0, 0, .5))
world = bpy.data.worlds.new('Furniture_World')
world.use_nodes = True
next(n for n in world.node_tree.nodes if n.type == 'BACKGROUND').inputs['Strength'].default_value = .4
scene.world = world
scene.render.resolution_x = 1600
scene.render.resolution_y = 1000
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.render.filepath = str(ART/'cabin-furniture-v01-preview.png')
text = bpy.data.texts.new('create_cabin_furniture.py')
text.write(Path(__file__).read_text(encoding='utf-8'))
bpy.ops.wm.save_as_mainfile(filepath=str(ART/'cabin-furniture-v01.blend'))
bpy.ops.render.render(write_still=True)
print('Furniture source and preview saved:', ART)
