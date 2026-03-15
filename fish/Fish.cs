using Godot;
using System;
using System.Collections.Generic;
using IntelligentFish.behavior_tree;
using IntelligentFish.fish;

namespace Behaviors {
    class Wander : BehaviorTree<Fish> {
        public override bool Execute(Fish data) {
            data.WanderUpdateCooldown -= (float)data.GetPhysicsProcessDeltaTime();

            if (data.WanderUpdateCooldown <= 0) {
                data.SpeedModifier = 1.0f;
                data.SnapModifier = 1.0f;

                data.WanderUpdateCooldown = 5f;

                var turn = new Vector3(data.Rng.RandfRange(-1f, 1f), data.Rng.Randfn(0f, 0.3f),
                        data.Rng.RandfRange(-1f, 1f))
                    .Normalized();
                var basis = Basis.LookingAt(turn, Vector3.Up);
                data.TargetDirection = basis.Z;
            }

            return true;
        }
    }

    class Fight : BehaviorTree<Fish> {
        public override bool Execute(Fish data) {
            var closestDistance2 = float.PositiveInfinity;
            Node3D closest = null;
            foreach (var fish in data.GetTree().GetNodesInGroup("bigfish")) {
                if (fish != data && fish is Node3D fish3d) {
                    if (fish3d.GlobalPosition.DistanceSquaredTo(data.GlobalPosition) < 225.0f) {
                        if (fish3d.GlobalPosition.DistanceSquaredTo(data.GlobalPosition) < closestDistance2) {
                            closestDistance2 = fish3d.GlobalPosition.DistanceSquaredTo(data.GlobalPosition);
                            closest = fish3d;
                        }
                    }
                }
            }

            if (closest != null) {
                data.FocusTimer = 3.0f;
                data.TargetedFish = closest;
            }

            return false;
        }
    }

    class Bob : BehaviorTree<Fish> {
        public override bool Execute(Fish data) {
            if (data.GlobalPosition.Y > 8.8) {
                data.GravityScale = 1.0f;
                return true;
            }

            data.GravityScale = 0.0f;
            return false;
        }
    }

    class CheckFocus : BehaviorTree<Fish> {
        public override bool Execute(Fish data) {
            data.FocusTimer = Math.Max(0.0f, data.FocusTimer - (float)data.GetPhysicsProcessDeltaTime());

            if (data.FocusTimer <= 0.0f) {
                return false;
            }

            return true;
        }
    }

    class FocusedBehavior : BehaviorTree<Fish> {
        public float FocusTime = 0.0f;
        private BehaviorTree<Fish> child;

        public FocusedBehavior(float focusTime, BehaviorTree<Fish> child) {
            FocusTime = focusTime;
            this.child = child;
        }

        public override bool Execute(Fish data) {
            data.FocusTimer = FocusTime;
            return child.Execute(data);
        }
    }

    abstract class GotoLocation : BehaviorTree<Fish> {
        public float SpeedModifier = 1.0f;
        public float SnapModifier = 1.0f;

        public override bool Execute(Fish data) {
            data.SpeedModifier = SpeedModifier;
            data.SnapModifier = SnapModifier;
            data.TargetDirection = data.GlobalPosition.DirectionTo(Location(data));
            return true;
        }

        public abstract Vector3 Location(Fish data);
    }

    class GotoCall : GotoLocation {
        public override bool Execute(Fish data) {
            data.WasCalled = false;
            GD.Print("GOTO CALL");
            return base.Execute(data);
        }

        public override Vector3 Location(Fish data) {
            return data.CallLocation;
        }
    }

    class GotoCenter : GotoLocation {
        public override bool Execute(Fish data) {
            GD.Print("GOTO CENTER");
            return base.Execute(data);
        }

        public override Vector3 Location(Fish data) {
            return Vector3.Zero;
        }
    }

    class IsOverwhelmed : BehaviorTree<Fish> {
        public override bool Execute(Fish data) {
            int count = 0;
            foreach (var fish in data.GetTree().GetNodesInGroup("bigfish")) {
                if (fish != data && fish is Node3D fish3d) {
                    if (fish3d.GlobalPosition.DistanceSquaredTo(data.GlobalPosition) < 225.0f) {
                        count++;
                    }
                }
            }

            return count > 3;
        }
    }

    class RunAway : BehaviorTree<Fish> {
        public override bool Execute(Fish data) {
            data.SpeedModifier = 3.0f;
            data.SnapModifier = 6.0f;
            return true;
        }
    }

    class MoveRandomly : BehaviorTree<Fish> {
        public override bool Execute(Fish data) {
            var turn = new Vector3(data.Rng.RandfRange(-1f, 1f), data.Rng.Randfn(0f, 0.3f),
                    data.Rng.RandfRange(-1f, 1f))
                .Normalized();
            var basis = Basis.LookingAt(turn, Vector3.Up);
            data.TargetDirection = basis.Z;
            return true;
        }
    }

    class FocusedFight : BehaviorTree<Fish> {
        public override bool Execute(Fish data) {
            if (data.TargetedFish != null && data.FocusTimer > 0) {
                data.TargetDirection = data.GlobalPosition.DirectionTo(data.TargetedFish.GlobalPosition);
            }

            return false;
        }
    }

