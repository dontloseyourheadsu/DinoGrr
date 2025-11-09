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

# Input actions used by this controller. Configure these in Project Settings > Input Map.
const INPUT_LEFT := "move_left"
const INPUT_RIGHT := "move_right"
const INPUT_JUMP := "jump"

@onready var animated_sprite: AnimatedSprite2D = $AnimatedSprite2D

# Gravity from project settings for consistency across scenes.
var gravity: float = ProjectSettings.get_setting("physics/2d/default_gravity") as float

func _ready() -> void:
	# Start idle.
	animated_sprite.play("stand")

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
