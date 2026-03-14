using Godot;
using System;
using System.Collections.Generic;
using IntelligentFish.behavior_tree;

public partial class Fish : RigidBody3D {
    private BehaviorTree<double> _behaviorTree;
    private RandomNumberGenerator _rng;
    [Export] private Vector3 targetDirection;
    [Export] private Quaternion errorQuat;
    private float speedModifier;

    [Export] public float TurningStrength = 5.0f;
    [Export] public float MovementStrength = 5.0f;

    private Node3D _laser;

    private float wanderUpdateCooldown = 0.0f;

    private bool wander(double delta) {
        wanderUpdateCooldown -= (float)delta;

        if (wanderUpdateCooldown <= 0) {
            wanderUpdateCooldown = 5f;
            var turn = new Vector3(_rng.RandfRange(-1f, 1f), _rng.Randfn(0f, 0.3f), _rng.RandfRange(-1f, 1f)).Normalized();
            var basis = Basis.LookingAt(turn, Vector3.Up);
            targetDirection = basis.Z;
        }

        return true;
    }

    private bool bob(double delta) {
        if (GlobalPosition.Y > 8.8) {
            GravityScale = 1.0f;
            return true;
        }

        GravityScale = 0.0f;
        return false;
    }

    public override void _Ready() {
        base._Ready();
        _laser = GetNode<Node3D>("Laser");
        _rng = new RandomNumberGenerator();
        _rng.Seed = (ulong)Random.Shared.NextInt64();
        _behaviorTree = new Selector<double>(new List<BehaviorTree<double>> {
            new Behavior<double>(bob),
            new Behavior<double>(wander),
        });
    }

    public override void _Process(double delta) {
        base._Process(delta);
    }

    public override void _PhysicsProcess(double delta) {
        base._PhysicsProcess(delta);
        speedModifier = 1.0f;

        _behaviorTree.Execute(delta);

        var basis = Basis.LookingAt(targetDirection, Vector3.Up);
        var quat = new Quaternion(basis);

        var error = Quaternion - quat;
        errorQuat = error;
        if (error.Length() > 0.001f) {
            Quaternion = Quaternion.Slerp(quat, (float)delta / 5.0f);
        }


        ApplyCentralForce(Basis.Z * MovementStrength * (float)delta);
    }
}