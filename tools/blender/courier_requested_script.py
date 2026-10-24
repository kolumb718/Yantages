"""
Blender 4.x / 5.x.
Создаёт стилизованную девушку-курьера по выбранному направлению:
рыжий боб, терракотовая куртка, водолазка, брюки и сумка.

Это исходная модель для дальнейшей художественной доработки.
Риг, анимации и точное воспроизведение концепта не включены.
Существующие сцены и объекты не удаляются.
"""

import math
import bpy
from mathutils import Vector


# ---------------------------------------------------------------------------
# Параметры
# ---------------------------------------------------------------------------

CHARACTER_HEIGHT = 1.76
SCENE_NAME = "Orbita_Courier"
SCALE = CHARACTER_HEIGHT / 1.76

scene = bpy.data.scenes.new(SCENE_NAME)
bpy.context.window.scene = scene
scene.unit_settings.system = "METRIC"
scene.unit_settings.scale_length = 1.0

model_collection = bpy.data.collections.new("Courier_Model")
studio_collection = bpy.data.collections.new("Courier_Studio")
scene.collection.children.link(model_collection)
scene.collection.children.link(studio_collection)

root = bpy.data.objects.new("Courier_Root", None)
model_collection.objects.link(root)
root["description"] = "Low-poly девушка-курьер"
root["rigged"] = False


# ---------------------------------------------------------------------------
# Материалы
# ---------------------------------------------------------------------------

def material(name, color, roughness=0.8, metallic=0.0):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    mat.diffuse_color = (*color, 1.0)

    shader = next(
        node for node in mat.node_tree.nodes
        if node.type == "BSDF_PRINCIPLED"
    )
    shader.inputs["Base Color"].default_value = (*color, 1.0)
    shader.inputs["Roughness"].default_value = roughness
    shader.inputs["Metallic"].default_value = metallic
    return mat


M = {
    "jacket": material("Courier_Terracotta", (0.46, 0.12, 0.035)),
    "trim": material("Courier_OrangeTrim", (0.62, 0.23, 0.07)),
    "knit": material("Courier_Oatmeal", (0.78, 0.68, 0.50)),
    "pants": material("Courier_Chocolate", (0.105, 0.070, 0.047)),
    "pocket": material("Courier_Pocket", (0.145, 0.095, 0.060)),
    "leather": material("Courier_Leather", (0.095, 0.045, 0.022)),
    "boots": material("Courier_Caramel", (0.28, 0.125, 0.040)),
    "sole": material("Courier_Sole", (0.045, 0.030, 0.020)),
    "brass": material("Courier_Brass", (0.52, 0.30, 0.075), 0.45, 0.55),
    "skin": material("Courier_Skin", (0.66, 0.40, 0.25)),
    "hair": material("Courier_Auburn", (0.25, 0.045, 0.012)),
    "hair_light": material("Courier_CopperHair", (0.39, 0.085, 0.020)),
    "hair_dark": material("Courier_HairShadow", (0.12, 0.020, 0.006)),
    "white": material("Courier_EyeWhite", (0.86, 0.81, 0.69)),
    "iris": material("Courier_Hazel", (0.19, 0.065, 0.016), 0.4),
    "dark": material("Courier_EyeDark", (0.018, 0.009, 0.006)),
    "lips": material("Courier_Lips", (0.40, 0.13, 0.085)),
}


# ---------------------------------------------------------------------------
# Геометрические функции. Лицо персонажа направлено по -Y, верх — +Z.
# ---------------------------------------------------------------------------

def move_to_collection(obj, collection):
    for old_collection in list(obj.users_collection):
        old_collection.objects.unlink(obj)
    collection.objects.link(obj)


def finish(obj, name, mat, smooth=False):
    obj.name = name
    move_to_collection(obj, model_collection)
    obj.parent = root
    obj.data.materials.append(mat)

    for polygon in obj.data.polygons:
        polygon.use_smooth = smooth

    return obj


def mesh_object(name, vertices, faces, mat, smooth=False):
    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()

    obj = bpy.data.objects.new(name, mesh)
    model_collection.objects.link(obj)
    obj.parent = root
    mesh.materials.append(mat)

    for polygon in mesh.polygons:
        polygon.use_smooth = smooth

    return obj


