"""Original low-poly courier #01, built in a new scene without clearing existing work."""
from pathlib import Path
import math
import json
import bpy
import bmesh
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'art/models/courier-girl'; OUT.mkdir(parents=True,exist_ok=True)
version=1
while (OUT/f'courier-girl-v{version:02}.blend').exists(): version+=1
stem=f'courier-girl-v{version:02}'
scene=bpy.data.scenes.new(f'Orbita_CourierGirl_v{version:02}')
bpy.context.window.scene=scene
scene.unit_settings.system='METRIC'; scene.unit_settings.scale_length=1
col=bpy.data.collections.new(f'CourierGirl_Model_v{version:02}');scene.collection.children.link(col)
studio=bpy.data.collections.new(f'CourierGirl_Studio_v{version:02}');scene.collection.children.link(studio)
root=bpy.data.objects.new('CourierGirl_Root',None);col.objects.link(root)
root['concept']='helmet-round-03/pair-01'; root['rigged']=False

def material(name,c,roughness=.85):
    m=bpy.data.materials.new('Courier_'+name);m.diffuse_color=(*c,1);m.use_nodes=True
    n=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    n.inputs['Base Color'].default_value=(*c,1);n.inputs['Roughness'].default_value=roughness
    return m

rust=material('Terracotta',(.49,.145,.045))
rust_light=material('OrangePanel',(.63,.235,.065))
rust_dark=material('JacketShadow',(.31,.075,.025))
ivory=material('Oatmeal',(.77,.66,.46))
ivory_dark=material('KnitShadow',(.57,.47,.32))
brown=material('CargoBrown',(.115,.08,.055))
brown_light=material('CargoPanel',(.17,.115,.075))
leather=material('Leather',(.12,.065,.032))
caramel=material('BootLeather',(.29,.145,.055))
sole=material('Sole',(.065,.045,.03))
gold=material('Brass',(.57,.34,.10),.55)
skin=material('Skin',(.67,.425,.26))
skin_shadow=material('SkinWarm',(.55,.30,.17))
lip=material('Lips',(.36,.13,.075))
hair=material('Auburn',(.255,.055,.017))
hair_light=material('CopperHair',(.42,.10,.025))
hair_dark=material('HairShadow',(.13,.026,.01))
eye_white=material('EyeWhite',(.85,.79,.65))
iris=material('Hazel',(.19,.075,.018),.5)
dark=material('Lashes',(.025,.012,.007))

def mesh(name,verts,faces,mats,tri=True):
    me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update()
    for m in mats:me.materials.append(m)
    bm=bmesh.new();bm.from_mesh(me)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    if tri:bmesh.ops.triangulate(bm,faces=list(bm.faces))
    bm.to_mesh(me);bm.free()
    o=bpy.data.objects.new(name,me);col.objects.link(o);o.parent=root
    for p in me.polygons:p.use_smooth=False
    return o

def rings(name,profile,mats,segments=10):
    # Each ring: centre x/y, height, half-width and half-depth.
    vs=[]
    for x,y,z,w,d in profile:
        for i in range(segments):
            a=2*math.pi*i/segments
            vs.append((x+w*math.sin(a),y-d*math.cos(a),z))
    fs=[tuple(reversed(range(segments)))]
    for j in range(len(profile)-1):
        for i in range(segments):
            a=j*segments+i;b=j*segments+(i+1)%segments
            fs.append((a,b,b+segments,a+segments))
    fs.append(tuple(range((len(profile)-1)*segments,len(vs))))
    return mesh(name,vs,fs,mats)

def box(name,center,size,mat,bevel=0):
    x,y,z=center;w,d,h=(s/2 for s in size)
    vs=[(x+a*w,y+b*d,z+c*h) for c in [-1,1] for b in [-1,1] for a in [-1,1]]
    o=mesh(name,vs,[(0,1,3,2),(4,6,7,5),(0,4,5,1),(2,3,7,6),(0,2,6,4),(1,5,7,3)],[mat],False)
    if bevel:
        bpy.context.view_layer.objects.active=o;o.select_set(True)
        mod=o.modifiers.new('SinglePlaneChamfer','BEVEL');mod.width=bevel;mod.segments=1
        bpy.ops.object.modifier_apply(modifier=mod.name)
        o.select_set(False)
    return o

