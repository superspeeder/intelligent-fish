extends Node3D

@onready var detection_area = $detection_area


const SPEED = 5.0
var boids_rad: float
var velocity

var parent:Node3D

var nearby_boids: Array[Node3D] = []

func _ready():
	parent = get_parent_node_3d()
	
	velocity = Vector3(1,0,0)
	

func _physics_process(delta):
	
	var rule1: Vector3 = Vector3(0, 0, 0) #steer to avoid crowding local flockmates
	var rule2: Vector3 = Vector3(0, 0, 0) #steer towards the average heading of local flockmates
	var rule3: Vector3 = Vector3(0, 0, 0) #steer to move towards the average position (center of mass) of local flockmates
	var rule4: Vector3 = Vector3(0, 0, 0) #obstical avoidance
	#var rule5: Vector3 = Vector3(0, 0, 0) #goal seeking to be implemented
	
	var number_of_boids_near: int = 0;
	#var nearby_boids = detection_area.get_overlapping_bodies()
	for child in nearby_boids:
		if child == self: #ignore ourselves
			continue
		
		var dist:Vector3 = child.position - self.position
		
		
		rule1 = rule1 - dist
		
		rule2 = rule2 + child.position
		
		rule3 = rule3 + child.velocity
		
		
	#self.velocity
	global_position += velocity * delta




func _on_detection_area_area_entered(area):
	if area.name == "boid_center":
		var boid_node = area.get_parent()
		if boid_node != self and not nearby_boids.has(boid_node): 
			nearby_boids.append(boid_node)
	pass # Replace with function body.


func _on_detection_area_area_exited(area):
	if area.name == "boid_center":
		var boid_node = area.get_parent()
		if boid_node != self: 
			nearby_boids.erase(boid_node)
	pass # Replace with function body.
