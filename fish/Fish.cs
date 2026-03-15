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
                GD.Print(data.Name + ": WANDER");
            }

            return true;
        }
    }

    class TargetBoid : BehaviorTree<Fish> {
        public override bool Execute(Fish data) {
            var boid_positions = data.boids.Get("boid_positions").AsVector3Array();
            var boid_count = data.boids.Get("num_boids").AsInt32();
            var boid_id = data.Rng.RandiRange(0, boid_count);
            if (boid_positions[boid_id].DistanceSquaredTo(data.GlobalPosition) < 900f) {
                data.TargetedBoid = boid_id;
                data.BoidIsTargeted = true;
                return true;
            }

            return false;
        }
    }

    class Fight : BehaviorTree<Fish> {
        public override bool Execute(Fish data) {
            var closestDistance2 = float.PositiveInfinity;
            Node3D closest = null;
            foreach (var fish in data.GetTree().GetNodesInGroup("bigfish")) {
                if (fish != data && fish is Node3D fish3d) {
                    if (fish3d.GlobalPosition.DistanceSquaredTo(data.GlobalPosition) < 400.0f) {
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
                GD.Print(data.Name + ": FIGHT");
                return true;
            }

            return false;
        }
    }

    class Bob : BehaviorTree<Fish> {
        public override bool Execute(Fish data) {
            if (data.GlobalPosition.Y > 8.8) {
                data.GravityScale = 1.0f;
                data.TargetDirection.Y = 0.0f;
                data.TargetedLocation.Y = 8.8f;
                return true;
            }

            data.GravityScale = 0.0f;
            return false;
        }
    }

    class CheckFocus : BehaviorTree<Fish> {
        public override bool Execute(Fish data) {
            if (data.FocusTimer <= 0.0f) {
                return false;
            }

            return true;
        }
    }

    class UpdateFocusTimer : BehaviorTree<Fish> {
        public override bool Execute(Fish data) {
            data.FocusTimer = Math.Max(0.0f, data.FocusTimer - (float)data.GetPhysicsProcessDeltaTime());
            // GD.Print(data.FocusTimer);
            return false;
        }
    }

    class Debug : BehaviorTree<Fish> {
        private readonly string _text;

        public Debug(String text) {
            _text = text;
        }

        public override bool Execute(Fish data) {
            GD.Print(data.Name + ": " + _text);
            return true;
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

            return count > 1;
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

    class ClearFocus : BehaviorTree<Fish> {
        public override bool Execute(Fish data) {
            if (data.BoidIsTargeted) {
                var boid_positions = data.boids.Get("boid_positions").AsVector3Array();
                var pos = boid_positions[data.TargetedBoid];
                if (data.GlobalPosition.DistanceSquaredTo(pos) > 25f && data.NumTimeTargetBoid < 10 && data.Rng.Randf() > 0.5) {
                    data.FocusTimer = 3.0f;
                    data.NumTimeTargetBoid++;
                    GD.Print(data.Name + ": RETARGET BOID");
                    return true; // Stop execution here
                }
            }

            data.NumTimeTargetBoid = 0;
            data.TargetedFish = null;
            data.TargetedFood = null;
            data.FocusTimer = 0f;
            data.LocationIsTargeted = false;
            data.BoidIsTargeted = false;
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
        private bool _persist = false;

        public Target(Func<Fish, Vector3> target) {
            _target = target;
        }

        public Target(Func<Fish, Vector3> target, bool persist) {
            _target = target;
            _persist = persist;
        }

        public override bool Execute(Fish data) {
            var target = _target(data);
            data.TargetDirection = data.GlobalPosition.DirectionTo(target);
            if (_persist) {
                data.TargetedLocation = target;
                data.LocationIsTargeted = true;
            }

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

    class EatFood : BehaviorTree<Fish> {
        public override bool Execute(Fish data) {
            var closestDistance2 = float.PositiveInfinity;
            Node3D closest = null;
            foreach (var food in data.GetTree().GetNodesInGroup("food")) {
                if (food != data && food is Node3D food3d) {
                    if (food3d.GlobalPosition.DistanceSquaredTo(data.GlobalPosition) < 225.0f) {
                        if (food3d.GlobalPosition.DistanceSquaredTo(data.GlobalPosition) < closestDistance2) {
                            closestDistance2 = food3d.GlobalPosition.DistanceSquaredTo(data.GlobalPosition);
                            closest = food3d;
                        }
                    }
                }
            }

            if (closest != null) {
                data.FocusTimer = 12.0f;
                data.TargetedFood = closest;
                GD.Print(data.Name + ": EAT FOOD");
                return true;
            }

            return false;
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
    [Export] public Node boids;

    public FishState State = FishState.Idle;

    public Node3D Laser;

    public float WanderUpdateCooldown = 0.0f;
    public float SnapModifier = 1.0f;

    public bool WasCalled = false;
    public Vector3 CallLocation = new Vector3();

    public float FocusTimer = 0.0f;

    public Node3D TargetedFish = null;
    public Node3D TargetedFood = null;
    public Vector3 TargetedLocation = Vector3.Zero;
    public bool LocationIsTargeted = false;

    public int TargetedBoid = 0;
    public bool BoidIsTargeted = false;
    public int NumTimeTargetBoid = 0;

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
            new Behavior<Fish>(_ => TargetedFish != null),
            new Behaviors.Modifiers {
                SpeedModifier = 2.0f,
                SnapModifier = 2.0f,
            },
            new Behaviors.Target(_ => TargetedFish.GlobalPosition)
        );

        var focusedEatFood = new Sequence<Fish>(
            new Behavior<Fish>(_ => TargetedFood != null && IsInstanceValid(TargetedFood)),
            new Behaviors.Modifiers {
                SpeedModifier = 3.0f,
                SnapModifier = 3.0f,
            },
            new Behaviors.Target(_ => TargetedFood.GlobalPosition)
        );

        var focusedTravel = new Sequence<Fish>(
            new Behavior<Fish>(_ => LocationIsTargeted),
            new Behaviors.Target(_ => TargetedLocation)
        );

        var focusedEat = new Sequence<Fish>(
            new Behavior<Fish>(_ => BoidIsTargeted),
            new Behaviors.Target(_ => {
                var boid_positions = boids.Get("boid_positions").AsVector3Array();
                return boid_positions[TargetedBoid];
            })
        );

        var gotoCall = new Sequence<Fish>(
            new Behavior<Fish>(_ => WasCalled),
            new Behavior<Fish>(_ => {
                WasCalled = false;
                return true;
            }),
            new Behaviors.Focus(20.0f),
            new Behaviors.Modifiers {
                SnapModifier = 3.0f,
                SpeedModifier = 2.0f
            },
            new Behaviors.Debug("GOTO CALL"),
            new Behaviors.Target(_ => CallLocation, true)
        );

        var eatBoid = new Sequence<Fish>(
            new Behaviors.Focus(5.0f),
            new Behaviors.Modifiers {
                SnapModifier = 3.0f,
                SpeedModifier = 5.0f
            },
            new Behaviors.TargetBoid(),
            new Behaviors.Debug("TARGET BOID")
        );

        var tether = new Sequence<Fish>(
            new Behavior<Fish>(_ => GlobalPosition.DistanceSquaredTo(Vector3.Zero) >= 2500f),
            new Behaviors.Focus(20.0f),
            new Behaviors.Modifiers {
                SpeedModifier = 1.5f,
                SnapModifier = 3.0f
            },
            new Behaviors.Debug("GOTO ZERO"),
            new Behaviors.Target(_ => Vector3.Zero)
        );

        var runFromDanger = new Sequence<Fish>(
            new Behaviors.IsOverwhelmed(),
            new Behaviors.Focus(15.0f),
            new Behaviors.Modifiers {
                SnapModifier = 6.0f,
                SpeedModifier = 3.0f
            },
            new Behaviors.Debug("RUN"),
            new Behaviors.MoveRandomly()
        );


        _behaviorTree = new Selector<Fish>(
            // These always run
            new Behaviors.Bob(),
            new Behaviors.UpdateFocusTimer(),

            // These run when focused (in order, may exit execution early)
            new Sequence<Fish>(
                isFocused,
                new Selector<Fish>(
                    focusedFight,
                    focusedEatFood,
                    focusedEat,
                    focusedTravel
                )
            ),

            // These prevent escaping focus states
            new Behaviors.CheckFocus(),
            new Behaviors.ClearFocus(),

            // These run when unfocused (in order, may exit execution early)
            gotoCall,
            tether,
            runFromDanger,
            new RandomSelector<Fish>(
                new Behaviors.Fight(),
                eatBoid,
                new Behaviors.EatFood()
            ),
            new Behaviors.Wander()
        );
    }

    public override void _Process(double delta) {
        base._Process(delta);
        if (Input.IsActionJustPressed("call_sharks")) {
            GD.Print(Name + ": CALL SHARKS");
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