def prism(name,points,thickness,mat):
    n=len(points);vs=list(points)+[(x,y+thickness,z) for x,y,z in points]
    return mesh(name,vs,[tuple(reversed(range(n))),tuple(range(n,2*n))]+
                [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)],[mat])

def rod(name,a,b,r,mat):
    a,b=Vector(a),Vector(b);d=(b-a).normalized();u=d.cross(Vector((0,1,0)))
    if u.length<.001:u=d.cross(Vector((1,0,0)))
    u.normalize();v=d.cross(u);vs=[]
    for p in [a,b]:
        for i in range(6):vs.append(tuple(p+r*(u*math.cos(i*math.pi/3)+v*math.sin(i*math.pi/3))))
    return mesh(name,vs,[tuple(reversed(range(6))),tuple(range(6,12))]+
                [(i,(i+1)%6,(i+1)%6+6,i+6) for i in range(6)],[mat])

# Cargo trousers and shaped leather boots.
rings('Pelvis',[(0,0,.80,.22,.125),(0,0,.89,.23,.125),(0,0,1.015,.145,.105)],[brown],12)
for s,label in [(-1,'L'),(1,'R')]:
    x=s*.113
    rings('Trouser_'+label,[(x,0,.25,.046,.05),(x,0,.33,.075,.075),
          (x,.014,.47,.105,.095),(x,.013,.62,.10,.105),(x,0,.79,.115,.12),(x,0,.88,.12,.115)],[brown,brown_light],9)
    boot=rings('Boot_'+label,[(x,-.062,.015,.078,.155),(x,-.062,.055,.078,.155),
          (x,-.065,.12,.074,.14),(x,-.03,.165,.063,.10),(x,0,.235,.057,.065)],[caramel,sole],8)
    for p in boot.data.polygons:
        if p.center.z<.06:p.material_index=1
    rings('BootCuff_'+label,[(x,0,.21,.062,.071),(x,0,.25,.066,.073),(x,0,.282,.059,.064)],[ivory],8)
    box('BootToeCap_'+label,(x,-.15,.11),(.125,.11,.055),ivory,.012)
    box('BootStrap_'+label,(x,-.071,.205),(.125,.018,.03),rust_light,.004)
    box('BootBuckle_'+label,(x+s*.044,-.084,.205),(.023,.012,.025),gold,.003)
    pocket=box('CargoPocket_'+label,(s*.216,-.005,.675),(.036,.13,.155),brown_light,.008)
    box('PocketFlap_'+label,(s*.237,-.006,.745),(.014,.142,.033),brown,.003)

# Fitted knit top visible through the cropped, open jacket.
rings('KnitTorso',[(0,0,.98,.143,.10),(0,0,1.075,.13,.085),
      (0,0,1.20,.175,.102),(0,0,1.305,.185,.102),(0,0,1.35,.18,.09),
      (0,0,1.41,.072,.055)],[ivory],12)
rings('Turtleneck',[(0,0,1.35,.073,.057),(0,0,1.425,.072,.056),(0,0,1.45,.066,.052)],[ivory,ivory_dark],10)
rings('Neck',[(0,0,1.42,.047,.043),(0,0,1.51,.046,.044)],[skin],10)
angles=[20,45,75,105,140,180,220,255,285,315,340]
vs=[]
profile=[(1.025,.161,.108),(1.09,.151,.106),(1.22,.196,.118),(1.335,.207,.112),(1.365,.19,.09)]
for z,w,d in profile:
    for a in angles:vs.append((w*math.sin(math.radians(a)),-d*math.cos(math.radians(a)),z))
fs=[];n=len(angles)
for j in range(len(profile)-1):
    for i in range(n-1):fs.append((j*n+i,j*n+i+1,(j+1)*n+i+1,(j+1)*n+i))
