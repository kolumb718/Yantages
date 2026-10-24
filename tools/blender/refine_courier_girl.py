"""Second art pass: preserve v01, rebuild the face and auburn bob in a new scene."""
import bpy, bmesh, math
from mathutils import Vector
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
scene=bpy.data.scenes.new('Orbita_CourierGirl_ArtPass02')
bpy.context.window.scene=scene
col=bpy.data.collections.new('CourierGirl_ArtPass02');scene.collection.children.link(col)
studio=bpy.data.collections.new('CourierGirl_Studio02');scene.collection.children.link(studio)
root=bpy.data.objects.new('CourierGirl_Root_v02',None);col.objects.link(root)
old=bpy.data.collections['CourierGirl_Model_v01']
exclude=('Head','Ear','Eye','Iris','Pupil','Lash','Nose','Mouth','Hair')
for src in old.objects:
    if src.type!='MESH' or src.name.startswith(exclude):continue
    o=src.copy();o.data=src.data.copy();col.objects.link(o);o.parent=root

def mat(name,c):
    m=bpy.data.materials.new('Courier02_'+name);m.diffuse_color=(*c,1);m.use_nodes=True
    p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    p.inputs['Base Color'].default_value=(*c,1);p.inputs['Roughness'].default_value=.8
    return m
skin=mat('Skin',(.63,.37,.23));skin_light=mat('LowerLip',(.49,.20,.14))
lip=mat('Lip',(.35,.10,.065));white=mat('Sclera',(.84,.78,.64))
iris=mat('Hazel',(.20,.065,.012));black=mat('Pupil',(.016,.009,.006))
glint=mat('Glint',(.96,.87,.69));brow=mat('Brow',(.14,.032,.009))
hair=mat('Auburn',(.27,.052,.012));hair_light=mat('Copper',(.40,.085,.019));hair_dark=mat('AuburnShadow',(.12,.021,.006))

def mesh(name,vs,fs,mats,smooth=False):
    me=bpy.data.meshes.new(name);me.from_pydata(vs,[],fs);me.update()
    for m in mats:me.materials.append(m)
    bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    bm.to_mesh(me);bm.free()
    o=bpy.data.objects.new(name,me);col.objects.link(o);o.parent=root
    for p in me.polygons:p.use_smooth=smooth
    return o

# More deliberate face topology: softer cheeks, narrow chin and eye recesses.
profile=[(1.474,.028,.043,-.006),(1.49,.049,.057,-.006),
 (1.515,.076,.071,-.002),(1.545,.098,.083,.003),(1.577,.112,.092,.006),
 (1.607,.115,.099,.008),(1.635,.114,.100,.011),(1.666,.109,.097,.013),
 (1.699,.097,.088,.014),(1.731,.075,.070,.014),(1.75,.04,.04,.014)]
N=24;vs=[]
for z,w,d,y in profile:
    for i in range(N):
        a=i*2*math.pi/N;x=w*math.sin(a);yy=y-d*math.cos(a)
        if math.cos(a)>.55:
            # Contours are shaped by features rather than a featureless cylinder.
            recess=.008*math.exp(-((abs(x)-.051)/.026)**2-((z-1.635)/.026)**2)
            cheek=.005*math.exp(-((abs(x)-.075)/.035)**2-((z-1.584)/.03)**2)
            yy+=recess-cheek
        vs.append((x,yy,z))
fs=[tuple(reversed(range(N)))]
for j in range(len(profile)-1):
    for i in range(N):fs.append((j*N+i,j*N+(i+1)%N,(j+1)*N+(i+1)%N,(j+1)*N+i))
fs.append(tuple(range((len(profile)-1)*N,len(vs))))
mesh('Courier02_Face',vs,fs,[skin],True)

def patch(name,vs,mat):
    o=mesh(name,vs,[tuple(range(len(vs)))],[mat])
    bm=bmesh.new();bm.from_mesh(o.data)
    for f in bm.faces:
        if f.normal.y>0:f.normal_flip()
    bm.to_mesh(o.data);bm.free()
    return o