def ellipsoid(name, location, radii, mat, smooth=False, segments=16, rings=8):
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=segments,
        ring_count=rings,
        radius=1.0,
        location=location,
    )
    obj = bpy.context.object
    obj.scale = radii

    bpy.ops.object.transform_apply(
        location=False, rotation=False, scale=True
    )
    return finish(obj, name, mat, smooth)


def box(name, location, dimensions, mat, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=location)
    obj = bpy.context.object
    obj.scale = dimensions

    bpy.ops.object.transform_apply(
        location=False, rotation=False, scale=True
    )
    finish(obj, name, mat)

    if bevel > 0:
        modifier = obj.modifiers.new("Edge_Chamfer", "BEVEL")
        modifier.width = bevel
        modifier.segments = 1
        bpy.ops.object.modifier_apply(modifier=modifier.name)

    return obj


def ring_mesh(name, profile, mat, segments=12, smooth=False):
    """Профиль: последовательность (x, y, z, радиус_x, радиус_y)."""
    vertices = []

    for x, y, z, radius_x, radius_y in profile:
        for index in range(segments):
            angle = math.tau * index / segments
            vertices.append((
                x + radius_x * math.sin(angle),
                y - radius_y * math.cos(angle),
                z,
            ))

    faces = [tuple(reversed(range(segments)))]

    for row in range(len(profile) - 1):
        for index in range(segments):
            current = row * segments + index
            following = row * segments + (index + 1) % segments
            faces.append((
                current,
                following,
                following + segments,
                current + segments,
            ))

    faces.append(tuple(range(
        (len(profile) - 1) * segments,
        len(vertices),
    )))

    return mesh_object(name, vertices, faces, mat, smooth)


def tube(name, points, radii, mat, segments=8):
    """Объёмная ломаная: используется для рук и крупных прядей."""
    vertices = []

    for index, point in enumerate(points):
        center = Vector(point)
        previous = Vector(points[max(0, index - 1)])
        following = Vector(points[min(len(points) - 1, index + 1)])
        direction = (following - previous).normalized()

        helper = Vector((0, 1, 0))
        if abs(direction.dot(helper)) > 0.95:
            helper = Vector((1, 0, 0))

        axis_x = direction.cross(helper).normalized()
        axis_y = direction.cross(axis_x).normalized()
        radius_x, radius_y = radii[index]

        for side in range(segments):
            angle = math.tau * side / segments
            vertex = (
                center
                + axis_x * math.cos(angle) * radius_x
                + axis_y * math.sin(angle) * radius_y
            )
            vertices.append(tuple(vertex))

    faces = [tuple(reversed(range(segments)))]

    for row in range(len(points) - 1):
        for side in range(segments):
            a = row * segments + side
            b = row * segments + (side + 1) % segments
            faces.append((a, b, b + segments, a + segments))

    faces.append(tuple(range(
        (len(points) - 1) * segments, len(vertices)
    )))
    return mesh_object(name, vertices, faces, mat)


def front_polygon(name, coordinates, mat):
    """Декоративная поверхность с нормалью в сторону -Y."""
    points = [Vector(point) for point in coordinates]
    normal = (points[1] - points[0]).cross(points[2] - points[0])

    order = list(range(len(points)))
    if normal.y > 0:
        order.reverse()

    return mesh_object(
        name, coordinates, [tuple(order)], mat
    )


def ribbon(name, points, width, mat):
    """Плоский ремень с небольшой толщиной."""
    vertices = []

    for index, point in enumerate(points):
        previous = Vector(points[max(0, index - 1)])
        following = Vector(points[min(len(points) - 1, index + 1)])
        tangent = following - previous

        side = Vector((tangent.z, 0, -tangent.x))
        if side.length < 0.0001:
            side = Vector((1, 0, 0))
        side.normalize()

        for offset in (-width / 2, width / 2):
            vertices.append(tuple(Vector(point) + side * offset))

    faces = [
        (2 * i, 2 * i + 1, 2 * i + 3, 2 * i + 2)
        for i in range(len(points) - 1)
    ]

    obj = mesh_object(name, vertices, faces, mat)
    modifier = obj.modifiers.new("Strap_Thickness", "SOLIDIFY")
    modifier.thickness = 0.006
    modifier.offset = 0
    return obj


