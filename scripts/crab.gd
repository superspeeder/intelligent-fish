extends Node3D

@export var move_speed: float = .5
@export var turn_speed: float = 1.0
@export var tilt_speed: float = 20
@export var ground_offset: float = 0.2

#@onready var first_left_step_target = $crab/body/stepTargets/FirstLeftRay/FirstLeftStepTarget
#@onready var second_left_step_target = $crab/body/stepTargets/SecondLeftRay/SecondLeftStepTarget
#@onready var third_left_step_target = $crab/body/stepTargets/ThirdLeftRay/ThirdLeftStepTarget
#@onready var fourth_left_step_target = $crab/body/stepTargets/FourthLeftRay/FourthLeftStepTarget
#@onready var first_right_step_target = $crab/body/stepTargets/FirstRightRay/FirstRightStepTarget
#@onready var second_right_step_target = $crab/body/stepTargets/SecondRightRay/SecondRightStepTarget
#@onready var third_right_step_target = $crab/body/stepTargets/ThirdRightRay/ThirdRightStepTarget
#@onready var fourth_right_step_target = $crab/body/stepTargets/RayCast3D/FourthRightStepTarget


#@onready var first_left_ik_target = $crab/body/stepTargets/FirstLeftRay/FirstLeftStepTarget
#@onready var second_left_ik_target = $crab/body/stepTargets/SecondLeftRay/SecondLeftStepTarget
#@onready var third_left_ik_target = $crab/body/stepTargets/ThirdLeftRay/ThirdLeftStepTarget
#@onready var fourth_left_ik_target =  $crab/body/stepTargets/FourthLeftRay/FourthLeftStepTarget
#@onready var first_right_ik_target = $crab/body/stepTargets/FirstRightRay/FirstRightStepTarget
#@onready var second_right_ik_target = $crab/body/stepTargets/SecondRightRay/SecondRightStepTarget
#@onready var third_right_ik_target = $crab/body/stepTargets/ThirdRightRay/ThirdRightStepTarget
#@onready var fourth_right_ik_target = $crab/body/stepTargets/RayCast3D/FourthRightStepTarget

@onready var first_left_ik_target = $crab/body/FirstLeftIkTarget
@onready var second_left_ik_target = $crab/body/SecondLeftIkTarget
@onready var third_left_ik_target = $crab/body/ThirdLeftIkTarget
@onready var fourth_left_ik_target = $crab/body/FourthLeftIkTarget
@onready var first_right_ik_target = $crab/body/FirstRightIkTarget
@onready var second_right_ik_target = $crab/body/SecondRightIkTarget
@onready var third_right_ik_target = $crab/body/ThirdRightIkTarget
@onready var fourth_right_ik_target = $crab/body/FourthRightIkTarget

@onready var turn_timer = $crab/TimerHolder/turnTimer
@onready var wait_timer = $crab/TimerHolder/waitTimer
@onready var walk_timer = $crab/TimerHolder/walkTimer
@onready var walk_turn_timer = $crab/TimerHolder/walkTurnTimer

var temp_move_speed :float = 0
var temp_turn_speed :float = 0

# Called when the node enters the scene tree for the first time.
func _ready():
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _physics_process(delta):
	
	var leg_normal = get_terrain_alignment_normal()
	var align_quat = Quaternion(transform.basis.y, leg_normal)
	var target_basis = Basis(align_quat) * transform.basis
	
	transform.basis = transform.basis.slerp(target_basis, tilt_speed * delta).orthonormalized()
	
	var avg = leg_avg()
	var target_pos = avg + transform.basis.y * ground_offset
	var distance = transform.basis.y.dot(target_pos - position)
	position = lerp(position, position + transform.basis.y * distance, move_speed * delta)
	
	if position.x > 100 or position.x < -100:
		temp_move_speed = -temp_move_speed
		
	if position.y > 100 or position.y < -100:
		temp_move_speed = -temp_move_speed
	
	if !turn_timer.is_stopped():
		turn(delta)
		
	elif !wait_timer.is_stopped():
		#print("waiting")
		pass
	elif !walk_timer.is_stopped():
		#print("walking")
		move(delta)
	elif !walk_turn_timer.is_stopped():
		move(delta)
		turn(delta)
		
	else:
		#enable random timer for a random amount of time
		var option = randi_range(0,3)
		match option:
			0:#turn
				#print("set turn")
				temp_turn_speed = turn_speed * -randi_range(0,1)
				turn_timer.wait_time = randf()*3
				turn_timer.start()
			1:#wait
				#print("set wait")
				wait_timer.wait_time = randf()*.5
				wait_timer.start()
			2:#walk
				temp_move_speed = move_speed * -randi_range(0,1)
				walk_timer.wait_time = randf()*10
				walk_timer.start()
				#print("set walk")
			3:#walk and turn
				temp_move_speed = move_speed * -randi_range(0,1)
				temp_turn_speed = turn_speed * randf()
				walk_turn_timer.wait_time = randf()*7
				walk_turn_timer.start()
				#print("set turn")
		
	
	pass



func basis_from_normal(normal: Vector3) ->Basis:
	var result = Basis()
	result.x = normal.cross(transform.basis.z)
	result.y = normal
	result.z = transform.basis.x.cross(normal)
	
	result = result.orthonormalized()
	result.x *= scale.x
	result.y *= scale.y
	result.z *= scale.z
	
	return result
	

func leg_avg() -> Vector3:
	var result := Vector3()
	
	result = result + first_left_ik_target.position
	result = result + second_left_ik_target.position
	result = result + third_left_ik_target.position
	result = result + fourth_left_ik_target.position
	result = result + first_right_ik_target.position
	result = result + second_right_ik_target.position
	result = result + third_right_ik_target.position
	result = result + fourth_right_ik_target.position
	
	result = result / 8
	
	return result
	

func get_terrain_alignment_normal() -> Vector3:
	var avg_left_pos = (first_left_ik_target.global_position + second_left_ik_target.global_position + third_left_ik_target.global_position + fourth_left_ik_target.global_position) / 4.0
	var avg_right_pos = (first_right_ik_target.global_position + second_right_ik_target.global_position + third_right_ik_target.global_position + fourth_right_ik_target.global_position) / 4.0
	
	var avg_front_pos = (first_left_ik_target.global_position + first_right_ik_target.global_position) / 2.0
	var avg_back_pos = (fourth_left_ik_target.global_position + fourth_right_ik_target.global_position) / 2.0
	
	var right_dir = (avg_right_pos - avg_left_pos).normalized()
	var forward_dir = (avg_front_pos - avg_back_pos).normalized()
	
	if right_dir == Vector3.ZERO or forward_dir == Vector3.ZERO:
		return Vector3.UP
		
	var body_normal = forward_dir.cross(right_dir).normalized()
	
	return body_normal

func move(delta):
	translate(Vector3(1 ,0, 0)* temp_move_speed * delta)

func turn(delta):
	rotate_object_local(Vector3.UP, temp_turn_speed * delta)
