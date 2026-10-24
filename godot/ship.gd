extends Node3D

var player: Node3D
var prompt: Label
var order_panel: PanelContainer
var accepted := false
var speed := 4.0
var deck := 0

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
	RenderingServer.set_default_clear_color(Color("151221"))
	var camera := Camera3D.new()
	camera.position = Vector3(14, 13, 29)
	camera.rotation = Vector3.ZERO
	add_child(camera)
	camera.look_at(Vector3(0, 3.5, 0))
	camera.fov = 48
	camera.current = true
	var ambient := WorldEnvironment.new()
	var env := Environment.new()
	env.background_mode = Environment.BG_COLOR
	env.background_color = Color("151221")
	env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	env.ambient_light_color = Color("9d80b7")
	env.ambient_light_energy = 1.3
	ambient.environment = env
	add_child(ambient)
	box(Vector3(0,-0.2,0),Vector3(28,0.4,10),Color("253633"))
	box(Vector3(0,4,-5),Vector3(28,8.5,0.3),Color("344744"))
	box(Vector3(-14,4,0),Vector3(.3,8,10),Color("344744"))
	for x in range(-12,14,4):
		box(Vector3(x,8,-2),Vector3(.2,.25,6),Color("16282a"))
		box(Vector3(x,4,-4.8),Vector3(.2,8,.3),Color("17292b"))
	for x in range(-14,14):
		box(Vector3(x+.45,.04,0),Vector3(.92,.04,9.8),Color("39433b"))
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
	box(Vector3(5.3,2.65,-3.05),Vector3(3.85,2.05,.1),Color("362d55"),self,true)
	# Industrial skyline outside the cockpit glass.
	for i in range(7):
		var height := 0.45 + float((i * 7) % 5) * 0.22
		var x := 3.63 + i * 0.54
		box(Vector3(x,1.65 + height * .5,-2.92),Vector3(.48,height,.08),Color("202236"))
		box(Vector3(x,1.75 + height,-2.92),Vector3(.12,.45,.08),Color("242637"))
		box(Vector3(x,1.72 + height * .4,-2.85),Vector3(.15,.055,.035),Color("ee90d4"),self,true)
	for i in range(4):
		box(Vector3(3.8 + i * .9,2.2,-2.87),Vector3(.07,.07,.05),Color("c0a3ff"),self,true)
	box(Vector3(5.7,.7,-2.2),Vector3(2.8,1.2,1.1),Color("1a2e30"))
	box(Vector3(5.5,.8,-.3),Vector3(1,.3,1),Color("8b7056"))
	box(Vector3(5.5,1.3,.1),Vector3(1,1.2,.2),Color("8b7056"))
	box(Vector3(7,.6,1.8),Vector3(1.7,1.2,1.5),Color("6a7665"))
	box(Vector3(3.2,.4,2),Vector3(1.1,.8,1.1),Color("7d634c"))
	var warm := OmniLight3D.new()
	warm.position = Vector3(-6,2,-1)
	warm.light_color = Color("ffbe79")
	warm.light_energy = 3.4
	warm.omni_range = 7
	add_child(warm)
	var cool := OmniLight3D.new()
	cool.position = Vector3(4,3,-1)
	cool.light_color = Color("ad81e8")
	cool.light_energy = 4.0
	cool.omni_range = 9
	add_child(cool)
	var purple := OmniLight3D.new()
	purple.position = Vector3(0,3.3,1.3)
	purple.light_color = Color("b56cfa")
	purple.light_energy = 4.2
	purple.omni_range = 11
	add_child(purple)
	var window_glow := OmniLight3D.new()
	window_glow.position = Vector3(5.3,2.8,-2.5)
	window_glow.light_color = Color("d69cf7")
	window_glow.light_energy = 3.3
	window_glow.omni_range = 7
	add_child(window_glow)
	# Mezzanine: open-front upper living deck, accessed by the central lift.
	box(Vector3(-6,3.9,-.8),Vector3(15.6,.3,7.8),Color("3e354b"))
	box(Vector3(-6,4.07,-.8),Vector3(15.5,.04,7.7),Color("725664"))
	for x in range(-13,2,2):
		box(Vector3(x,4.65,3),Vector3(.06,1.2,.06),Color("bc9690"))
	box(Vector3(-6,5.25,3),Vector3(15.6,.06,.06),Color("d1a58d"))
	box(Vector3(-6,4,3.1),Vector3(15.6,.05,.05),Color("ed8ec6"),self,true)
	# Bedroom, reading corner, records and books upstairs.
	box(Vector3(-10,4.4,-2.5),Vector3(4.3,.6,2.6),Color("40384d"))
	box(Vector3(-10,4.8,-2.5),Vector3(4.2,.25,2.5),Color("d5bfa7"))
	box(Vector3(-9.5,5,-2.5),Vector3(3,.18,2.55),Color("a47691"))
	box(Vector3(-11.6,5,-2.5),Vector3(.8,.25,1.8),Color("e6c8ad"))
	box(Vector3(-5,4.13,-1),Vector3(3.4,.04,3),Color("927b80"))
	box(Vector3(-4.6,4.6,-2),Vector3(1.5,.9,1.5),Color("b37a88"))
	box(Vector3(-4.6,5.2,-2.6),Vector3(1.5,1.2,.25),Color("b37a88"))
	box(Vector3(-2.5,4.7,-2),Vector3(1.5,.12,1),Color("c09c80"))
	for y in [4.3,5.2,6.1]:
		box(Vector3(-6,y,-4.6),Vector3(4,.1,.6),Color("ae8879"))
		for i in range(10):
			box(Vector3(-7.7+i*.36,y+.3,-4.6),Vector3(.22,.5,.4),Color.from_hsv(float(i)/12,.3,.7))
	# Spacious lower lounge and cargo area.
	box(Vector3(-10,.08,1.4),Vector3(5,.05,3.8),Color("775b78"))
	box(Vector3(-11.6,.6,.6),Vector3(1.7,.9,3.5),Color("82637f"))
	box(Vector3(-12.3,1.2,.6),Vector3(.3,1.4,3.5),Color("82637f"))
	box(Vector3(-9.5,.6,1.2),Vector3(2,.15,1.4),Color("b98e77"))
	box(Vector3(-9.5,.3,1.2),Vector3(.25,.6,.8),Color("42384a"))
	box(Vector3(-9.5,.8,1.2),Vector3(.2,.25,.2),Color("efd3ad"))
	for x in [10.0,12.0]:
		box(Vector3(x,.7,3.5),Vector3(1.6,1.4,1.5),Color("716579"))
		box(Vector3(x,.8,4.27),Vector3(.8,.15,.03),Color("e1b787"))
	# Tall bridge window and neon skyline inspired by a violet Taipei dusk.
	box(Vector3(8,5.4,-4.8),Vector3(10,4.6,.1),Color("674778"),self,true)
	for i in range(18):
		var h := .7 + float((i*7)%11)*.24
		var x := 3.4 + float(i)*.53
		box(Vector3(x,3.2+h*.5,-4.65),Vector3(.45,h,.08),Color("292a44"))
		for j in range(5):
			box(Vector3(x,3.3+float(j)*h/6,-4.59),Vector3(.24,.04,.02),Color("eda6cc"),self,true)
	box(Vector3(8,5.5,-4.5),Vector3(.6,4.3,.12),Color("33314c"))
	box(Vector3(8,8,-4.5),Vector3(.06,.8,.06),Color("edb0d6"),self,true)
	for x in [3.0,8.0,13.0]:
		box(Vector3(x,5.4,-4.4),Vector3(.1,4.7,.15),Color("242b3d"))
	# Lift landings are reachable from the walking lane on either floor.
	for y in [.08,4.1]:
		box(Vector3(.3,y,1.8),Vector3(2.3,.08,2.1),Color("8c729d"))
		box(Vector3(.3,y+.06,2.86),Vector3(2.3,.04,.04),Color("9cd8d2"),self,true)
	for x in [-.9,1.5]:
		box(Vector3(x,2.1,.8),Vector3(.06,4.2,.06),Color("aba0b0"))
	# Plants, warm lamps and a sagging string of bulbs.
	for pos in [Vector3(-13,0,2),Vector3(12,0,-2),Vector3(-12,4.1,1.5),Vector3(-2.6,4.1,-.8)]:
		box(pos+Vector3(0,.3,0),Vector3(.65,.6,.65),Color("b38270"))
		for i in range(3):
			var leaf := box(pos+Vector3((i-1)*.22,1+i*.12,0),Vector3(.25,1.1,.25),Color("739d86"))
			leaf.rotation.z = (i-1)*.35
	for i in range(17):
		var y := 7.4-sin(float(i)/16*PI)*.6
		box(Vector3(-13+i*.9,y,1.6),Vector3(.15,.2,.15),Color("ffd2a0"),self,true)
	for pos in [Vector3(-10,2,1),Vector3(-9,6,-1),Vector3(-3,6,0)]:
		var lamp := OmniLight3D.new()
		lamp.position = pos
		lamp.light_color = Color("ffc498")
		lamp.light_energy = 2.5
		lamp.omni_range = 7
		add_child(lamp)
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
	var hint := label("WASD / стрелки — ходить     E — терминал / лифт     Esc — закрыть",16)
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
	player.position.x = clampf(player.position.x,-13.2,1.0 if deck == 1 else 13.2)
	player.position.z = clampf(player.position.z,-.8,2.5 if deck == 1 else 4.4)
	if direction.length() > 0:
		player.rotation.y = atan2(direction.x,direction.z)
	var near := deck == 0 and player.position.distance_to(Vector3(.4,0,-.6)) < 2.0
	prompt.text = "E · Посмотреть заказ" if near and accepted else "E · Открыть терминал" if near else ""
	if Vector2(player.position.x-.3,player.position.z-1.8).length() < 1.1:
		prompt.text = "E · Лифт — " + ("в каюту наверху" if deck == 0 else "в гостиную")
	if order_panel.visible:
		prompt.text = ""

func _unhandled_key_input(event: InputEvent) -> void:
	if not event is InputEventKey or not event.pressed or event.echo:
		return
	if event.keycode == KEY_ESCAPE:
		order_panel.hide()
	if event.keycode != KEY_E or order_panel.visible:
		return
	if Vector2(player.position.x-.3,player.position.z-1.8).length() < 1.1:
		deck = 1-deck
		player.position = Vector3(.3,4.1 if deck == 1 else 0.0,1.8)
	elif deck == 0 and player.position.distance_to(Vector3(.4,0,-.6)) < 2.0:
		order_panel.show()
