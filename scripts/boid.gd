extends CharacterBody3D

@onready var rad_visualizer = $rad_visualizer


const SPEED = 5.0
var boids_rad: float


var parent:Node3D

func _ready():
	parent = get_parent_node_3d()
	boids_rad = rad_visualizer.shape.radius ** 2 #square it so its faster
	
	velocity = Vector3()
	

func _physics_process(delta):
	
	var rule1: Vector3 = Vector3(0, 0, 0) #steer to avoid crowding local flockmates
	var rule2: Vector3 = Vector3(0, 0, 0) #steer towards the average heading of local flockmates
	var rule3: Vector3 = Vector3(0, 0, 0) #steer to move towards the average position (center of mass) of local flockmates
	var rule4: Vector3 = Vector3(0, 0, 0) #obstical avoidance
	#var rule5: Vector3 = Vector3(0, 0, 0) #goal seeking to be implemented
	
	var number_of_boids_near: int = 0;
	for child in parent.get_children():
		if child == self: #ignore ourselves
			continue
		
		var dist:Vector3 = child.position - self.position
		
		if dist.length_squared() > boids_rad: 
			number_of_boids_near += 1
		
		rule1 = rule1 - dist
		
		rule2 = rule2 + child.position
		
		
	

	move_and_slide()