# ---------------------------------------------------------------------------
# Тело и брюки
# ---------------------------------------------------------------------------

ring_mesh("Knit_Torso", [
    (0, 0, 0.99, 0.135, 0.095),
    (0, 0, 1.08, 0.125, 0.085),
    (0, 0, 1.22, 0.170, 0.100),
    (0, 0, 1.33, 0.180, 0.095),
    (0, 0, 1.40, 0.066, 0.052),
], M["knit"])

ring_mesh("Turtleneck", [
    (0, 0, 1.35, 0.069, 0.055),
    (0, 0, 1.43, 0.069, 0.055),
], M["knit"])

ring_mesh("Neck", [
    (0, 0, 1.41, 0.045, 0.042),
    (0, 0, 1.51, 0.044, 0.041),
], M["skin"], smooth=True)

hips = ring_mesh("Trouser_Hips", [
    (0, 0, 0.79, 0.205, 0.120),
    (0, 0, 0.89, 0.215, 0.125),
    (0, 0, 1.015, 0.142, 0.102),
], M["pants"])

legs = []
for side, label in ((-1, "L"), (1, "R")):
    x = side * 0.108
    legs.append(ring_mesh("Trouser_" + label, [
        (x, 0, 0.25, 0.048, 0.055),
        (x, 0, 0.34, 0.075, 0.078),
        (x, 0, 0.50, 0.095, 0.095),
        (x, 0, 0.69, 0.107, 0.110),
        (x, 0, 0.87, 0.113, 0.120),
    ], M["pants"], segments=10))

# Убираем пересечения отдельных объёмов брюк.
bpy.context.view_layer.update()
bpy.context.view_layer.objects.active = hips

for leg in legs:
    modifier = hips.modifiers.new("Trouser_Union", "BOOLEAN")
    modifier.operation = "UNION"
    modifier.object = leg
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    bpy.data.objects.remove(leg, do_unlink=True)

remesh = hips.modifiers.new("Continuous_Cloth", "REMESH")
remesh.mode = "VOXEL"
remesh.voxel_size = 0.009
bpy.ops.object.modifier_apply(modifier=remesh.name)

smooth = hips.modifiers.new("Cloth_Softening", "SMOOTH")
smooth.factor = 1.0
smooth.iterations = 4
bpy.ops.object.modifier_apply(modifier=smooth.name)

decimate = hips.modifiers.new("Low_Poly_Cloth", "DECIMATE")
decimate.ratio = 0.18
bpy.ops.object.modifier_apply(modifier=decimate.name)

for side, label in ((-1, "L"), (1, "R")):
    box(
        "Cargo_Pocket_" + label,
        (side * 0.205, 0, 0.67),
        (0.035, 0.135, 0.16),
        M["pocket"], 0.007,
    )


# ---------------------------------------------------------------------------
# Открытая укороченная куртка
# ---------------------------------------------------------------------------

angles = [22, 45, 75, 105, 140, 180, 220, 255, 285, 315, 338]
profile = [
    (1.025, 0.155, 0.108),
    (1.10, 0.150, 0.106),
    (1.23, 0.190, 0.114),
    (1.34, 0.200, 0.106),
]

vertices = []
for z, radius_x, radius_y in profile:
    for degrees in angles:
        angle = math.radians(degrees)
        vertices.append((
            radius_x * math.sin(angle),
            -radius_y * math.cos(angle),
            z,
        ))

count = len(angles)
faces = []
for row in range(len(profile) - 1):
    for index in range(count - 1):
        a = row * count + index
        faces.append((a, a + 1, a + count + 1, a + count))

jacket = mesh_object("Jacket_Open", vertices, faces, M["jacket"])
thickness = jacket.modifiers.new("Fabric_Thickness", "SOLIDIFY")
thickness.thickness = 0.008

