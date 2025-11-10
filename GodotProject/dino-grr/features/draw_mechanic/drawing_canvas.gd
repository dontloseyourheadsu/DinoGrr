extends Node2D

## Handles mouse input to draw a Line2D in the LinePolygon child and then
## triggers polygon & collision generation.

@export var min_point_distance: float = 4.0
@export var line_width: float = 24.0
@export var line_color: Color = Color8(255, 191, 0, 255) # FFBF00
@export var hide_line_after_finalize: bool = true
@export var line_polygon_scene: PackedScene = preload("res://features/draw_mechanic/line_polygon/line_polygon.tscn")

var _current_line_polygon: Node = null
var _current_line: Line2D = null

var _is_drawing: bool = false

func _ready() -> void:
	# Nothing to configure yet; we'll set up the Line2D on each new instance.
	pass

func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT:
		if event.pressed:
			_begin_draw()
		else:
			_end_draw()

func _process(_delta: float) -> void:
	if _is_drawing:
		_append_point_if_far_enough(get_global_mouse_position())

func _begin_draw() -> void:
	_is_drawing = true
	# Instance a fresh LinePolygon for this stroke.
	_current_line_polygon = line_polygon_scene.instantiate()
	add_child(_current_line_polygon)
	_current_line = _current_line_polygon.get_node("Line2D") as Line2D
	# Configure the visible line
	if line_width > 0:
		_current_line.width = line_width
	# Ensure no gradient overrides the default color
	_current_line.gradient = null
	_current_line.default_color = line_color
	_current_line.points = PackedVector2Array()
	_current_line.visible = true
	if _current_line_polygon.has_method("begin_drawing"):
		_current_line_polygon.begin_drawing()
	_append_point_if_far_enough(get_global_mouse_position())

func _end_draw() -> void:
	if not _is_drawing:
		return
	_is_drawing = false
	# Finalize shape.
	if _current_line and _current_line.points.size() > 1 and _current_line_polygon and _current_line_polygon.has_method("build_from_line"):
		_current_line_polygon.build_from_line()
	if hide_line_after_finalize:
		if _current_line:
			_current_line.visible = false
	_current_line = null
	_current_line_polygon = null

func _append_point_if_far_enough(global_pos: Vector2) -> void:
	if _current_line == null:
		return
	var local_pos: Vector2 = _current_line.to_local(global_pos)
	var pts: PackedVector2Array = _current_line.points
	if pts.size() == 0 or pts[pts.size() - 1].distance_to(local_pos) >= min_point_distance:
		pts.append(local_pos)
		_current_line.points = pts
