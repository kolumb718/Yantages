"""Rebuild the courier's bob with continuous, softly flowing hair surfaces.

Run in the live Orbita_Courier scene after v06. Previous hair stays hidden for
rollback. Only objects carrying this script's marker are replaced on a rerun.
"""
import math
import random
import bpy
import bmesh
from mathutils import Vector

SCENE = bpy.data.scenes['Orbita_Courier']
COLLECTION = bpy.data.collections['Courier_Model']
ROOT = bpy.data.objects['Courier_Root']
TAG = 'courier_hair_pass07'
bpy.context.window.scene = SCENE

for obj in list(COLLECTION.objects):
    if obj.get(TAG):
        bpy.data.objects.remove(obj, do_unlink=True)
    elif obj.name.startswith('Hair_'):
        obj.hide_render = True
        obj.hide_viewport = True


def material(name, color, roughness):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.use_nodes = True
    shader = next(n for n in mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    shader.inputs['Base Color'].default_value = (*color, 1)
    shader.inputs['Roughness'].default_value = roughness
    mat.diffuse_color = (*color, 1)
    return mat


HAIR = material('Courier_Hair07_Auburn', (.265, .055, .020), .43)
LIGHT = material('Courier_Hair07_Copper', (.292, .065, .025), .42)
STRAND = material('Courier_Hair07_Strand', (.31, .074, .029), .44)
DARK = material('Courier_Hair07_Groove', (.21, .040, .015), .48)


def make_mesh(name, vertices, faces, mat):
    data = bpy.data.meshes.new(name)
    data.from_pydata(vertices, [], faces)
    data.update()
    # Consistent outward normals give a continuous highlight across each lock.
    bm = bmesh.new()
    bm.from_mesh(data)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(data)
    bm.free()
    obj = bpy.data.objects.new(name, data)
    COLLECTION.objects.link(obj)
    obj.parent = ROOT
    obj[TAG] = True
    data.materials.append(mat)
    for poly in data.polygons:
        poly.use_smooth = True
    return obj


def smoothstep(x):
    x = max(0, min(1, x))
    return x*x*(3-2*x)


def spline(values, t):
    """Centrally sampled Catmull-Rom interpolation, shared by all hair paths."""
    t = max(0, min(1, t)) * (len(values)-1)
    i = min(int(t), len(values)-2)
    u = t-i
    a, b, c, d = [Vector(values[max(0, min(len(values)-1, j))])
                   for j in (i-1, i, i+1, i+2)]
    return .5*((2*b)+(-a+c)*u+(2*a-5*b+4*c-d)*u*u
               +(-a+3*b-3*c+d)*u*u*u)


# A single continuous shell joins the crown, sides and nape. Its hem rolls
# inward; the face opening is smoothly cut away above the brow and cheek.
PROFILE = [
    (1.787, .000, .000), (1.777, .057, .054),
    (1.746, .096, .091), (1.706, .119, .113),
    (1.658, .132, .125), (1.607, .139, .130),
    (1.554, .137, .124), (1.515, .124, .109),
    (1.510, .115, .101),
]
HEAD_PROFILE = [
    (1.474, .029, .042, -.006), (1.495, .054, .059, -.003),
    (1.535, .090, .079, .002), (1.580, .108, .092, .007),
    (1.630, .110, .097, .011), (1.680, .101, .091, .013),
    (1.720, .079, .072, .014), (1.748, .035, .035, .014),
    (1.759, .003, .003, .014),
]


def scalp_point(x, y, z):
    for a, b in zip(HEAD_PROFILE, HEAD_PROFILE[1:]):
        if z <= b[0]:
            t = max(0, min(1, (z-a[0])/(b[0]-a[0])))
            rx, ry, cy = [a[i]*(1-t)+b[i]*t for i in (1, 2, 3)]
            length = math.sqrt((x/rx)**2+((y-cy)/ry)**2)
            return Vector((x/length*1.025, cy+(y-cy)/length*1.025, z))
    return Vector((x, y, z))


def shell_point(theta, v):
    angle = abs(math.atan2(math.sin(theta), math.cos(theta)))
    # Keep the supporting shell behind the swept fringe, avoiding a straight
    # helmet-like bar across the forehead.
    end = .27 + .73*smoothstep((angle-.60)/.65)
    t = v*end
    z, rx, ry = spline(PROFILE, t)
    # Low-amplitude irregular channels avoid both flat facets and deep spikes.
    wave = (.0009*math.sin(theta*18+1.3*t)
            + .0005*math.sin(theta*29-2.1*t)) * math.sin(math.pi*t*.85)
    radial = 1+wave/max(.025, rx)
    drift = .12*(1-t)*math.sin(theta)
    a = theta+drift
    x = .022*(1-t)**3 + rx*math.sin(a)*radial
    y = .019-ry*math.cos(a)*radial
    z += .0025*math.sin(theta*5+.7)*t**4
    point = Vector((x, y, z))
    # The front hairline seats on the scalp instead of leaving a floating rim.
    fit = smoothstep((v-.68)/.32)*(1-smoothstep((angle-.66)/.57))
    if fit > 0:
        point = point.lerp(scalp_point(x, y, z), fit)
    return point


N, ROWS = 144, 52
verts = [(.022, .019, 1.787)]
for row in range(1, ROWS+1):
    for j in range(N):
        verts.append(shell_point(math.tau*j/N, row/ROWS))
faces = [(0, 1+(j+1)%N, 1+j) for j in range(N)]
for row in range(ROWS-1):
    for j in range(N):
        a = 1+row*N+j
        b = 1+row*N+(j+1)%N
        faces.append((a, b, b+N, a+N))
shell = make_mesh('Hair07_ContinuousBob', verts, faces, HAIR)
solid = shell.modifiers.new('SoftHemThickness', 'SOLIDIFY')
solid.thickness = .003
solid.offset = -1


def fine_line(name, points, radius, mat):
    """Small geometric fibres follow the main flow rather than random fuzz."""
    curve = bpy.data.curves.new(name, 'CURVE')
    curve.dimensions = '3D'
    curve.resolution_u = 2
    curve.bevel_depth = radius
    curve.bevel_resolution = 2
    poly = curve.splines.new('POLY')
    poly.points.add(len(points)-1)
    for i, (point, co) in enumerate(zip(poly.points, points)):
        point.co = (*co, 1)
        point.radius = .08+.92*math.sin(math.pi*i/(len(points)-1))**.6
    obj = bpy.data.objects.new(name, curve)
    COLLECTION.objects.link(obj)
    obj.parent = ROOT
    obj[TAG] = True
    curve.materials.append(mat)
    return obj


def lock(name, controls, widths, thicknesses, mat, strands=9):
    """A thin flattened lock with a soft elliptical section and tapered ends."""
    steps, sides = 68, 20

    def frame(t):
        center = spline(controls, t)
        tangent = (spline(controls, min(1, t+.002))
                   - spline(controls, max(0, t-.002))).normalized()
        normal = Vector((0, -1, .10))
        normal = (normal-tangent*normal.dot(tangent)).normalized()
        across = tangent.cross(normal).normalized()
        width = max(.0002, spline([(v, 0, 0) for v in widths], t)[0])
        thick = max(.00015, spline([(v, 0, 0) for v in thicknesses], t)[0])
        return center, normal, across, width, thick

    vertices, polygons = [], []
    for i in range(steps+1):
        center, normal, across, width, thick = frame(i/steps)
        for j in range(sides):
            a = math.tau*j/sides
            vertices.append(center+across*width*math.cos(a)+normal*thick*math.sin(a))
    for i in range(steps):
        for j in range(sides):
            a = i*sides+j
            b = i*sides+(j+1)%sides
            polygons.append((a, b, b+sides, a+sides))
    polygons.extend([tuple(reversed(range(sides))),
                     tuple(range(steps*sides, (steps+1)*sides))])
    obj = make_mesh(name, vertices, polygons, mat)
    for k in range(strands):
        u = -.82+1.64*(k+.25)/strands
        points = []
        for i in range(56):
            t = .035+.925*i/55
            center, normal, across, width, thick = frame(t)
            uu = u+.025*math.sin(t*math.tau+k)
            points.append(center+across*width*uu
                          +normal*(thick*math.sqrt(1-uu*uu)+.00006))
        fine_line(name+'_Flow_%02d' % k, points, .00012,
                  STRAND if k % 3 else DARK)
    return obj


# Side part and a broad, flowing sweep. Every control path has enough samples
# to round the original angular turns without rounding it into a bulky tube.
lock('Hair07_SweptFringe', [
    (.047, -.023, 1.778), (.035, -.068, 1.773),
    (-.018, -.102, 1.752), (-.071, -.104, 1.714),
    (-.109, -.073, 1.660), (-.127, -.038, 1.589),
    (-.113, -.034, 1.530),
], [.002, .022, .031, .032, .025, .021, .001],
   [.001, .006, .008, .008, .007, .006, .0005], LIGHT, 14)

lock('Hair07_RightSweep', [
    (.047, -.022, 1.777), (.082, -.054, 1.754),
    (.112, -.053, 1.714), (.127, -.041, 1.654),
    (.143, -.017, 1.589), (.132, -.015, 1.539),
    (.116, -.011, 1.518),
], [.001, .015, .024, .025, .024, .016, .001],
   [.0005, .005, .007, .007, .006, .004, .0003], HAIR, 12)

lock('Hair07_LeftFaceFrame', [
    (-.075, -.065, 1.730), (-.109, -.060, 1.684),
    (-.132, -.025, 1.626), (-.144, -.009, 1.568),
    (-.128, -.020, 1.519),
], [.001, .016, .023, .020, .0007],
   [.0004, .004, .005, .005, .0003], HAIR, 7)

# Curved channels over the crown and nape soften the single mass without
# multiplying thick, disconnected chunks. Deterministic spacing is repeatable.
rng = random.Random(47)
for k in range(74):
    theta = math.tau*(k+rng.uniform(-.22, .22))/74
    points = []
    start = rng.uniform(.12, .25)
    end = rng.uniform(.88, .98)
    for j in range(56):
        v = start+(end-start)*j/55
        a = theta+.065*math.sin(math.pi*v)
        co = shell_point(a, v)
        dt = shell_point(a+.0005, v)-shell_point(a-.0005, v)
        dv = shell_point(a, min(.999, v+.0005))-shell_point(a, max(.001, v-.0005))
        normal = dt.cross(dv).normalized()
        radial = Vector((co.x, co.y-.019, (co.z-1.66)*.5))
        if normal.dot(radial) < 0:
            normal = -normal
        points.append(co+normal*.00024)
    fine_line('Hair07_Fibre_%02d' % k, points, rng.uniform(.00009, .00017),
              STRAND if k % 4 else DARK)

SCENE['hair_pass07'] = 'Continuous bob, curved fringe and soft strand flow'
bpy.context.view_layer.update()
print('Hair v07 rebuilt:', sum(bool(o.get(TAG)) for o in COLLECTION.objects),
      'objects; previous nine hair meshes retained hidden.')
