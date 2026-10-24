"""Additional clothed silhouette and hair refinement, applied once after v04."""
import math
import bpy

scene=bpy.context.scene
col=next(c for c in scene.collection.children if c.name.startswith('Courier_Model'))
if scene.get('curves_pass05'):raise RuntimeError('Pass already applied.')

def bell(v,center,width):return math.exp(-((v-center)/width)**2)

for o in col.objects:
    if o.type!='MESH':continue
    name=o.name
    if name in ('Knit_Torso','Jacket_Open'):
        o.data=o.data.copy()
        # Extra samples allow curved shaping rather than a single angular wedge.
        bpy.context.view_layer.objects.active=o
        mod=o.modifiers.new('ClothShapeSamples','SUBSURF')
        mod.subdivision_type='SIMPLE';mod.levels=2
        bpy.ops.object.modifier_apply(modifier=mod.name)
        for v in o.data.vertices:
            x,y,z=v.co
            waist=bell(z,1.075,.085)
            bust=bell(z,1.225,.078)
            v.co.x=x*(1-.17*waist+.035*bust)
            if name=='Knit_Torso':v.co.x*=1-.09*bell(z,1.005,.04)
            if y<0:
                lobes=bell(x,-.066,.061)+bell(x,.066,.061)
                v.co.y=y*(1-.12*waist)-.044*bust*lobes*min(1,-y/.10)
        for p in o.data.polygons:p.use_smooth=True
        o.data.update()
    elif name.startswith('Trouser_Hips'):
        o.data=o.data.copy()
        for v in o.data.vertices:
            x,y,z=v.co
            waist=bell(z,1.025,.065)
            seat=bell(z,.835,.105)
            v.co.x=x*(1-.14*waist+.055*seat)
            if y>0:
                lobes=bell(x,-.105,.12)+bell(x,.105,.12)
                v.co.y=y*(1-.10*waist)+.057*seat*lobes*min(1,y/.105)
            else:v.co.y=y*(1-.10*waist)
        o.data.update()
        bpy.context.view_layer.objects.active=o
        mod=o.modifiers.new('SeatTransition','SMOOTH');mod.factor=.65;mod.iterations=3
        bpy.ops.object.modifier_apply(modifier=mod.name)
    elif name.startswith('Belt_Buckle'):
        o.location.y*=.88
    elif name.startswith('Belt'):
        o.data=o.data.copy()
        for v in o.data.vertices:v.co.x*=.87;v.co.y*=.90
        o.data.update()
    elif name=='Satchel_Strap':
        o.data=o.data.copy()
        for v in o.data.vertices:
            if v.co.y<0:v.co.y-=.045*bell(v.co.z,1.225,.09)
        o.data.update()
    elif name.startswith('Chest_Pocket') or name.startswith('Pocket_Flap'):
        o.location.y-=.035
    elif name.startswith('Hair_'):
        o.data=o.data.copy()
        bpy.context.view_layer.objects.active=o
        # Small edge radii preserve the bob length and large clumps.
        mod=o.modifiers.new('RoundedHairEdges','BEVEL');mod.width=.004
        mod.segments=3;mod.limit_method='ANGLE';mod.angle_limit=math.radians(24)
        bpy.ops.object.modifier_apply(modifier=mod.name)
        mod=o.modifiers.new('SoftHairSurface','SUBSURF');mod.levels=1
        bpy.ops.object.modifier_apply(modifier=mod.name)
        for p in o.data.polygons:p.use_smooth=True

scene['curves_pass05']=True
scene['art_status']='waist, bust, seat and hair refinement; awaiting review'
bpy.context.view_layer.update()
print('Narrower waist, fuller clothed bust and seat, softened hair geometry and normals.')