for side, label in ((-1, "L"), (1, "R")):
    front_polygon("Jacket_Collar_" + label, [
        (side * 0.055, -0.082, 1.34),
        (side * 0.085, -0.075, 1.415),
        (side * 0.183, -0.052, 1.39),
        (side * 0.137, -0.120, 1.29),
    ], M["trim"])

    box(
        "Chest_Pocket_" + label,
        (side * 0.12, -0.12, 1.235),
        (0.065, 0.022, 0.075),
        M["jacket"], 0.004,
    )
    box(
        "Pocket_Flap_" + label,
        (side * 0.12, -0.135, 1.27),
        (0.070, 0.012, 0.022),
        M["trim"], 0.003,
    )


# ---------------------------------------------------------------------------
# Руки: одна опущена, другая держит ремень
# ---------------------------------------------------------------------------

arm_paths = {
    "R": [(0.18, 0, 1.32), (0.265, 0, 1.12), (0.29, -0.005, 0.96)],
    "L": [(-0.18, 0, 1.32), (-0.27, -0.025, 1.13), (-0.09, -0.15, 1.23)],
}

for label, points in arm_paths.items():
    tube(
        "Jacket_Sleeve_" + label, points,
        [(0.076, 0.078), (0.067, 0.068), (0.044, 0.049)],
        M["jacket"],
    )

    wrist = Vector(points[-1])
    direction = (wrist - Vector(points[-2])).normalized()

    tube(
        "Sleeve_Cuff_" + label,
        [tuple(wrist - direction * 0.025), tuple(wrist + direction * 0.025)],
        [(0.047, 0.052), (0.044, 0.049)],
        M["knit"],
    )
    hand_center = wrist + direction * 0.065
    glove = ellipsoid(
        "Glove_" + label, hand_center,
        (0.038, 0.032, 0.065), M["leather"],
        segments=10, rings=6,
    )
    glove.rotation_euler = direction.to_track_quat("Z", "Y").to_euler()


# ---------------------------------------------------------------------------
# Ботинки и пояс
# ---------------------------------------------------------------------------

for side, label in ((-1, "L"), (1, "R")):
    x = side * 0.108

    box("Boot_Sole_" + label, (x, -0.065, 0.035),
        (0.15, 0.28, 0.065), M["sole"], 0.018)

    box("Boot_Foot_" + label, (x, -0.070, 0.105),
        (0.14, 0.245, 0.12), M["boots"], 0.025)

    ring_mesh("Boot_Ankle_" + label, [
        (x, 0, 0.12, 0.060, 0.070),
        (x, 0, 0.245, 0.056, 0.064),
    ], M["boots"], segments=8)

    ring_mesh("Boot_Cuff_" + label, [
        (x, 0, 0.225, 0.064, 0.070),
        (x, 0, 0.275, 0.060, 0.065),
    ], M["knit"], segments=8)

    box("Boot_Toe_Cap_" + label, (x, -0.16, 0.13),
        (0.125, 0.09, 0.035), M["knit"], 0.009)

    box("Boot_Strap_" + label, (x, -0.071, 0.205),
        (0.125, 0.016, 0.027), M["trim"], 0.003)

ring_mesh("Belt", [
    (0, 0, 0.995, 0.147, 0.109),
    (0, 0, 1.022, 0.147, 0.109),
], M["leather"])

box("Belt_Buckle", (0, -0.12, 1.008),
    (0.044, 0.014, 0.035), M["brass"], 0.003)


# ---------------------------------------------------------------------------
# Голова: узкий подбородок, щёки, небольшие глаза
# ---------------------------------------------------------------------------

head_profile = [
    (0, -0.006, 1.474, 0.029, 0.042),
    (0, -0.003, 1.495, 0.054, 0.059),
    (0, 0.002, 1.535, 0.090, 0.079),
    (0, 0.007, 1.580, 0.108, 0.092),
    (0, 0.011, 1.630, 0.110, 0.097),
    (0, 0.013, 1.680, 0.101, 0.091),
    (0, 0.014, 1.720, 0.079, 0.072),
    (0, 0.014, 1.748, 0.035, 0.035),
]
ring_mesh("Head", head_profile, M["skin"], segments=24, smooth=True)