jacket=mesh('Jacket_OpenShell',vs,fs,[rust,rust_light,rust_dark])
for p in jacket.data.polygons:p.material_index=1 if p.center.x>.1 else 0
for s,label in [(-1,'L'),(1,'R')]:
    prism('RaisedCollar_'+label,[(s*.055,-.08,1.35),(s*.095,-.085,1.44),
          (s*.192,-.055,1.415),(s*.152,-.115,1.31)],.02,rust_light)
    prism('Lapel_'+label,[(s*.068,-.12,1.34),(s*.153,-.115,1.30),
          (s*.092,-.127,1.17),(s*.047,-.113,1.24)],.012,rust_dark)
    box('JacketHem_'+label,(s*.102,-.101,1.027),(.10,.023,.035),rust_light,.004)
    box('ChestPocket_'+label,(s*.127,-.121,1.245),(.062,.025,.075),rust_dark,.004)
    box('ChestPocketFlap_'+label,(s*.127,-.138,1.275),(.067,.012,.023),rust_light,.002)
    rings('Sleeve_'+label,[(s*.31,.01,.99,.044,.052),(s*.305,.01,1.07,.06,.063),
          (s*.284,0,1.15,.063,.075),(s*.24,0,1.265,.069,.078),(s*.192,0,1.335,.073,.077)],[rust,rust_light],8)
    rings('ShoulderCap_'+label,[(s*.192,0,1.30,.08,.08),(s*.188,0,1.35,.081,.08),
          (s*.166,0,1.38,.036,.053)],[rust],8)
    rings('SleeveCuff_'+label,[(s*.312,.01,.945,.043,.05),(s*.312,.01,1.005,.05,.055)],[ivory],8)
    rings('Glove_'+label,[(s*.317,-.004,.84,.036,.036),(s*.318,-.005,.90,.04,.041),
          (s*.312,.006,.955,.036,.039)],[leather],8)
    rod('GloveThumb_'+label,(s*.293,-.032,.923),(s*.278,-.052,.867),.019,leather)

# Belt and little brass fasteners.
rings('Belt',[(0,0,.988,.15,.111),(0,0,1.019,.15,.111)],[leather],12)
box('BeltBuckle',(0,-.122,1.004),(.048,.014,.04),gold,.003)
box('BuckleOpening',(0,-.131,1.004),(.026,.006,.022),leather,.001)

# A hand-built faceted face with a narrower jaw and natural small facial features.
rings('Head',[(0,0,1.475,.035,.045),(0,-.003,1.50,.067,.061),
      (0,0,1.545,.092,.078),(0,0,1.605,.102,.087),(0,.003,1.655,.099,.089),
      (0,.008,1.70,.084,.078),(0,.01,1.735,.045,.045)],[skin],12)

def face_patch(name,points,mat):
    o=mesh(name,points,[tuple(range(len(points)))],[mat])
    bm=bmesh.new();bm.from_mesh(o.data)
    for f in bm.faces:
        if f.normal.y>0:f.normal_flip()
    bm.to_mesh(o.data);bm.free()
    return o

for s,label in [(-1,'L'),(1,'R')]:
    x=s*.044
    face_patch('EyeWhite_'+label,[(x-.025,-.087,1.619),(x-.013,-.093,1.63),
        (x+.012,-.093,1.631),(x+.026,-.085,1.621),(x+.012,-.092,1.612),(x-.012,-.092,1.612)],eye_white)
    pts=[(x+math.cos(i*math.pi/4)*.010,-.096,1.621+math.sin(i*math.pi/4)*.0105) for i in range(8)]
    face_patch('Iris_'+label,pts,iris)
    pts=[(x+math.cos(i*math.pi/4)*.005,-.098,1.621+math.sin(i*math.pi/4)*.007) for i in range(8)]
    face_patch('Pupil_'+label,pts,dark)
    face_patch('Lash_'+label,[(x-.025,-.092,1.621),(x-.014,-.097,1.633),
        (x+.012,-.097,1.634),(x+.026,-.090,1.623),(x+.012,-.098,1.630),(x-.014,-.098,1.630)],dark)
    face_patch('Eyebrow_'+label,[(x-.025,-.080,1.651),(x-.012,-.086,1.662),
        (x+.015,-.085,1.66),(x+.026,-.076,1.652),(x+.012,-.087,1.655),(x-.011,-.087,1.657)],hair_dark)
    # Ears are small, angular closed volumes, largely tucked behind the bob.
    rings('Ear_'+label,[(s*.1,.005,1.565,.014,.012),(s*.105,.005,1.596,.019,.016),
          (s*.10,.005,1.625,.012,.013)],[skin_shadow],6)