    class ClearFocus : BehaviorTree<Fish> {
        public override bool Execute(Fish data) {
            data.TargetedFish = null;
            data.FocusTimer = 0f;
            return false;
        }
    }

    class Modifiers : BehaviorTree<Fish> {
        public float SpeedModifier = 1.0f;
        public float SnapModifier = 1.0f;

        public override bool Execute(Fish data) {
            data.SpeedModifier = SpeedModifier;
            data.SnapModifier = SnapModifier;
            return true;
        }
    }

    class Target : BehaviorTree<Fish> {
        private Func<Fish, Vector3> _target;

        public Target(Func<Fish, Vector3> target) {
            _target = target;
        }

        public override bool Execute(Fish data) {
            data.TargetDirection = data.GlobalPosition.DirectionTo(_target(data));
            return true;
        }
    }

    class Focus : BehaviorTree<Fish> {
        private float _focus;

        public Focus(float focus) {
            _focus = focus;
        }

        public override bool Execute(Fish data) {
            data.FocusTimer = _focus;
            return true;
        }
    }
}

public partial class Fish : RigidBody3D {
    private BehaviorTree<Fish> _behaviorTree;
    public RandomNumberGenerator Rng;
    [Export] public Vector3 TargetDirection;
    [Export] public Quaternion errorQuat;
    public float SpeedModifier;

    [Export] public float TurningStrength = 5.0f;
    [Export] public float MovementStrength = 5.0f;
    [Export] public float MaxSpeed = 30.0f;
    [Export] public float TetherDistance = 50f;

    public FishState State = FishState.Idle;

    public Node3D Laser;

    public float WanderUpdateCooldown = 0.0f;
    public float SnapModifier = 1.0f;

    public bool WasCalled = false;
    public Vector3 CallLocation = new Vector3();

    public float FocusTimer = 0.0f;

    public Node3D TargetedFish = null;

    private bool clearFocus(Fish fish) {
        TargetedFish = null;
        FocusTimer = 0.0f;
        return false;
    }

    public override void _Ready() {
        base._Ready();
        Laser = GetNode<Node3D>("Laser");
        Rng = new RandomNumberGenerator();
        Rng.Seed = (ulong)Random.Shared.NextInt64();

        var isFocused = new Behavior<Fish>(_ => FocusTimer > 0f);
        var focusedFight = new Sequence<Fish>(
            isFocused,
            new Behaviors.Modifiers {
                SpeedModifier = 2.0f,
                SnapModifier = 2.0f,
            },
            new Behavior<Fish>(_ => TargetedFish != null),
            new Behaviors.Target(_ => TargetedFish.GlobalPosition)
        );

        var gotoCall = new Sequence<Fish>(
            new Behavior<Fish>(_ => WasCalled),
            new Sequence<Fish>(
                new Behaviors.Focus(30.0f),
                new Behaviors.Modifiers {
                    SnapModifier = 3.0f,
                    SpeedModifier = 2.0f
                },
                new Behaviors.Target(_ => CallLocation))
        );

        var tether = new Sequence<Fish>(
            new Behavior<Fish>(_ => GlobalPosition.DistanceSquaredTo(Vector3.Zero) >= 625f),
            new Behaviors.Focus(30.0f),
            new Behaviors.Modifiers {
                SpeedModifier = 1.5f,
                SnapModifier = 3.0f
            },
            new Behaviors.Target(_ => Vector3.Zero)
        );

        var runFromDanger = new Sequence<Fish>(
            new Behaviors.IsOverwhelmed(),
            new Behaviors.Focus(15.0f),
            new Behaviors.Modifiers {
                SnapModifier = 6.0f,
                SpeedModifier = 3.0f
            },
            new Behaviors.MoveRandomly()
        );

        _behaviorTree = new Selector<Fish>(
            new Behaviors.Bob(),
            focusedFight,
            new Behaviors.CheckFocus(),
            new Behaviors.ClearFocus(),
            gotoCall,
            tether,
            runFromDanger,
            new Behaviors.Fight(),
            new Behaviors.Wander()
        );
    }

    public override void _Process(double delta) {
        base._Process(delta);
        if (Input.IsActionJustPressed("call_sharks")) {
            GD.Print("call_sharks");
            WasCalled = true;
            CallLocation = GetTree().Root.GetCamera3D().GlobalPosition;
        }
    }

    public override void _PhysicsProcess(double delta) {
        base._PhysicsProcess(delta);
        _behaviorTree.Execute(this);

        LinearVelocity.LimitLength(MaxSpeed);

        if (TargetDirection != Vector3.Zero) {
            var basis = Basis.LookingAt(-TargetDirection, Vector3.Up);
            var quat = new Quaternion(basis);

            var error = Quaternion - quat;
            errorQuat = error;
            if (error.Length() > 0.001f) {
                Quaternion = Quaternion.Slerp(quat, (float)delta / 5.0f * SnapModifier);
            }


            ApplyCentralForce(Basis.Z * MovementStrength * (float)delta);
        }
    }
}