for side, label in ((-1, "L"), (1, "R")):
    x = side * 0.048

    ellipsoid("Ear_" + label, (side * 0.108, 0.005, 1.592),
              (0.018, 0.014, 0.034), M["skin"], True, 10, 6)

    ellipsoid("Eye_White_" + label, (x, -0.080, 1.628),
              (0.029, 0.016, 0.014), M["white"], True, 12, 6)

    ellipsoid("Eye_Iris_" + label, (x, -0.095, 1.628),
              (0.013, 0.003, 0.013), M["iris"], True, 12, 6)

    ellipsoid("Eye_Pupil_" + label, (x, -0.098, 1.628),
              (0.006, 0.002, 0.008), M["dark"], True, 10, 6)

    ellipsoid("Eye_Glint_" + label, (x - 0.004, -0.100, 1.633),
              (0.0025, 0.001, 0.0025), M["white"], True, 8, 4)

    ribbon("Eyebrow_" + label, [
        (x - 0.029, -0.077, 1.665),
        (x - 0.010, -0.085, 1.674),
        (x + 0.014, -0.081, 1.671),
        (x + 0.028, -0.073, 1.665),
    ], 0.004, M["hair_dark"])

mesh_object("Nose", [
    (0, -0.085, 1.635),
    (-0.011, -0.089, 1.592),
    (0.011, -0.089, 1.592),
    (0, -0.120, 1.594),
    (0, -0.087, 1.581),
], [
    (0, 1, 3), (0, 3, 2), (1, 4, 3),
    (2, 3, 4), (0, 2, 4, 1),
], M["skin"], smooth=True)

front_polygon("Lips", [
    (-0.025, -0.079, 1.555),
    (-0.008, -0.085, 1.558),
    (0, -0.087, 1.555),
    (0.010, -0.084, 1.559),
    (0.025, -0.078, 1.558),
    (0.009, -0.085, 1.549),
    (-0.008, -0.085, 1.549),
], M["lips"])


# ---------------------------------------------------------------------------
# Рыжий боб: объёмные крупные пряди вместо сплошного «шлема»
# ---------------------------------------------------------------------------

ring_mesh("Hair_Crown", [
    (0, 0.025, 1.690, 0.115, 0.105),
    (0, 0.025, 1.735, 0.100, 0.092),
    (0, 0.025, 1.770, 0.055, 0.055),
    (0, 0.025, 1.780, 0.010, 0.012),
], M["hair"], segments=12)

tube("Hair_Swept_Fringe", [
    (0.065, -0.045, 1.756),
    (0.020, -0.095, 1.753),
    (-0.045, -0.106, 1.720),
    (-0.095, -0.075, 1.670),
    (-0.108, -0.055, 1.620),
], [
    (0.025, 0.016), (0.035, 0.018),
    (0.037, 0.018), (0.023, 0.014), (0.003, 0.003),
], M["hair_light"], segments=6)

for side, label in ((-1, "L"), (1, "R")):
    tube("Hair_Side_" + label, [
        (side * 0.100, 0.010, 1.710),
        (side * 0.125, 0.005, 1.640),
        (side * 0.130, 0.010, 1.560),
        (side * 0.108, -0.005, 1.490),
    ], [
        (0.026, 0.023), (0.031, 0.027),
        (0.029, 0.024), (0.004, 0.004),
    ], M["hair"], segments=6)

for index in range(5):
    x = (index - 2) * 0.040
    tube("Hair_Back_%02d" % index, [
        (x * 0.6, 0.085, 1.730),
        (x, 0.122, 1.650),
        (x * 1.12, 0.125, 1.570),
        (x * 1.20, 0.100, 1.495 + (index % 2) * 0.01),
    ], [
        (0.025, 0.020), (0.029, 0.024),
        (0.027, 0.022), (0.003, 0.003),
    ], M["hair_dark"] if index % 2 == 0 else M["hair"], segments=6)


# ---------------------------------------------------------------------------
# Сумка и ремень
# ---------------------------------------------------------------------------

ribbon("Satchel_Strap", [
    (-0.25, -0.10, 0.91),
    (-0.14, -0.14, 1.08),
    (-0.025, -0.142, 1.235),
    (0.095, -0.115, 1.365),
    (0.14, 0.010, 1.385),
    (0.10, 0.12, 1.30),
    (-0.10, 0.13, 1.02),
    (-0.25, 0.07, 0.91),
], 0.043, M["leather"])

box("Satchel_Body", (-0.27, -0.015, 0.80),
    (0.285, 0.145, 0.275), M["trim"], 0.015)

