extends Node3D

const boid_res: PackedScene = preload("res://Models/blender/fish.blend")

####This shit be using the packed vector for performance
@export var boid_mesh_resource: Mesh
@export var boid_material_resource: Material

@export var num_boids:int = 200
@export var spawn_radius:float = 20
@export var boid_vision_radius_squared:float = 9
@export var boid_speed:float = 1.5
@export var boid_turn_speed:float = 1.5
@export var ray_length:float = 1.5
@export var run_from_player_squared:float = 9

##steer to avoid crowding local flockmates
@export_range(0.0, 6.0, 0.01) var rule1_strength: float = 1
##steer towards the average heading of local flockmates
@export_range(0.0, 6.0, 0.01) var rule2_strength: float = 1
##steer to move towards the average position (center of mass) of local flockmates
@export_range(0.0, 6.0, 0.01) var rule3_strength: float = 1
##obstical avoidance
@export_range(0.0, 6.0, 0.01) var rule4_strength: float = 1

#@export_range(0.0, 6.0, 0.01) var rule5_strength: float = 1
##Move away from player
@export_range(0.0, 6.0, 0.01) var rule6_strength: float = 1

@export var boundary_size: float = 150

##show raycasts
@export var show_debug_rays: bool = true
var debug_mesh_instance: MeshInstance3D
var debug_mesh: ImmediateMesh


var boid_positions := PackedVector3Array()
var boid_velocity := PackedVector3Array()


var boid_meshs: MultiMeshInstance3D = MultiMeshInstance3D.new();

var player_cam :Camera3D;

