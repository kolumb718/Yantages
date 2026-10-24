extends Node3D

var player: Node3D
var prompt: Label
var order_panel: PanelContainer
var accepted := false
var speed := 3.0

func material(color: Color, glow := false) -> StandardMaterial3D:
	var m := StandardMaterial3D.new()
	m.albedo_color = color
	m.roughness = 0.9
	if glow:
		m.emission_enabled = true
		m.emission = color
		m.emission_energy_multiplier = 1.5
	return m

func box(pos: Vector3, size: Vector3, color: Color, parent: Node3D = self, glow := false) -> MeshInstance3D:
	var mesh := BoxMesh.new()
	mesh.size = size
	var node := MeshInstance3D.new()
	node.mesh = mesh
	node.material_override = material(color, glow)
	node.position = pos
	parent.add_child(node)
	return node

func label(text: String, size: int = 18) -> Label:
	var l := Label.new()
	l.text = text
	l.add_theme_font_size_override("font_size", size)
	l.add_theme_color_override("font_color", Color("dce5d9"))
	return l

func _ready() -> void:
	RenderingServer.set_default_clear_color(Color("101c22"))
	var camera := Camera3D.new()
	camera.position = Vector3(14, 10, 22)
	camera.rotation = Vector3.ZERO
	add_child(camera)
	camera.look_at(Vector3(0, 1.5, 0))
	camera.fov = 43
	camera.current = true
	var ambient := WorldEnvironment.new()
	var env := Environment.new()
	env.background_mode = Environment.BG_COLOR
	env.background_color = Color("101c22")
	env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	env.ambient_light_color = Color("789096")
	env.ambient_light_energy = 0.8
	ambient.environment = env
	add_child(ambient)
	box(Vector3(0,-0.2,0),Vector3(17,0.4,7),Color("253633"))
	box(Vector3(0,2.2,-3.45),Vector3(17,4.6,0.3),Color("344744"))
	box(Vector3(-8.5,2,0),Vector3(.3,4,7),Color("344744"))
	for x in range(-8,9,4):
		box(Vector3(x,4.4,0),Vector3(.2,.25,7),Color("16282a"))
		box(Vector3(x,2.2,-3.2),Vector3(.2,4.5,.3),Color("17292b"))
	for x in range(-8,8):
		box(Vector3(x+.45,.04,0),Vector3(.92,.04,6.8),Color("39433b"))
	# Living corner
	box(Vector3(-5.6,.55,-1.9),Vector3(3.3,.5,1.7),Color("1d3030"))
	box(Vector3(-5.6,.9,-1.9),Vector3(3.1,.2,1.55),Color("a99f80"))
	box(Vector3(-5.2,1.05,-1.9),Vector3(2,.2,1.55),Color("667763"))
	box(Vector3(-7.2,1.3,-2),Vector3(.55,.4,.5),Color("c4ac78"))
	box(Vector3(-2.2,.75,-2.5),Vector3(2,1.5,1.2),Color("253736"))
	box(Vector3(-2.2,1.55,-2.5),Vector3(2.2,.12,1.4),Color("b1a682"))
	box(Vector3(-2.2,3,-3),Vector3(2,.7,.7),Color("697667"))
	# Terminal and cockpit
	box(Vector3(.4,.65,-1.9),Vector3(1.4,1.3,.9),Color("1b3030"))
	box(Vector3(.4,1.8,-2),Vector3(1.65,1.2,.4),Color("364c48"))
	box(Vector3(.4,1.9,-1.77),Vector3(1.35,.85,.08),Color("8cd5a9"),self,true)
	box(Vector3(.4,1.15,-1.4),Vector3(1.65,.13,.7),Color("66796d"))
	box(Vector3(5.3,2.65,-3.2),Vector3(4.4,2.5,.2),Color("182d31"))
	box(Vector3(5.3,2.65,-3.05),Vector3(3.85,2.05,.1),Color("386475"),self,true)
	for i in range(12):
		box(Vector3(3.55+i*.29,1.7,-2.95),Vector3(.2,.7,.06),Color("1c3c48"))
	box(Vector3(5.7,.7,-2.2),Vector3(2.8,1.2,1.1),Color("1a2e30"))
	box(Vector3(5.5,.8,-.3),Vector3(1,.3,1),Color("8b7056"))
	box(Vector3(5.5,1.3,.1),Vector3(1,1.2,.2),Color("8b7056"))
	box(Vector3(7,.6,1.8),Vector3(1.7,1.2,1.5),Color("6a7665"))
	box(Vector3(3.2,.4,2),Vector3(1.1,.8,1.1),Color("7d634c"))
	var warm := OmniLight3D.new()
	warm.position = Vector3(-6,2,-1)
	warm.light_color = Color("ffbe79")
	warm.light_energy = 2.5
	warm.omni_range = 7
	add_child(warm)
	var cool := OmniLight3D.new()
	cool.position = Vector3(4,3,-1)
	cool.light_color = Color("91c9d2")
	cool.light_energy = 1.7
	cool.omni_range = 9
	add_child(cool)
	player = Node3D.new()
	player.position = Vector3(-4,0,.6)
	add_child(player)
	box(Vector3(0,.85,0),Vector3(.5,.7,.38),Color("b99a61"),player)
	box(Vector3(0,1.45,0),Vector3(.4,.42,.4),Color("a59376"),player)
	box(Vector3(-.15,.25,0),Vector3(.17,.5,.22),Color("252d2b"),player)
	box(Vector3(.15,.25,0),Vector3(.17,.5,.22),Color("252d2b"),player)
	var ui := CanvasLayer.new()
	add_child(ui)
	var top := label("ОРБИТА   /   МЕЖПЛАНЕТНАЯ ДОСТАВКА",23)
	top.position = Vector2(35,28)
	ui.add_child(top)
	var hint := label("WASD / стрелки — ходить     E — терминал     Esc — закрыть",16)
	hint.position = Vector2(35,665)
	ui.add_child(hint)
	prompt = label("",20)
	prompt.position = Vector2(470,585)
	ui.add_child(prompt)
	order_panel = PanelContainer.new()
	order_panel.position = Vector2(390,130)
	order_panel.custom_minimum_size = Vector2(500,430)
	ui.add_child(order_panel)
	var margin := MarginContainer.new()
	margin.add_theme_constant_override("margin_left",28)
	margin.add_theme_constant_override("margin_top",25)
	margin.add_theme_constant_override("margin_right",28)
	margin.add_theme_constant_override("margin_bottom",25)
	order_panel.add_child(margin)
	var column := VBoxContainer.new()
	column.add_theme_constant_override("separation",17)
	margin.add_child(column)
	column.add_child(label("ОРБИТА / ВХОДЯЩИЕ",15))
	column.add_child(label("Ужин на краю\nСолнечной системы",28))
	column.add_child(label("ЗЕМЛЯ  →  НЕПТУН\n\nПолучатель: Мира, станция «Тритон»\nЗаказ: горячий суп с лунными грибами\nОплата: 240 кредитов\n\n«Не переворачивать. Суп живой.\nИ захватите ложку»."))
	var accept := Button.new()
	accept.text = "Принять заказ"
	column.add_child(accept)
	accept.pressed.connect(func():
		accepted = true
		accept.text = "Заказ принят ✓"
		accept.disabled = true
		column.add_child(label("Следующий шаг: забрать суп на Земле."))
	)
	order_panel.hide()

func _process(delta: float) -> void:
	var direction := Vector3.ZERO
	if not order_panel.visible:
		if Input.is_key_pressed(KEY_D) or Input.is_key_pressed(KEY_RIGHT): direction.x += 1
		if Input.is_key_pressed(KEY_A) or Input.is_key_pressed(KEY_LEFT): direction.x -= 1
		if Input.is_key_pressed(KEY_S) or Input.is_key_pressed(KEY_DOWN): direction.z += 1
		if Input.is_key_pressed(KEY_W) or Input.is_key_pressed(KEY_UP): direction.z -= 1
	player.position += direction.normalized() * speed * delta
	player.position.x = clampf(player.position.x,-7.6,7.6)
	player.position.z = clampf(player.position.z,-.8,2.7)
	if direction.length() > 0:
		player.rotation.y = atan2(direction.x,direction.z)
	var near := player.position.distance_to(Vector3(.4,0,-.6)) < 2.0
	prompt.text = "E · Посмотреть заказ" if near and accepted else "E · Открыть терминал" if near else ""
	if Input.is_key_pressed(KEY_ESCAPE): order_panel.hide()
	if Input.is_key_pressed(KEY_E) and near and not order_panel.visible: order_panel.show()
