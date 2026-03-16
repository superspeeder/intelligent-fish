extends RayCast3D

@export var step_target: Node3D

# Called when the node enters the scene tree for the first time.
func _ready():
	pass # Replace with function body.


func _physics_process(delta):
	var hit_point = get_collision_point()
	if hit_point:
		step_target.global_position = hit_point