# Called when the node enters the scene tree for the first time.
func _ready():
	
	player_cam = get_viewport().get_camera_3d()
	
	var multi_mesh = MultiMesh.new()
	multi_mesh.transform_format = MultiMesh.TRANSFORM_3D
	multi_mesh.use_custom_data = true
	multi_mesh.instance_count = num_boids
	
	multi_mesh.mesh = boid_mesh_resource
	
	for i in range(num_boids):
		multi_mesh.set_instance_custom_data(i, Color(randf(), randf(), randf(), randf()))

	
	boid_meshs.multimesh = multi_mesh
	boid_meshs.material_override = boid_material_resource
	
	add_child(boid_meshs)
	
	
	#for debugging the raycast
	debug_mesh_instance = MeshInstance3D.new()
	debug_mesh = ImmediateMesh.new()
	debug_mesh_instance.mesh = debug_mesh
	debug_mesh_instance.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	
	var line_material = StandardMaterial3D.new()
	line_material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	line_material.vertex_color_use_as_albedo = true
	debug_mesh_instance.material_override = line_material
	
	add_child(debug_mesh_instance)
	
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
		#var boid:Node3D = boid_res.instantiate()
		
		boid_positions.append(boid_position)
		boid_velocity.append(random_vel)
		
		
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _physics_process(delta):
	
	#todo optimization step where this is called every random range of frames (actually unsure how possible this is)
	#todo octree optimizations
	#todo multi threading
	#todo interaction limiting
	
	#idk if it matters in godot but im gonna put these here so it avoids alocating and dealocating memory
	var number_of_boids_near: int = 0;
	var dist: float = 0;
	
	var space_state = get_world_3d().direct_space_state
	
	#debug
	debug_mesh.clear_surfaces()
	if show_debug_rays:
		debug_mesh.surface_begin(Mesh.PRIMITIVE_LINES)
	
	#iterate through all boids
	for boid1 in range(num_boids):
		#rules for boids
		var rule1: Vector3 = Vector3(0, 0, 0) #steer to avoid crowding local flockmates
		var rule2: Vector3 = Vector3(0, 0, 0) #steer towards the average heading of local flockmates
		var rule3: Vector3 = Vector3(0, 0, 0) #steer to move towards the average position (center of mass) of local flockmates
		var rule4: Vector3 = Vector3(0, 0, 0) #obstical avoidance
		#var rule5: Vector3 = Vector3(0, 0, 0) #goal seeking to be implemented
		var rule6: Vector3 = Vector3(0, 0, 0) #obstical avoidance
		
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
			var distance = diff.length_squared()
			if distance > 0.001: 
				rule1 += (diff / distance)
			
			rule2 = rule2 + boid_velocity.get(boid2)
			
			rule3 = rule3 + boid_positions.get(boid2)
		
		#collision detection
		var start_of_ray = boid_positions.get(boid1)
		var end_of_ray = boid_positions.get(boid1) + boid_velocity.get(boid1) * ray_length
		
		var query = PhysicsRayQueryParameters3D.create(start_of_ray, end_of_ray)
		var collision = space_state.intersect_ray(query)
		var obstacle_importance_modifier = boid_turn_speed
		var dynamic_turn_speed = boid_turn_speed
		if !collision.is_empty():
			var wall_normal = collision.normal
			var hit_point = collision.position
			var obstical_distance = boid_positions.get(boid1).distance_to(hit_point)
			var panic_multiplier = 3.0 / max(obstical_distance, 0.1) #stronger the closer we are to a wall
			rule4 = (wall_normal +boid_velocity.get(boid1).normalized().bounce(wall_normal))  * panic_multiplier
			obstacle_importance_modifier = boid_turn_speed + (panic_multiplier * 5.0)
		
		#debug
		if show_debug_rays:
			if collision.is_empty():
				debug_mesh.surface_set_color(Color.GREEN) # safe path
			else:
				debug_mesh.surface_set_color(Color.RED)   # hitting an obstacle
				
			debug_mesh.surface_add_vertex(start_of_ray)
			debug_mesh.surface_add_vertex(end_of_ray)
		
		if number_of_boids_near != 0:#average that shit crazy style
			rule2 = rule2 / number_of_boids_near
			rule3 = rule3 / number_of_boids_near
			
			rule2 = rule2 - boid_velocity.get(boid1)
			rule3 = rule3 - boid_positions.get(boid1)
		
		
		#rule 6
		var camera_dist = boid_positions.get(boid1).distance_squared_to(player_cam.global_position)
		if camera_dist < run_from_player_squared:
			
			rule6 = boid_positions.get(boid1) -  player_cam.global_position
		
		
		
		#apply all forces/ rules and turn that shit
		var steering_force = ((rule1 * rule1_strength) + (rule2 * rule2_strength) + (rule3 * rule3_strength) + (rule6 * rule6_strength))/obstacle_importance_modifier  + (rule4 * rule4_strength)
		var current_vel = boid_velocity.get(boid1)
		
		var target_vel = (current_vel + steering_force ).normalized() * boid_speed
		var new_boid_velocity = current_vel.lerp(target_vel, delta * boid_turn_speed) 
		boid_velocity.set(boid1, new_boid_velocity)
		
		#wrap around to the other side the boundy
		var new_pos = boid_positions.get(boid1) + boid_velocity.get(boid1) * delta
		new_pos.x = wrapf(new_pos.x, -boundary_size, boundary_size)
		new_pos.y = wrapf(new_pos.y, -10, 10)
		new_pos.z = wrapf(new_pos.z, -boundary_size, boundary_size)
		
		boid_positions.set(boid1, new_pos)
		
		
		update_boid_transform(boid1, boid_positions.get(boid1) , boid_velocity.get(boid1))
	
	#debug
	if show_debug_rays:
		debug_mesh.surface_end()
	pass



func update_boid_transform(index: int, pos: Vector3, vel: Vector3):
	var look_target =  pos + vel
	
	var trans := Transform3D()
	trans.origin = pos
	if not pos.is_equal_approx(look_target): #in case errors :)
			trans = trans.looking_at(look_target, Vector3.UP)
			
	trans = trans.rotated_local(Vector3.UP, PI / 2 )

	
	boid_meshs.multimesh.set_instance_transform(index, trans)
	pass
