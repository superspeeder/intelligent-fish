extends Node3D

const boid_res: PackedScene = preload("res://Models/blender/fish.blend")

####This shit be using the packed vector for performance
@export var num_boids:int = 200
@export var spawn_radius:float = 20
@export var boid_vision_radius_squared:float = 9

@export_range(0.0, 6.0, 0.01) var rule1_strength: float = 1
@export_range(0.0, 6.0, 0.01) var rule2_strength: float = 1
@export_range(0.0, 6.0, 0.01) var rule3_strength: float = 1
@export_range(0.0, 6.0, 0.01) var rule4_strength: float = 1
@export_range(0.0, 6.0, 0.01) var rule5_strength: float = 1

var boid_positions := PackedVector3Array()
var boid_velocity := PackedVector3Array()
var boid_mesh: Array[Node3D] = Array[Node3D]

# Called when the node enters the scene tree for the first time.
func _ready():
	for i in range(num_boids):
		#spawn location stuff
		var random_dir = Vector3(
			randf_range(-1.0, 1.0), 
			randf_range(-1.0, 1.0), 
			randf_range(-1.0, 1.0)
		).normalized()
		var random_vel = Vector3(
			randf_range(-1.0, 1.0), 
			randf_range(-1.0, 1.0), 
			randf_range(-1.0, 1.0)
		).normalized()
		
		var boid_position = random_dir * randf() * spawn_radius
		var boid:Node3D = boid_res.instantiate()
		
		boid_positions.append(boid_position)
		boid_velocity.append(random_vel)
		
		boid.position = boid_position
		add_child(boid)
		boid_mesh.append(boid)
		
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	
	#todo optimization step where this is called every random range of frames
	#todo octree optimizations
	
	#idk if it matters in godot but im gonna put these here so it avoids alocating and dealocating memory
	var number_of_boids_near: int = 0;
	var dist: float = 0;
	
	#iterate through all boids
	for boid1 in range(num_boids):
		#rules for boids
		var rule1: Vector3 = Vector3(0, 0, 0) #steer to avoid crowding local flockmates
		var rule2: Vector3 = Vector3(0, 0, 0) #steer towards the average heading of local flockmates
		var rule3: Vector3 = Vector3(0, 0, 0) #steer to move towards the average position (center of mass) of local flockmates
		var rule4: Vector3 = Vector3(0, 0, 0) #obstical avoidance
		#var rule5: Vector3 = Vector3(0, 0, 0) #goal seeking to be implemented
		
		number_of_boids_near = 0
		
		#for each boid iterate through that
		for boid2 in range(num_boids):
			
			if boid1 == boid2:#skip if we would be refering to ourselfs
				continue
			
			#check if in radius if not skip
			dist = boid_positions.get(boid1).distance_squared_to(boid_positions.get(boid2))
			if dist > boid_vision_radius_squared:
				continue
			number_of_boids_near += 1;
			
			
			#mmk now do some vector math lets a go wahoo! (reference to the game series super mario in case you didnt know)
			rule1 = rule1 + (boid_positions.get(boid1) - boid_positions.get(boid2))
			
			rule2 = rule2 + boid_velocity.get(boid2)
			
			rule3 = rule3 + boid_positions.get(boid2)
		
		
		
	pass