for side,label in [(-1,'L'),(1,'R')]:
    x=side*.052
    outline=[(-.031,0),(-.02,.011),(-.006,.015),(.014,.011),(.031,0),(.015,-.011),(-.007,-.012),(-.022,-.006)]
    pts=[(x+dx,-.095+abs(dx)*.16,1.635+dz) for dx,dz in outline]
    pts.append((x,-.104,1.635))
    eye=mesh('Courier02_Eye_'+label,pts,[(i,(i+1)%8,8) for i in range(8)],[white],True)
    for name,r,ry,y,m in [('Iris',.014,.0145,-.105,iris),('Pupil',.007,.009,-.107,black)]:
        pts=[(x+math.cos(i*math.pi/6)*r,y,1.635+math.sin(i*math.pi/6)*ry) for i in range(12)]
        patch('Courier02_'+name+'_'+label,pts,m)
    patch('Courier02_Glint_'+label,[(x-.005,-.109,1.642),(x-.002,-.109,1.644),(x+.001,-.109,1.641),(x-.003,-.109,1.639)],glint)
    # Thin upper lids, not blocks across the eyeballs.
    upper=[(x+dx,-.098+abs(dx)*.13,1.635+dz) for dx,dz in outline[:5]]
    patch('Courier02_UpperLid_'+label,upper+[(xx,yy-.001,zz-.0025) for xx,yy,zz in reversed(upper)],brow)
    patch('Courier02_Brow_'+label,[(x-.032,-.087,1.672),(x-.014,-.090,1.683),(x+.012,-.087,1.681),
          (x+.030,-.078,1.673),(x+.012,-.088,1.677),(x-.014,-.091,1.679)],brow)
    # Visible cheek-side ear; part of it will be covered by hair.
    ev=[(side*.11,-.005,1.562),(side*.129,-.005,1.582),(side*.129,.015,1.619),
        (side*.111,.014,1.632),(side*.106,.034,1.59),(side*.122,-.011,1.601)]
    mesh('Courier02_Ear_'+label,ev,[(0,1,5),(1,2,5),(2,3,5),(3,0,5),(0,3,4),(3,2,4),(2,1,4),(1,0,4)],[skin],True)

mesh('Courier02_Nose',[(0,-.092,1.648),(-.009,-.094,1.605),(-.014,-.09,1.588),
 (0,-.126,1.591),(.014,-.09,1.588),(.009,-.094,1.605),(0,-.092,1.581)],
 [(0,1,3),(0,3,5),(1,2,3),(3,4,5),(2,6,3),(3,6,4),(0,5,4,6,2,1)],[skin],True)
patch('Courier02_UpperLip',[(-.026,-.080,1.557),(-.011,-.085,1.560),(0,-.087,1.557),(.009,-.085,1.561),
      (.026,-.080,1.56),(.012,-.087,1.555),(0,-.089,1.553),(-.012,-.086,1.554)],lip)
patch('Courier02_LowerLip',[(-.025,-.081,1.557),(-.009,-.088,1.554),(0,-.09,1.553),(.011,-.087,1.555),
      (.025,-.081,1.560),(.012,-.085,1.549),(-.008,-.086,1.548)],skin_light)

# Hair cap is recessed behind sculptural clumps. Each clump tapers independently.
angles=[i*2*math.pi/16 for i in range(16)]
vs=[]
for z,w,d in [(1.69,.119,.111),(1.735,.102,.10),(1.77,.064,.07),(1.785,.015,.02)]:
    vs += [(w*math.sin(a),.025-d*math.cos(a),z) for a in angles]
fs=[]
for j in range(3):
    for i in range(16):fs.append((j*16+i,j*16+(i+1)%16,(j+1)*16+(i+1)%16,(j+1)*16+i))
fs.append(tuple(range(48,64)))
mesh('Courier02_HairBase',vs,fs,[hair])

def clump(name,path,widths,depths,material):
    vs=[]
    for i,p in enumerate(path):
        p=Vector(p);a=Vector(path[max(0,i-1)]);b=Vector(path[min(len(path)-1,i+1)])
        tangent=(b-a).normalized();side=tangent.cross(Vector((0,-1,0)))
        if side.length<.001:side=Vector((1,0,0))
        side.normalize();out=side.cross(tangent).normalized()
        if out.y>0:out=-out
        w,d=widths[i],depths[i]
        for u,v in [(-1,0),(-.55,1),(.55,1),(1,0),(.5,-.5),(-.5,-.5)]:
            vs.append(tuple(p+side*w*u+out*d*v))
    fs=[tuple(reversed(range(6)))]
    for j in range(len(path)-1):
        for i in range(6):fs.append((j*6+i,j*6+(i+1)%6,(j+1)*6+(i+1)%6,(j+1)*6+i))
    fs.append(tuple(range((len(path)-1)*6,len(vs))))
    return mesh(name,vs,fs,[material])

# Side-parted fringe sweeps diagonally; uneven ends make a bob rather than a helmet.
clump('Courier02_FringeWide',[(.068,-.057,1.76),(.026,-.098,1.76),(-.039,-.111,1.729),
      (-.095,-.085,1.68),(-.111,-.063,1.615)],[.026,.036,.041,.029,.003],[.014,.019,.019,.015,.002],hair)
clump('Courier02_FringeHighlight',[(.064,-.045,1.772),(.011,-.092,1.769),(-.055,-.103,1.73),
      (-.099,-.072,1.67)],[.012,.018,.018,.001],[.009,.012,.010,.001],hair_light)