mesh('Nose',[(0,-.085,1.628),(-.012,-.087,1.591),(.012,-.087,1.591),
             (0,-.12,1.593),(0,-.088,1.579)],[(0,1,3),(0,3,2),(1,4,3),(2,3,4),(0,2,4,1)],[skin])
face_patch('Mouth', [(-.023,-.080,1.557),(-.006,-.085,1.560),(.005,-.085,1.558),
           (.023,-.080,1.559),(.006,-.086,1.552),(-.007,-.086,1.552)],lip)

# Angular auburn bob: separate cap, curved back shell and broad swept fringe.
rings('Hair_Cap',[(0,.018,1.668,.114,.100),(0,.015,1.715,.105,.092),
      (0,.018,1.745,.065,.065),(0,.015,1.765,.015,.016)],[hair,hair_light],10)
back_angles=[65,90,120,150,180,210,240,270,295]
vs=[]
for z,w,d in [(1.49,.108,.088),(1.56,.13,.106),(1.67,.118,.105)]:
    for a in back_angles:vs.append((w*math.sin(math.radians(a)),.02-d*math.cos(math.radians(a)),z))
fs=[];n=len(back_angles)
for j in range(2):
    for i in range(n-1):fs.append((j*n+i,j*n+i+1,(j+1)*n+i+1,(j+1)*n+i))
mesh('Hair_Bob_Back',vs,fs,[hair_dark,hair])
prism('Hair_SweptFringe',[(-.108,-.047,1.698),(-.055,-.096,1.759),(.027,-.102,1.764),
      (.089,-.072,1.71),(.027,-.111,1.688),(-.046,-.112,1.659),(-.097,-.081,1.59)],.025,hair)
prism('Hair_PartHighlight',[(-.065,-.099,1.732),(.022,-.112,1.766),(.06,-.096,1.738),
      (-.012,-.124,1.70),(-.055,-.116,1.679)],.008,hair_light)
prism('Hair_LeftLock',[(-.091,-.061,1.704),(-.116,-.05,1.65),(-.123,-.036,1.53),
      (-.088,-.058,1.496),(-.083,-.073,1.607)],.03,hair_light)
prism('Hair_RightLock',[(.087,-.063,1.718),(.119,-.04,1.666),(.125,-.025,1.55),
      (.099,-.052,1.51),(.08,-.074,1.615)],.025,hair)

# Shoulder strap follows the torso in front and goes back over the shoulder.
strap_path=[(-.22,-.12,.91),(-.14,-.14,1.07),(-.04,-.142,1.22),
            (.075,-.13,1.355),(.139,-.074,1.388),(.15,.08,1.35),
            (.07,.125,1.20),(-.08,.13,1.02),(-.22,.08,.91)]
for i,(a,b) in enumerate(zip(strap_path,strap_path[1:])):
    # Ribbon across the body: broad and flat, not a cylindrical rope.
    aa,bb=Vector(a),Vector(b);direction=bb-aa
    side=Vector((direction.z,0,-direction.x)).normalized()*.023
    prism('SatchelStrap_%02d'%i,[tuple(aa-side),tuple(bb-side),tuple(bb+side),tuple(aa+side)],.008,leather)
