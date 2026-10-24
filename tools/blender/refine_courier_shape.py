"""Refine the existing script-built courier: clothed silhouette and round shoulders.

Run in the scene containing Courier_Model. Does not clear scenes or save over v03.
"""
import math
import bpy
from mathutils import Vector

scene=bpy.context.scene
col=next((c for c in scene.collection.children if c.name.startswith('Courier_Model')),None)
if col is None:raise RuntimeError('Open the script-built courier scene first.')
if scene.get('curvier_shape_applied'):raise RuntimeError('This refinement is already applied.')
root=next(o for o in col.objects if o.type=='EMPTY')

def bell(z,center,width):return math.exp(-((z-center)/width)**2)

for o in col.objects:
    if o.type!='MESH':continue
    name=o.name
    if name=='Knit_Torso' or name=='Jacket_Open':
        o.data=o.data.copy()
        for v in o.data.vertices:
            x,y,z=v.co
            waist=bell(z,1.075,.072)
            bust=bell(z,1.225,.085)
            v.co.x=x*(1-.13*waist+.075*bust)
            if y<0:
                # Volume belongs to the garment; no separate body spheres.
                v.co.y=y*(1-.10*waist)-.034*bust*min(1,-y/.09)
        o.data.update()
    elif name.startswith('Trouser_Hips'):
        o.data=o.data.copy()
        for v in o.data.vertices:
            x,y,z=v.co
            hip=bell(z,.86,.105)
            thigh=bell(z,.69,.14)
            waist=bell(z,1.0,.06)
            v.co.x=x*(1+.19*hip+.085*thigh-.12*waist)
            v.co.y=y*(1+.13*hip+.06*thigh-.08*waist)
        o.data.update()
    elif name.startswith('Belt'):
        o.data=o.data.copy()
        for v in o.data.vertices:
            v.co.x*=.90;v.co.y*=.94
        o.data.update()
    elif name.startswith('Cargo_Pocket'):
        # Box mesh geometry is local, whereas pocket placement is object translation.
        o.location.x*=1.10
    elif name.startswith('Satchel_') and name!='Satchel_Strap':
        o.location.x-=.10
    elif name=='Satchel_Strap':
        o.data=o.data.copy()
        for v in o.data.vertices:
            if v.co.y<0:v.co.y-=.032*bell(v.co.z,1.225,.10)
        o.data.update()
    elif name.startswith('Chest_Pocket') or name.startswith('Pocket_Flap'):
        o.location.y-=.03;o.location.x*=1.04

# Rebuild the sleeves with rounded shoulder ends and a curved upper-arm transition.
def sleeve_geometry(points,radii,segments=12):
    vertices=[]
    for i,p in enumerate(points):
        p=Vector(p);previous=Vector(points[max(i-1,0)]);following=Vector(points[min(i+1,len(points)-1)])
        tangent=(following-previous).normalized()
        helper=Vector((0,1,0));a=tangent.cross(helper).normalized();b=tangent.cross(a).normalized()
        rx,ry=radii[i]
        for k in range(segments):
            angle=math.tau*k/segments
            vertices.append(tuple(p+a*math.cos(angle)*rx+b*math.sin(angle)*ry))
    faces=[tuple(reversed(range(segments)))]
    for i in range(len(points)-1):
        for k in range(segments):
            a=i*segments+k;b=i*segments+(k+1)%segments
            faces.append((a,b,b+segments,a+segments))
    faces.append(tuple(range((len(points)-1)*segments,len(vertices))))
    return vertices,faces

paths={
 'R':[(.135,0,1.33),(.163,0,1.34),(.195,0,1.31),(.235,0,1.23),(.265,0,1.12),(.29,-.005,.96)],
 'L':[(-.135,0,1.33),(-.163,0,1.34),(-.195,0,1.31),(-.235,-.01,1.23),(-.27,-.025,1.13),(-.09,-.15,1.23)],
}
radii=[(.025,.035),(.055,.059),(.074,.077),(.072,.073),(.067,.068),(.044,.049)]
for label,points in paths.items():
    obj=next(o for o in col.objects if o.name=='Jacket_Sleeve_'+label)
    mats=list(obj.data.materials)
    data=bpy.data.meshes.new('RoundedSleeve_'+label)
    vertices,faces=sleeve_geometry(points,radii)
    data.from_pydata(vertices,[],faces);data.update()
    for m in mats:data.materials.append(m)
    for p in data.polygons:p.use_smooth=True
    obj.data=data

    # Rebuild cuffs at the same radial resolution to avoid sawtooth intersections.
    cuff=next(o for o in col.objects if o.name=='Sleeve_Cuff_'+label)
    direction=(Vector(points[-1])-Vector(points[-2])).normalized()
    wrist=Vector(points[-1])
    cvs,cfs=sleeve_geometry([tuple(wrist-direction*.03),tuple(wrist+direction*.026)],
                          [(.052,.058),(.046,.052)])
    cm=bpy.data.meshes.new('RoundCuff_'+label);cm.from_pydata(cvs,[],cfs);cm.update()
    for m in cuff.data.materials:cm.materials.append(m)
    cuff.data=cm

pants=next(o for o in col.objects if o.name.startswith('Trouser_Hips'))
bpy.context.view_layer.objects.active=pants
modifier=pants.modifiers.new('SoftenHipTransition','SMOOTH');modifier.factor=1.0;modifier.iterations=6
bpy.ops.object.modifier_apply(modifier=modifier.name)

# Blunt the collar points as well, so they do not read as shoulder spikes.
for o in col.objects:
    if not o.name.startswith('Jacket_Collar'):continue
    old=o.data
    ordered=[old.vertices[i].co.copy() for i in old.polygons[0].vertices]
    rounded=[]
    for i,p in enumerate(ordered):
        rounded.extend([p.lerp(ordered[i-1],.17),p.lerp(ordered[(i+1)%len(ordered)],.17)])
    data=bpy.data.meshes.new('SoftCollar')
    data.from_pydata(rounded,[],[tuple(range(len(rounded)))]);data.update()
    for m in old.materials:data.materials.append(m)
    o.data=data
    modifier=o.modifiers.new('CollarThickness','SOLIDIFY');modifier.thickness=.006

scene['curvier_shape_applied']=True
scene['art_status']='clothed silhouette refinement, awaiting user review'
bpy.context.view_layer.update()
print('Refined waist, chest and hips; rounded shoulder meshes and collar corners.')