clump('Courier02_RightSweep',[(.069,-.04,1.76),(.107,-.062,1.708),(.122,-.048,1.648),
      (.109,-.057,1.61)],[.024,.023,.017,.002],[.013,.017,.012,.002],hair_light)
for s,label in [(-1,'L'),(1,'R')]:
    clump('Courier02_SideBob_'+label,[(s*.11,.01,1.712),(s*.137,.008,1.642),(s*.142,.019,1.558),
          (s*.123,.004,1.497),(s*.094,-.012,1.482)],[.023,.031,.034,.027,.002],[.021,.025,.021,.014,.002],hair)
    clump('Courier02_SideBobOuter_'+label,[(s*.083,.085,1.717),(s*.122,.094,1.642),(s*.137,.095,1.565),
          (s*.16,.055,1.508)],[.035,.037,.029,.002],[.021,.026,.022,.002],hair_dark)
    clump('Courier02_TempleWisp_'+label,[(s*.102,-.04,1.69),(s*.119,-.048,1.628),(s*.096,-.071,1.573)],
          [.012,.016,.001],[.010,.012,.001],hair_light)
for i in range(5):
    x=(i-2)*.041
    clump('Courier02_BackBob_'+str(i),[(x*.6,.085,1.74),(x,.126,1.66),(x*1.15,.129,1.57),
          (x*1.25,.105,1.49+(i%2)*.01)],[.027,.032,.029,.002],[.02,.024,.021,.002],hair if i%2 else hair_dark)

# Make the trouser pieces one intersecting-volume union so there is no "skirt" ledge.
pelvis=next(o for o in col.objects if o.name.startswith('Pelvis'))
union_ids=[v.identifier for v in bpy.types.BooleanModifier.bl_rna.properties['operation'].enum_items]
if 'UNION' in union_ids:
    bpy.context.view_layer.update();bpy.context.view_layer.objects.active=pelvis
    for o in list(col.objects):
        if o.name.startswith('Trouser_'):
            mod=pelvis.modifiers.new('JoinTrousers','BOOLEAN');mod.operation='UNION';mod.object=o
            bpy.ops.object.modifier_apply(modifier=mod.name)
            bpy.data.objects.remove(o,do_unlink=True)

    # Resolve the remaining intersection ridge into continuous cloth.
    rem=pelvis.modifiers.new('ResolveClothJoins','REMESH');rem.mode='VOXEL';rem.voxel_size=.008
    bpy.ops.object.modifier_apply(modifier=rem.name)
    sm=pelvis.modifiers.new('SoftenCrotchSeam','SMOOTH');sm.factor=1.2;sm.iterations=5
    bpy.ops.object.modifier_apply(modifier=sm.name)
    dec=pelvis.modifiers.new('BroadClothPlanes','DECIMATE');dec.ratio=.15
    bpy.ops.object.modifier_apply(modifier=dec.name)
    for p in pelvis.data.polygons:p.use_smooth=False

# Static reference pose; deliberately not a rig or a gameplay animation.
pivot=Vector((-.284,0,1.15));rotation=__import__('mathutils').Matrix.Rotation(math.radians(-120),3,'Y')
for obj in col.objects:
    if not any(obj.name.startswith(k) for k in ['Sleeve_L','SleeveCuff_L','Glove_L','GloveThumb_L']):continue
    for v in obj.data.vertices:
        if obj.name.startswith('Sleeve_L') and v.co.z>=1.15:continue
        before=v.co.copy();v.co=pivot+rotation@(before-pivot)
        v.co.y-=.145*min(1,max(0,(1.15-before.z)/.16))
    obj.data.update()
R=__import__('mathutils').Matrix.Rotation(math.radians(8),3,'Z');P=Vector((0,0,1.56))
for obj in col.objects:
    if obj.name.startswith('Courier02_'):
        for v in obj.data.vertices:v.co=P+R@(v.co-P)
        obj.data.update()

# Same studio, neutral warm light and a useful close portrait camera as well as full body.
srcstudio=bpy.data.collections['CourierGirl_Studio_v01']
for src in srcstudio.objects:
    o=src.copy();o.data=src.data.copy();studio.objects.link(o)
    if o.type=='CAMERA':
        scene.camera=o;o.name='Courier02_FullBodyCamera'
        o.location=(2.2,-6,2.3);o.rotation_euler=(Vector((-.04,0,.92))-o.location).to_track_quat('-Z','Y').to_euler()
        o.data.ortho_scale=2.13
scene.world=bpy.data.worlds['Courier_WarmStudio'].copy()
scene.render.resolution_x=1100;scene.render.resolution_y=1400;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.render.filepath=str(ROOT/'art/models/courier-girl/courier-girl-v02-preview.png')
for o in scene.objects:o.select_set(False)
root.select_set(True);bpy.context.view_layer.objects.active=root
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':area.spaces.active.region_3d.view_perspective='CAMERA'
print('Art pass 02 created; not exported or marked production-ready')
