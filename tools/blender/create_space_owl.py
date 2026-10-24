"""Build an original flat-shaded owl in a new scene; never clear existing scenes.

Run in Blender's Python context. Outputs are versioned; an existing blend is not overwritten.
"""
from pathlib import Path
import math
import json
import random
import bpy
import bmesh
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'art/models/space-owl'
OUT.mkdir(parents=True, exist_ok=True)
version = 1
while (OUT / f'space-owl-v{version:02}.blend').exists():
    version += 1
stem = f'space-owl-v{version:02}'
scene = bpy.data.scenes.new(f'Orbita_SpaceOwl_v{version:02}')
bpy.context.window.scene = scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1
model = bpy.data.collections.new('SpaceOwl_Model')
scene.collection.children.link(model)
studio = bpy.data.collections.new('SpaceOwl_Studio')
scene.collection.children.link(studio)
root = bpy.data.objects.new('SpaceOwl_Root', None)
model.objects.link(root)
root['asset_type'] = 'original low-poly companion concept; unrigged'
root['forward_axis_blender'] = '-Y'
root['version'] = version
rng = random.Random(17)

def mat(name, color, roughness=.85, emission=0):
    m = bpy.data.materials.new('Owl_' + name)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    node = next(n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    node.inputs['Base Color'].default_value = (*color, 1)
    node.inputs['Roughness'].default_value = roughness
    if emission:
        node.inputs['Emission Color'].default_value = (*color, 1)
        node.inputs['Emission Strength'].default_value = emission
    return m

ochre = mat('Ochre', (.40, .18, .065))
ochre_light = mat('Honey', (.56, .29, .105))
ochre_dark = mat('Umber', (.255, .105, .045))
cream = mat('Cream', (.80, .65, .40))
cream_light = mat('Ivory', (.95, .82, .57))
cream_shadow = mat('WarmLinen', (.67, .49, .26))
belly_mat = mat('Breast', (.69, .48, .25))
eye_dark = mat('EyeSocket', (.045, .025, .017))
amber = mat('AmberIris', (.70, .32, .045), .38)
pupil = mat('Pupil', (.012, .009, .007), .32)
gold = mat('Starlight', (.95, .67, .22), .5, .18)
beak_mat = mat('Beak', (.40, .24, .08))
feet_mat = mat('Feet', (.24, .135, .06))

def mesh(name, verts, faces, materials, shade_variation=False):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.materials.clear()
    for m in materials:
        data.materials.append(m)
    data.update()
    bm = bmesh.new()
    bm.from_mesh(data)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(data)
    bm.free()
    obj = bpy.data.objects.new(name, data)
    model.objects.link(obj)
    obj.parent = root
    for face in data.polygons:
        face.use_smooth = False
        if shade_variation and rng.random() < .2:
            face.material_index = rng.randrange(len(materials))
    return obj

def ellipsoid(name, center, scale, material, segments=12, rings=6):
    verts = [(center[0], center[1], center[2]-scale[2])]
    for j in range(1, rings):
        a = -math.pi/2 + math.pi*j/rings
        for i in range(segments):
            t = 2*math.pi*i/segments + (j%2)*math.pi/segments
            verts.append((center[0]+scale[0]*math.cos(a)*math.cos(t),
                          center[1]+scale[1]*math.cos(a)*math.sin(t),
                          center[2]+scale[2]*math.sin(a)))
    top = len(verts)
    verts.append((center[0],center[1],center[2]+scale[2]))
    faces = []
    for i in range(segments):
        faces.append((0,1+(i+1)%segments,1+i))
    for j in range(rings-2):
        a, b = 1+j*segments, 1+(j+1)*segments
        for i in range(segments):
            k = (i+1)%segments
            faces.extend([(a+i,a+k,b+i),(a+k,b+k,b+i)])
    last = 1+(rings-2)*segments
    for i in range(segments):
        faces.append((last+i,last+(i+1)%segments,top))
    return mesh(name, verts, faces, material if isinstance(material,list) else [material], True)

ellipsoid('Body', (0,.035,.43), (.265,.22,.34), [ochre,ochre_light,ochre_dark])
ellipsoid('Head', (0,-.01,.81), (.325,.235,.285), [ochre,ochre_light])
ellipsoid('Breast', (0,-.135,.43), (.19,.115,.25), [belly_mat,cream_shadow], 10, 5)

# A continuous heart-shaped facial shield with a raised rim, not two cartoon eyeballs.
outline = [(0,.945),(.08,1.01),(.19,.99),(.275,.91),(.285,.81),
           (.235,.72),(.14,.625),(0,.56),(-.14,.625),(-.235,.72),
           (-.285,.81),(-.275,.91),(-.19,.99),(-.08,1.01)]
n = len(outline)
verts = [(x,-.195,z) for x,z in outline]
verts += [(x*.86,-.26,.80+(z-.80)*.86) for x,z in outline]
verts.append((0,-.274,.80))
faces=[]
for i in range(n):
    k=(i+1)%n
    faces.extend([(i,k,n+i),(k,n+k,n+i),(n+i,n+k,2*n)])
mask=mesh('Heart_Facial_Disc',verts,faces,[cream,cream_light,cream_shadow])
for i,p in enumerate(mask.data.polygons):
    p.material_index = 1 if i%3 != 2 else (2 if i//3 in [4,5,6,7,8,9] else 0)

def disc(name, x, z, radius, y, material, aspect=1, segments=10):
    verts=[(x,y-.004,z)]
    verts += [(x+math.cos(t*2*math.pi/segments)*radius,
               y,z+math.sin(t*2*math.pi/segments)*radius*aspect) for t in range(segments)]
    return mesh(name,verts,[(0,1+i,1+(i+1)%segments) for i in range(segments)],[material])

for side in [-1,1]:
    x=side*.122
    disc(f'EyeSocket_{side}',x,.834,.062,-.281,eye_dark,1.05)
    disc(f'Iris_{side}',x,.834,.040,-.289,amber,1.07)
    disc(f'Pupil_{side}',x,.834,.025,-.297,pupil,1.15)
    disc(f'EyeGlint_{side}',x-.009,.85,.008,-.304,cream_light)
    # Feather brow is a broad graphic wedge, leaving the expression neutral and curious.
    mesh(f'Brow_{side}',[(x-side*.075,-.267,.90),(x+side*.073,-.262,.916),
                       (x+side*.06,-.294,.885),(x-side*.05,-.298,.883)],
         [(0,1,2),(0,2,3)],[cream_light])

mesh('Beak',[(0,-.29,.82),(-.031,-.28,.77),(.031,-.28,.77),
             (0,-.355,.751),(0,-.29,.708)],
     [(0,1,3),(0,3,2),(1,4,3),(2,3,4),(0,2,4,1)],[beak_mat])

# Closed, volumetric wing shells; large planes read as folded feathers.
for side,label in [(-1,'L'),(1,'R')]:
    outline3=[(.205,-.075,.72),(.315,-.06,.68),(.365,.005,.56),
              (.345,.12,.39),(.225,.205,.19),(.20,.12,.48)]
    vs=[(side*x,y,z) for x,y,z in outline3]
    vs += [(side*.355,.015,.49),(side*.245,.055,.49)]
    fs=[]
    for i in range(6):
        fs.extend([(i,(i+1)%6,6),((i+1)%6,i,7)])
    wing=mesh('Wing_'+label,vs,fs,[ochre_dark,ochre,ochre_light])
    for p in wing.data.polygons: p.material_index=(p.index//2)%3
    # A four-point compass star lies over the visible broad side plane.
    x=side*.359
    yz=[(-.018,.57),(.001,.53),(.038,.515),(.008,.50),
        (.002,.46),(-.012,.50),(-.048,.515),(-.022,.53)]
    mark=mesh('Wing_Star_'+label,[(x,y,z) for y,z in yz],[tuple(range(8))],[gold])
    bm=bmesh.new(); bm.from_mesh(mark.data)
    for f in bm.faces:
        if f.normal.x*side < 0: f.normal_flip()
    bm.to_mesh(mark.data); bm.free()

for i in [-1,0,1]:
    mesh('Tail_'+str(i),[(i*.065-.047,.17,.31),(i*.065+.047,.17,.31),
                       (i*.085+.03,.39,.12),(i*.085-.03,.39,.12),
                       (i*.065,.21,.35)],
         [(0,1,4),(1,2,4),(2,3,4),(3,0,4),(0,3,2,1)],[ochre_dark])

def rod(name,a,b,r,material,segments=5):
    d=Vector(b)-Vector(a)
    u=d.cross(Vector((0,0,1)))
    if u.length<.001: u=d.cross(Vector((0,1,0)))
    u.normalize(); v=d.normalized().cross(u)
    verts=[]
    for p in [Vector(a),Vector(b)]:
        for i in range(segments):
            verts.append(tuple(p+r*(u*math.cos(i*2*math.pi/segments)+v*math.sin(i*2*math.pi/segments))))
    fs=[tuple(reversed(range(segments))),tuple(range(segments,segments*2))]
    fs += [(i,(i+1)%segments,(i+1)%segments+segments,i+segments) for i in range(segments)]
    return mesh(name,verts,fs,[material])

for side in [-1,1]:
    x=side*.105
    rod('Ankle_'+str(side),(x,.015,.15),(x,-.025,.055),.021,feet_mat)
    for toe in [-1,0,1]:
        end=(x+toe*.048,-.17+abs(toe)*.025,.018)
        rod(f'Toe_{side}_{toe}',(x,-.025,.048),end,.016,feet_mat)
    rod('BackToe_'+str(side),(x,.01,.05),(x,.095,.02),.014,feet_mat)

# A small forehead lozenge ties the owl to its stellar marking without extra gear.
mesh('Forehead_Star',[(0,-.218,1.045),(.018,-.231,1.015),(0,-.243,.987),
                      (-.018,-.231,1.015)],[(0,1,2),(0,2,3)],[gold])

def link_studio(obj):
    for c in list(obj.users_collection): c.objects.unlink(obj)
    studio.objects.link(obj)

bpy.ops.mesh.primitive_plane_add(size=200, location=(0,0,-.012))
ground=bpy.context.object; ground.name='Studio_Ground'; link_studio(ground)
ground.data.materials.append(mat('Backdrop',(.28,.19,.12)))

def aim(obj, point):
    obj.rotation_euler=(Vector(point)-obj.location).to_track_quat('-Z','Y').to_euler()

for name,pos,power,color,size in [
    ('Key',(-2,-3,4),420,(1,.77,.48),3),
    ('Fill',(2,-1,2),160,(1,.88,.68),2.5),
    ('Rim',(1,2,3),500,(1,.59,.3),2)]:
    data=bpy.data.lights.new('Studio_'+name,'AREA'); data.energy=power; data.color=color; data.shape='DISK'; data.size=size
    obj=bpy.data.objects.new('Studio_'+name,data); studio.objects.link(obj); obj.location=pos; aim(obj,(0,0,.5))
camera_data=bpy.data.cameras.new('Owl_Preview_Camera')
camera=bpy.data.objects.new('Owl_Preview_Camera',camera_data); studio.objects.link(camera)
camera.location=(1.65,-3.5,1.55); aim(camera,(0,0,.53))
camera_data.type='ORTHO'; camera_data.ortho_scale=1.63; camera_data.lens=55
scene.camera=camera
world=bpy.data.worlds.new('Owl_Studio_World'); world.use_nodes=True
next(n for n in world.node_tree.nodes if n.type=='BACKGROUND').inputs['Color'].default_value=(.18,.12,.075,1)
next(n for n in world.node_tree.nodes if n.type=='BACKGROUND').inputs['Strength'].default_value=.45
scene.world=world
scene.render.resolution_x=1200; scene.render.resolution_y=1200; scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.render.filepath=str(OUT/(stem+'-preview.png'))
scene.view_settings.view_transform='AgX'
for obj in bpy.context.selected_objects: obj.select_set(False)
for obj in model.objects: obj.select_set(True)
bpy.context.view_layer.objects.active=root
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.region_3d.view_perspective='CAMERA'
            area.spaces.active.shading.type='MATERIAL'

meshes=[o for o in model.objects if o.type=='MESH']
for obj in meshes:
    obj.data.calc_loop_triangles()
stats={'version':version,'mesh_objects':len(meshes),
       'triangles':sum(len(o.data.loop_triangles) for o in meshes),
       'vertices':sum(len(o.data.vertices) for o in meshes),
       'materials':len({m.name for o in meshes for m in o.data.materials}),
       'rigged':False,'animated':False,'source':'procedural original mesh',
       'forward_blender':'-Y','units':'meters'}
root['triangles']=stats['triangles']
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/(stem+'.blend')))
fbx=OUT/(stem+'.fbx')
fbx.parent.mkdir(parents=True,exist_ok=True)
bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=False,collection=model.name,object_types={'EMPTY','MESH'},
                         apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',
                         axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False)
(OUT/(stem+'-stats.json')).write_text(json.dumps(stats,indent=2)+'\n',encoding='utf-8')
print('SPACE_OWL_CREATED',json.dumps(stats),str(OUT/(stem+'.blend')),str(fbx))
