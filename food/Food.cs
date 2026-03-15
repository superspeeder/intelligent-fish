using Godot;
using System;

public partial class Food : RigidBody3D {
	private float _foodValue = 100.0f;
	private float _visibleFoodValue = 100.0f;
	[Export] private MeshInstance3D Visuals;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	public override void _Process(double delta) {
		base._Process(delta);
		float err = _foodValue - _visibleFoodValue;
		_visibleFoodValue = _foodValue + Mathf.Sign(err) * Mathf.SmoothStep(_visibleFoodValue, _foodValue, (float)delta);
		
		if (_visibleFoodValue <= 0.0f) {
			QueueFree();
			return;
		}

		Visuals.SetScale(new Vector3(_visibleFoodValue / 100f, _visibleFoodValue / 100f, _visibleFoodValue / 100f));
	}

	private void OnBodyEntered(Node body) {
		if (body.IsInGroup("bigfish")) {
			_foodValue -= 20.0f;
			if (body is RigidBody3D rb) {
				rb.ApplyCentralImpulse((GlobalPosition - rb.GlobalPosition) * 1.5f);
			}
		}
	}
}
