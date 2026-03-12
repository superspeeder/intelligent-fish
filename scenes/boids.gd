extends Node3D

const boid_res: PackedScene = preload("res://Models/blender/fish.blend")

####This shit be using the packed vector for performance

@export var num_boids:int = 200
@export var spawn_radius:float = 20
@export var boid_vision_radius_squared:float = 9
@export var boid_speed:float = 1.5
@export var boid_turn_speed:float = 1.5
@export var ray_length:float = 1.5

##steer to avoid crowding local flockmates
@export_range(0.0, 6.0, 0.01) var rule1_strength: float = 1
##steer towards the average heading of local flockmates
@export_range(0.0, 6.0, 0.01) var rule2_strength: float = 1
##steer to move towards the average position (center of mass) of local flockmates
@export_range(0.0, 6.0, 0.01) var rule3_strength: float = 1
##obstical avoidance
@export_range(0.0, 6.0, 0.01) var rule4_strength: float = 1
#@export_range(0.0, 6.0, 0.01) var rule5_strength: float = 1

@export var boundary_size: float = 10

var boid_positions := PackedVector3Array()
var boid_velocity := PackedVector3Array()
var boid_mesh: Array[Node3D] = []

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
func _physics_process(delta):
	
	#todo optimization step where this is called every random range of frames (actually unsure how possible this is)
	#todo octree optimizations
	#todo multi threading
	
	#idk if it matters in godot but im gonna put these here so it avoids alocating and dealocating memory
	var number_of_boids_near: int = 0;
	var dist: float = 0;
	
	var space_state = get_world_3d().direct_space_state
	
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
		for boid2 in range(num_boids): #todo also avoid boids behind with dot product
			
			if boid1 == boid2:#skip if we would be refering to ourselfs
				continue
			
			#check if in radius if not skip
			dist = boid_positions.get(boid1).distance_squared_to(boid_positions.get(boid2))
			if dist > boid_vision_radius_squared:
				continue
			number_of_boids_near += 1;
			
			
			#mmk now do some vector math lets a go wahoo! (reference to the game series super mario in case you didnt know)
			var diff = boid_positions.get(boid1) - boid_positions.get(boid2)
			var distance = diff.length()
			if distance > 0.001: 
				rule1 += (diff.normalized() / distance)
			
			rule2 = rule2 + boid_velocity.get(boid2)
			
			rule3 = rule3 + boid_positions.get(boid2)
		
		#collision detection
		var start_of_ray = boid_positions.get(boid1)
		var end_of_ray = boid_positions.get(boid1) + boid_velocity.get(boid1) * ray_length
		
		var query = PhysicsRayQueryParameters3D.create(start_of_ray, end_of_ray)
		var collision = space_state.intersect_ray(query)
		
		if !collision.is_empty():
			var wall_normal = collision.normal
			var hit_point = collision.position
			var obstical_distance = boid_positions.get(boid1).distance_to(hit_point)
			var panic_multiplier = 1 / max(obstical_distance, 0.1) #stronger the closer we are to a wall
			rule4 = (wall_normal +boid_velocity.get(boid1).normalized().bounce(wall_normal))  * panic_multiplier
		
		if number_of_boids_near != 0:#average that shit crazy style
			rule2 = rule2 / number_of_boids_near
			rule3 = rule3 / number_of_boids_near
			
			rule2 = rule2 - boid_velocity.get(boid1)
			rule3 = rule3 - boid_positions.get(boid1)
		
		#apply all forces/ rules and turn that shit
		var steering_force = (rule1 * rule1_strength) + (rule2 * rule2_strength) + (rule3 * rule3_strength) + (rule4 * rule4_strength)
		var current_vel = boid_velocity.get(boid1)
		
		var target_vel = (current_vel + steering_force).normalized() * boid_speed
		var new_boid_velocity = current_vel.lerp(target_vel, delta * boid_turn_speed) 
		boid_velocity.set(boid1, new_boid_velocity)
		
		var new_pos = boid_positions.get(boid1) + boid_velocity.get(boid1) * delta
		new_pos.x = wrapf(new_pos.x, -boundary_size, boundary_size)
		new_pos.y = wrapf(new_pos.y, -boundary_size, boundary_size)
		new_pos.z = wrapf(new_pos.z, -boundary_size, boundary_size)
		
		boid_positions.set(boid1, new_pos)
		
		var look_at_vel = Vector3(boid_velocity.get(boid1))
		
		
		#update mesh
		var look_target =  boid_positions.get(boid1) + boid_velocity.get(boid1)
		boid_mesh[boid1].position = boid_positions.get(boid1)
		boid_mesh[boid1].look_at(look_target,Vector3(0,1,0))
		boid_mesh[boid1].rotate_object_local(Vector3.FORWARD, -PI / 2 )
		boid_mesh[boid1].rotate_object_local(Vector3.RIGHT, PI / 2)
		boid_mesh[boid1].rotate_object_local(Vector3.FORWARD, .5)
		#boid_mesh[boid1].rotate(Vector3(0,1,0), PI/2)
	pass