box("Satchel_Front", (-0.27, -0.094, 0.80),
    (0.235, 0.017, 0.205), M["knit"], 0.009)

box("Satchel_Lid", (-0.27, -0.015, 0.945),
    (0.30, 0.16, 0.040), M["jacket"], 0.010)

for side in (-1, 1):
    box("Satchel_Binding_%s" % side,
        (-0.27 + side * 0.105, -0.109, 0.80),
        (0.023, 0.014, 0.24), M["leather"], 0.002)

    box("Satchel_Clasp_%s" % side,
        (-0.27 + side * 0.102, -0.120, 0.895),
        (0.032, 0.014, 0.032), M["brass"], 0.003)

ellipsoid("Satchel_Planet", (-0.27, -0.106, 0.80),
          (0.038, 0.003, 0.025), M["jacket"], segments=12, rings=6)

front_polygon("Satchel_Orbit", [
    (-0.338, -0.111, 0.784),
    (-0.201, -0.111, 0.819),
    (-0.200, -0.111, 0.810),
    (-0.338, -0.111, 0.776),
], M["jacket"])


# ---------------------------------------------------------------------------
# Студия. Камера и свет не входят в коллекцию игровой модели.
# ---------------------------------------------------------------------------

root.scale = (SCALE, SCALE, SCALE)

floor_material = material("Studio_WarmGround", (0.24, 0.17, 0.11))

bpy.ops.mesh.primitive_plane_add(size=200, location=(0, 0, -0.01))
floor = bpy.context.object
floor.name = "Studio_Ground"
move_to_collection(floor, studio_collection)
floor.data.materials.append(floor_material)


def aim_at(obj, point):
    direction = Vector(point) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def area_light(name, location, energy, color, size):
    data = bpy.data.lights.new(name, "AREA")
    data.energy = energy
    data.color = color
    data.size = size

    obj = bpy.data.objects.new(name, data)
    studio_collection.objects.link(obj)
    obj.location = location
    aim_at(obj, (0, 0, CHARACTER_HEIGHT * 0.55))
    return obj


area_light("Studio_Key", (-3, -4, 5), 500, (1.0, 0.84, 0.65), 4)
area_light("Studio_Fill", (3, -2, 3), 220, (1.0, 0.93, 0.82), 3)
area_light("Studio_Rim", (1, 3, 4), 550, (1.0, 0.60, 0.35), 3)

camera_data = bpy.data.cameras.new("Courier_Camera")
camera = bpy.data.objects.new("Courier_Camera", camera_data)
studio_collection.objects.link(camera)

camera.location = (2.2 * SCALE, -6 * SCALE, 2.3 * SCALE)
aim_at(camera, (-0.04 * SCALE, 0, CHARACTER_HEIGHT * 0.52))
camera_data.type = "ORTHO"
camera_data.ortho_scale = CHARACTER_HEIGHT * 1.22
scene.camera = camera

world = bpy.data.worlds.new("Courier_StudioWorld")
world.use_nodes = True
background = next(
    node for node in world.node_tree.nodes
    if node.type == "BACKGROUND"
)
background.inputs["Color"].default_value = (0.16, 0.11, 0.07, 1)
background.inputs["Strength"].default_value = 0.5
scene.world = world

scene.render.resolution_x = 1000
scene.render.resolution_y = 1300
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"

# Выделяем только корневой объект созданного персонажа.
bpy.context.view_layer.update()
for obj in scene.objects:
    obj.select_set(False)

root.select_set(True)
bpy.context.view_layer.objects.active = root

for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type == "VIEW_3D":
            area.spaces.active.region_3d.view_perspective = "CAMERA"
            area.spaces.active.shading.type = "MATERIAL"

# Статистика относится только к геометрии персонажа, без студии.
triangle_count = 0
for obj in model_collection.objects:
    if obj.type == "MESH":
        obj.data.calc_loop_triangles()
        triangle_count += len(obj.data.loop_triangles)

print(
    f"Создана сцена: {scene.name}. "
    f"Рост: около {CHARACTER_HEIGHT:.2f} м. "
    f"Треугольников без учёта неприменённых модификаторов: {triangle_count}. "
    "Исходные сцены сохранены."
)
