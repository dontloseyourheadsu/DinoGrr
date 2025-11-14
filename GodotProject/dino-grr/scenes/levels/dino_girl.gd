extends CharacterBody2D

# Player controller for DinoGirl.
#
# Key ideas:
# - Run physics in _physics_process() for deterministic movement.
# - Apply gravity only when not on the floor.
# - Compute horizontal direction once via Input.get_axis().
# - Only switch animations when the state actually changes to avoid
#   restarting animations every frame.

@export var speed: float = 800.0 # Horizontal speed in pixels/second.
@export var jump_velocity: float = -600.0 # Negative to move up in Godot's Y-down coordinates.
@export var max_life: int = 4 # Number of hearts representing DinoGirl's life.
var life: int = 0 # Current life in hearts; initialized on _ready to max_life.

# Heart UI
@export var heart_texture: Texture2D # Assign in inspector; falls back to res://dinogirl/sprites/heart.png if empty.
@export var heart_scale: float = 1.0
@export var heart_spacing: float = 4.0 # Extra pixels between hearts beyond texture width

# Drawing points (TintBar represents these)
@export var max_draw_points: int = 150
var draw_points: int = 0

# Input actions used by this controller. Configure these in Project Settings > Input Map.
const INPUT_LEFT := "move_left"
const INPUT_RIGHT := "move_right"
const INPUT_JUMP := "jump"

@onready var animated_sprite: AnimatedSprite2D = $AnimatedSprite2D
@onready var life_container: Node2D = $LifeContainer
@onready var tint_bar: ProgressBar = $TintBar

# Gravity from project settings for consistency across scenes.
var gravity: float = ProjectSettings.get_setting("physics/2d/default_gravity") as float

func _ready() -> void:
	# Start idle.
	animated_sprite.play("stand")

	# Initialize life in hearts and draw heart UI.
	life = max_life
	_setup_hearts()

	# Initialize drawing points (TintBar configured in scene file)
	draw_points = max_draw_points

func _physics_process(delta: float) -> void:
	# Apply gravity when airborne.
	if not is_on_floor():
		velocity.y += gravity * delta

	# Horizontal movement: -1 (left) to +1 (right).
	var direction_x: float = Input.get_axis(INPUT_LEFT, INPUT_RIGHT)
	if direction_x != 0.0:
		velocity.x = direction_x * speed
		apply_flip(direction_x < 0.0)
	else:
		# Come to a stop. For smoother motion, replace with accel/decel ramps.
		velocity.x = move_toward(velocity.x, 0.0, speed)

	# Jump on press while grounded.
	if Input.is_action_just_pressed(INPUT_JUMP):
		perform_jump()

	# Update animation based on current state.
	update_animation(direction_x)

	# Perform the actual Kinematic movement.
	move_and_slide()


func perform_jump() -> void:
	if is_on_floor():
		velocity.y = jump_velocity

func apply_flip(flip: bool) -> void:
	animated_sprite.flip_h = flip

func update_animation(direction_x: float) -> void:
	if not is_on_floor():
		animated_sprite.play("jump")
		return
	elif abs(direction_x) > 0.0:
		if animated_sprite.animation != "run":
			animated_sprite.play("run")
	else:
		if animated_sprite.animation != "stand":
			animated_sprite.play("stand")

# Public helper to update life (in hearts) and keep the UI in sync.
func set_life(new_value: int) -> void:
	life = int(clamp(new_value, 0, max_life))
	_update_hearts_visibility()

# Public helper for drawing points
func set_draw_points(new_value: int) -> void:
	draw_points = int(clamp(new_value, 0, max_draw_points))
	if is_instance_valid(tint_bar):
		tint_bar.value = draw_points

func consume_draw_point() -> bool:
	if draw_points > 0:
		set_draw_points(draw_points - 1)
		return true
	return false

# Internal: create or refresh heart sprites under LifeContainer
func _setup_hearts() -> void:
	if not is_instance_valid(life_container):
		return
	# Resolve texture if not assigned in inspector.
	if heart_texture == null:
		var tex: Resource = load("res://dinogirl/sprites/heart.png")
		if tex is Texture2D:
			heart_texture = tex
		else:
			push_warning("Heart texture not set and fallback not found at res://dinogirl/sprites/heart.png")

	# Clear previous heart nodes.
	for c in life_container.get_children():
		c.queue_free()

	if heart_texture == null:
		return

	var tex_size: Vector2 = Vector2(heart_texture.get_size())
	var step_x: float = tex_size.x * heart_scale + heart_spacing
	# Draw hearts left-to-right anchored at LifeContainer's origin.
	for i in range(max_life):
		var spr := Sprite2D.new()
		spr.texture = heart_texture
		spr.centered = false
		spr.position = Vector2(i * step_x, 0)
		spr.scale = Vector2(heart_scale, heart_scale)
		life_container.add_child(spr)

	_update_hearts_visibility()

# Internal: dim hearts beyond current life
func _update_hearts_visibility() -> void:
	if not is_instance_valid(life_container):
		return
	var i := 0
	for child in life_container.get_children():
		if child is Sprite2D:
			(child as Sprite2D).modulate.a = 1.0 if i < life else 0.25
			i += 1