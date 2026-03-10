extends Node3D

const boid_res: PackedScene = preload("res://scenes/Boid.tscn")
@export var num_boids:int = 20
@export var spawn_radius:float = 20

signal spawned_fish

# Called when the node enters the scene tree for the first time.
func _ready():
	for i in range(num_boids):
		var boid:Node = boid_res.instantiate()
		#spawn location stuff
		var random_dir = Vector3(
			randf_range(-1.0, 1.0), 
			randf_range(-1.0, 1.0), 
			randf_range(-1.0, 1.0)
		).normalized()
		var spawn_vector = Vector3(1, 0, 0)
		#def a better way to do this but idk
		boid.position = random_dir * (randf() * spawn_radius)
		spawned_fish.emit()
		add_child(boid)
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(_delta):
	pass
