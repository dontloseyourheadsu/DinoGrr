extends RigidBody2D

## Builds a filled polygon + collision from the points in the child Line2D.
## The scene is expected to look like:
##   LinePolygon (RigidBody2D) [this script]
##     └─ Line2D
##   StaticBody2D (sibling) → receives CollisionPolygon2D + Polygon2D children.
## A call to `build_from_line()` will:
##   1. Take the current Line2D points.
##   2. Generate one or more offset polygons using Geometry2D.offset_polyline.
##   3. Center the RigidBody2D on the weighted average of all points.
##   4. Create CollisionPolygon2D + Polygon2D visuals under the StaticBody2D.

@export var use_line_width: bool = true # If true, use Line2D.width / 2 for polygon thickness
@export var default_half_width: float = 8.0 # Fallback half-width when Line2D.width is 0
@export var collision_thickness_scale: float = 1.5 # Multiplier to inflate collision vs visual width
@export var clear_previous: bool = true # Remove previously generated Polygon2D / CollisionPolygon2D

@onready var _line: Line2D = $Line2D

func _ready() -> void:
	# Optionally build immediately if there are already points.
	# Keep the rigid body from falling while using it only as a transform anchor.
	freeze_mode = RigidBody2D.FREEZE_MODE_KINEMATIC
	freeze = true
	gravity_scale = 0.0
	if _line.points.size() > 1:
		build_from_line()

## Call before starting a new stroke to keep the body in place while drawing.
func begin_drawing() -> void:
	freeze = true
	gravity_scale = 0.0

## Public API: rebuild polygon & collision from the current line points.
func build_from_line() -> void:
	var pts: PackedVector2Array = _line.points
	if pts.size() < 2:
		return

	var half_width: float = _line.width / 2.0 if (use_line_width and _line.width > 0) else default_half_width
	half_width *= collision_thickness_scale
	var polys: Array = Geometry2D.offset_polyline(pts, half_width)

	if polys.is_empty():
		push_warning("offset_polyline returned no polygons")
		return

	var center: Vector2 = _get_line_center(pts)
	# Convert center from Line2D local space to global, then place the body there.
	global_position = _line.to_global(center)

	if clear_previous:
		_clear_generated_children()

	for poly in polys:
		if typeof(poly) != TYPE_PACKED_VECTOR2_ARRAY:
			continue
		var cleaned: PackedVector2Array = _sanitize_polygon(poly)
		if cleaned.size() < 3:
			continue
		var shifted: PackedVector2Array = _offset_points(center, cleaned)
		_create_visual_and_collision(shifted)

	# Allow physics to take over once the polygon is baked.
	freeze = false
	gravity_scale = 1.0

## Compute a simple weighted average of all points.
func _get_line_center(pts: PackedVector2Array) -> Vector2:
	var count: int = pts.size()
	if count == 0:
		return Vector2.ZERO
	var accum: Vector2 = Vector2.ZERO
	for p in pts:
		accum += p
	return accum / float(count)

## Shift polygon points so that they are relative to the rigidbody (center).
func _offset_points(center: Vector2, poly: PackedVector2Array) -> PackedVector2Array:
	var adjusted: PackedVector2Array = PackedVector2Array()
	for p in poly:
		adjusted.append(p - center)
	return adjusted

func _create_visual_and_collision(local_poly: PackedVector2Array) -> void:
	# Collision
	var col := CollisionPolygon2D.new()
	col.polygon = local_poly
	add_child(col)

	# Visual polygon (optional). Use a semi-transparent color.
	var poly2d := Polygon2D.new()
	poly2d.polygon = local_poly
	poly2d.color = Color(1, 0.15, 0.15, 0.65)
	add_child(poly2d)

func _clear_generated_children() -> void:
	# Remove existing Polygon2D / CollisionPolygon2D that were previously generated.
	for child in get_children():
		if child is Polygon2D or child is CollisionPolygon2D:
			child.queue_free()

## Remove duplicate & near-collinear points, ensure orientation, optionally simplify.
func _sanitize_polygon(poly: PackedVector2Array) -> PackedVector2Array:
	var out := PackedVector2Array()
	if poly.size() == 0:
		return out
	var last_added: Vector2 = poly[0]
	out.append(last_added)
	var tolerance := 0.5
	for i in range(1, poly.size()):
		var p: Vector2 = poly[i]
		if p.distance_to(last_added) <= tolerance:
			continue
		# Drop points that are nearly collinear with previous & next to reduce self intersections.
		var prev: Vector2 = last_added
		var next: Vector2 = poly[(i + 1) % poly.size()]
		var area: float = abs((prev.x * (p.y - next.y) + p.x * (next.y - prev.y) + next.x * (prev.y - p.y)) * 0.5)
		if area <= 0.05 and i + 1 < poly.size():
			continue
		out.append(p)
		last_added = p
	# Close if almost closed.
	if out.size() >= 3 and out[0].distance_to(out[out.size() - 1]) <= tolerance:
		out[out.size() - 1] = out[0]
	# Optional: could apply RDP simplification here if needed.
	# Ensure clockwise orientation (Godot prefers counter-clockwise for polygons; reverse if clockwise)
	if Geometry2D.is_polygon_clockwise(out):
		var reversed := PackedVector2Array()
		for i in range(out.size() - 1, -1, -1):
			reversed.append(out[i])
		out = reversed
	return out