box('StrapBuckle',(.03,-.158,1.29),(.058,.018,.045),gold,.003)
box('StrapBuckleCentre',(.03,-.17,1.29),(.033,.006,.023),leather,.001)
bag=box('DeliveryBag',(-.265,-.015,.80),(.29,.145,.28),rust_light,.018)
box('BagFrontPanel',(-.265,-.093,.795),(.238,.017,.207),ivory,.012)
box('BagLid',(-.265,-.012,.94),(.31,.16,.043),rust,.012)
for s in [-1,1]:
    box('BagBinding_'+str(s),(-.265+s*.107,-.11,.795),(.025,.015,.245),leather,.003)
    box('BagClasp_'+str(s),(-.265+s*.103,-.12,.892),(.036,.016,.038),gold,.004)
box('BagBottom',(-.265,-.014,.66),(.30,.153,.026),rust_dark,.008)
# Raised planet and orbit insignia on the delivery bag.
pts=[(-.265+math.cos(i*math.pi/6)*.038,-.108,.795+math.sin(i*math.pi/6)*.027) for i in range(12)]
face_patch('ParcelPlanet',pts,rust_dark)
prism('ParcelOrbit',[(-.335,-.112,.783),(-.194,-.112,.819),(-.193,-.112,.808),(-.335,-.112,.772)],.003,rust_dark)

def studio_object(o):
    for c in list(o.users_collection):c.objects.unlink(o)
    studio.objects.link(o)
def aim(o,p):o.rotation_euler=(Vector(p)-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.008))
floor=bpy.context.object;floor.name='Courier_StudioFloor';studio_object(floor)
floor.data.materials.append(material('StudioWarm',(.25,.18,.12)))
for name,pos,energy,color,size in [('Key',(-3,-4,5),500,(1,.83,.62),4),
       ('Fill',(3,-2,3),220,(1,.91,.78),3),('Rim',(1,3,4),600,(1,.58,.30),3)]:
    d=bpy.data.lights.new('Courier_'+name,'AREA');d.energy=energy;d.color=color;d.shape='DISK';d.size=size
    o=bpy.data.objects.new(d.name,d);studio.objects.link(o);o.location=pos;aim(o,(0,0,.95))
camdata=bpy.data.cameras.new('Courier_PreviewCamera');cam=bpy.data.objects.new(camdata.name,camdata)
studio.objects.link(cam);cam.location=(2.6,-6,2.5);aim(cam,(-.05,0,.9));camdata.type='ORTHO';camdata.ortho_scale=2.18
scene.camera=cam
world=bpy.data.worlds.new('Courier_WarmStudio');world.use_nodes=True
bg=next(n for n in world.node_tree.nodes if n.type=='BACKGROUND');bg.inputs['Color'].default_value=(.16,.11,.07,1);bg.inputs['Strength'].default_value=.5
scene.world=world
scene.render.resolution_x=1000;scene.render.resolution_y=1300;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.render.filepath=str(OUT/(stem+'-preview.png'))
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.region_3d.view_perspective='CAMERA'
            area.spaces.active.shading.type='MATERIAL'
bpy.context.view_layer.update()
for o in scene.objects:o.select_set(False)
root.select_set(True);bpy.context.view_layer.objects.active=root
meshes=[o for o in col.objects if o.type=='MESH']
for o in meshes:o.data.calc_loop_triangles()
stats={'mesh_objects':len(meshes),'vertices':sum(len(o.data.vertices) for o in meshes),
       'triangles':sum(len(o.data.loop_triangles) for o in meshes),
       'materials':len({m.name for o in meshes for m in o.data.materials}),
       'rigged':False,'animated':False,'concept':'helmet-round-03/pair-01','height_m':1.766}
(OUT/(stem+'-stats.json')).write_text(json.dumps(stats,indent=2)+'\n',encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/(stem+'.blend')))
bpy.ops.export_scene.fbx(filepath=str(OUT/(stem+'.fbx')),use_selection=False,collection=col.name,
   object_types={'MESH','EMPTY'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',
   axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False)
print('COURIER_CREATED',json.dumps(stats),str(OUT))
