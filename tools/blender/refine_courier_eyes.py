"""Fit almond eyes and volumetric eyelids to the actual face; narrow hips in X only."""
import bpy,bmesh,math
from mathutils import Vector

scene=bpy.context.scene
if scene.get('eyes_pass06'):raise RuntimeError('Eye refinement already applied.')
col=bpy.data.collections['Courier_Model']
root=next(o for o in col.objects if o.type=='EMPTY')
head=next(o for o in col.objects if o.name.startswith('Head'))
skin=head.data.materials[0]
materials={}
for key,prefix in [('white','Eye_White_L'),('iris','Eye_Iris_L'),('dark','Eye_Pupil_L')]:
    materials[key]=next(o for o in col.objects if o.name==prefix).data.materials[0]

def material(name,color,roughness):
    m=bpy.data.materials.new(name);m.use_nodes=True;m.diffuse_color=(*color,1)
    node=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    node.inputs['Base Color'].default_value=(*color,1);node.inputs['Roughness'].default_value=roughness
    return m
lash=material('Courier06_SoftLash',(.07,.027,.014),.8)
glint=material('Courier06_Catchlight',(.96,.90,.76),.25)

def mesh(name,vertices,faces,mat,closed=False):
    data=bpy.data.meshes.new(name);data.from_pydata(vertices,[],faces);data.materials.append(mat);data.update()
    bm=bmesh.new();bm.from_mesh(data)
    if closed:bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    else:
        for f in bm.faces:
            if f.normal.y>0:f.normal_flip()
    bm.to_mesh(data);bm.free()
    for p in data.polygons:p.use_smooth=True
    obj=bpy.data.objects.new(name,data);col.objects.link(obj);obj.parent=root
    return obj

def face_y(x,z):
    hit,point,normal,index=head.ray_cast(Vector((x,-1,z)),Vector((0,1,0)))
    if not hit:raise RuntimeError('Eye contour missed the face: '+str((x,z)))
    return point.y

Z=1.628;RX=.028;TOP=.012;BOTTOM=.009
def surface_y(x,z,cx):
    radial=((x-cx)/RX)**2+((z-Z)/.014)**2
    return face_y(x,z)-.003-.010*max(0,1-radial)

for label,side in [('L',-1),('R',1)]:
    cx=side*.048
    # Retain previous geometry hidden for rollback; it no longer renders.
    for prefix in ['Eye_White_','Eye_Iris_','Eye_Pupil_','Eye_Glint_']:
        old=next(o for o in col.objects if o.name==prefix+label)
        old.hide_render=True;old.hide_viewport=True
    N=32
    contour=[]
    for i in range(N):
        angle=math.tau*i/N;sin=math.sin(angle)
        x=cx+RX*math.cos(angle);z=Z+(TOP if sin>=0 else BOTTOM)*sin
        contour.append((x,surface_y(x,z,cx),z))
    # Two intermediate rings follow the cheek curvature instead of floating spheres.
    vertices=[(cx,surface_y(cx,Z,cx),Z)]
    for f in [.5,1.0]:
        for x,y,z in contour:
            xx=cx+(x-cx)*f;zz=Z+(z-Z)*f
            vertices.append((xx,surface_y(xx,zz,cx),zz))
    faces=[(0,1+i,1+(i+1)%N) for i in range(N)]
    for i in range(N):faces.append((1+i,1+(i+1)%N,1+N+(i+1)%N,1+N+i))
    mesh('Eye06_Sclera_'+label,vertices,faces,materials['white'])

    def disc(name,x,z,rx,rz,offset,mat):
        vs=[(x,surface_y(x,z,cx)-offset,z)]
        for i in range(24):
            a=math.tau*i/24;xx=x+rx*math.cos(a);zz=z+rz*math.sin(a)
            vs.append((xx,surface_y(xx,zz,cx)-offset,zz))
        return mesh(name,vs,[(0,1+i,1+(i+1)%24) for i in range(24)],mat)
    disc('Eye06_Iris_'+label,cx+.001,Z,.0105,.0115,.001,materials['iris'])
    disc('Eye06_Pupil_'+label,cx+.001,Z,.0048,.006,.002,materials['dark'])
    disc('Eye06_Glint_'+label,cx-.0024,Z+.0038,.002,.0022,.003,glint)
    disc('Eye06_SmallGlint_'+label,cx+.0044,Z-.0033,.0008,.0008,.003,glint)

    for upper,name in [(True,'UpperLid'),(False,'LowerLid')]:
        vertices=[];curve=[];steps=20
        for i in range(steps+1):
            a=math.pi*i/steps+(0 if upper else math.pi)
            s=math.sin(a);x=cx+RX*math.cos(a);z=Z+(TOP if upper else BOTTOM)*s
            ox=cx+(x-cx)*1.14;oz=Z+(z-Z)*1.65
            iy=surface_y(x,z,cx)-.001
            oy=face_y(ox,oz)-.001
            mx=x*.55+ox*.45;mz=z*.55+oz*.45
            vertices.extend([(x,iy,z),(mx,min(iy,oy)-.003,mz),(ox,oy,oz),
                             (ox,oy+.005,oz),(x,iy+.005,z)])
            curve.append((x,iy-.001,z))
        faces=[]
        for i in range(steps):
            for j in range(5):faces.append((i*5+j,i*5+(j+1)%5,(i+1)*5+(j+1)%5,(i+1)*5+j))
        faces.extend([tuple(reversed(range(5))),tuple(range(steps*5,steps*5+5))])
        mesh('Eye06_'+name+'_'+label,vertices,faces,skin,True)
        if upper:
            # A restrained lash line follows the upper lid; no oversized spikes.
            vs=[]
            for i,(x,y,z) in enumerate(curve):
                width=.0012*math.sin(math.pi*i/steps)
                vs.extend([(x,y,z),(x,y-.0004,z+width)])
            mesh('Eye06_Lash_'+label,vs,[(2*i,2*i+1,2*i+3,2*i+2) for i in range(steps)],lash)

    brow=next(o for o in col.objects if o.name.startswith('Eyebrow_'+label))
    brow.data=brow.data.copy()
    for v in brow.data.vertices:v.co.z-=.003
    brow.data.update()

pants=bpy.data.objects['Trouser_Hips'];pants.data=pants.data.copy()
before=pants.dimensions.x
for v in pants.data.vertices:
    z=v.co.z
    weight=math.exp(-((z-.84)/.15)**2)
    v.co.x*=1-.16*weight
pants.data.update()
for o in col.objects:
    if o.name.startswith('Cargo_Pocket'):o.location.x*=.95
scene['eyes_pass06']=True
bpy.context.view_layer.update()
print('Eyelids fitted to facial surface; hips width:',before,'->',pants.dimensions.x)
