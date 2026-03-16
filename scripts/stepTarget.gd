extends Marker3D

@export var step_target:Node3D
@export var step_distance: float = .19
@export var step_hight: float = .13

@export var adjacent_target1: Node3D
@export var adjacent_target2: Node3D

var is_stepping := false

# Called when the node enters the scene tree for the first time.
func _ready():
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	if abs(global_position.distance_to(step_target.global_position)) > step_distance and !is_stepping and !adjecent_moving():
		step()
	pass

func step():
	#print("step")
	var target_pos = step_target.global_position
	var half_way = (global_position + step_target.global_position)/2
	is_stepping = true
	var t = get_tree().create_tween()
	t.tween_property(self, "global_position", half_way + (owner.basis.y * step_hight), 0.1)
	t.tween_property(self, "global_position", target_pos, 0.1)
	t.tween_callback(func(): is_stepping = false)


func adjecent_moving() -> bool:
	if adjacent_target1 != null:
		return adjacent_target1.is_stepping
	
	if adjacent_target2 != null:
		return adjacent_target2.is_stepping
	
	return !(!adjacent_target2.is_stepping or !adjacent_target2.is_stepping)
	pass
