using System.Collections.Generic;
using Godot;

public partial class StreetViewController : Node3D
{
    public enum MovementMode
    {
        StreetView = 0,
        FreeFly = 1
    }

    [Export] public MovementMode StartMode = MovementMode.StreetView;
    [Export] public Key ToggleModeKey = Key.Tab;

    [Export] public NodePath PlayerRigPath = "PlayerRig";
    [Export] public NodePath CameraPath = "PlayerRig/Camera3D";
    [Export] public NodePath NavigationPointsPath = "NavigationPoints";
    [Export] public NodePath ArrowContainerPath = "Arrows";

    [Export] public float MouseSensitivity = 0.0025f;
    [Export] public float MaxPitchDegrees = 75.0f;

    [ExportCategory("StreetView")]
    [Export] public float EyeHeightMeters = 1.65f;
    [Export] public float MoveDurationSeconds = 0.35f;
    [Export] public float MaxLinkDistanceMeters = 5.5f;
    [Export] public int MaxNeighborCount = 3;
    [Export] public float ArrowHeightMeters = 0.03f;

    [ExportCategory("FreeFly")]
    [Export] public float FreeFlySpeedMetersPerSecond = 8.0f;
    [Export] public float FreeFlyVerticalSpeedMetersPerSecond = 5.5f;

    private const string TargetIndexMeta = "target_index";

    private Node3D _playerRig = null!;
    private Camera3D _camera = null!;
    private Node3D _navigationPointsRoot = null!;
    private Node3D _arrowRoot = null!;
    private StandardMaterial3D _arrowMaterial = null!;

    private readonly List<Marker3D> _points = new();

    private MovementMode _currentMode;
    private int _currentIndex;
    private bool _isMoving;
    private float _yaw;
    private float _pitch;

    public override void _Ready()
    {
        _playerRig = GetNode<Node3D>(PlayerRigPath);
        _camera = GetNode<Camera3D>(CameraPath);
        _navigationPointsRoot = GetNode<Node3D>(NavigationPointsPath);
        _arrowRoot = GetNode<Node3D>(ArrowContainerPath);

        BuildArrowMaterial();
        CollectNavigationPoints();

        _yaw = _playerRig.Rotation.Y;
        _pitch = _camera.Rotation.X;
        ApplyLookRotation();

        SetMovementMode(StartMode, force: true);
    }

    public override void _ExitTree()
    {
        if (Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_currentMode != MovementMode.FreeFly)
        {
            return;
        }

        Basis basis = _playerRig.GlobalTransform.Basis;
        Vector3 moveDirection = Vector3.Zero;

        if (Input.IsPhysicalKeyPressed(Key.W))
        {
            moveDirection += -basis.Z;
        }
        if (Input.IsPhysicalKeyPressed(Key.S))
        {
            moveDirection += basis.Z;
        }
        if (Input.IsPhysicalKeyPressed(Key.A))
        {
            moveDirection += -basis.X;
        }
        if (Input.IsPhysicalKeyPressed(Key.D))
        {
            moveDirection += basis.X;
        }

        moveDirection.Y = 0.0f;
        if (moveDirection.LengthSquared() > 0.0001f)
        {
            moveDirection = moveDirection.Normalized();
        }

        Vector3 velocity = moveDirection * FreeFlySpeedMetersPerSecond;

        if (Input.IsPhysicalKeyPressed(Key.Space))
        {
            velocity += Vector3.Up * FreeFlyVerticalSpeedMetersPerSecond;
        }
        if (Input.IsPhysicalKeyPressed(Key.Shift))
        {
            velocity += Vector3.Down * FreeFlyVerticalSpeedMetersPerSecond;
        }

        _playerRig.GlobalPosition += velocity * (float)delta;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent &&
            keyEvent.Pressed &&
            !keyEvent.Echo &&
            keyEvent.Keycode == ToggleModeKey)
        {
            ToggleMovementMode();
            return;
        }

        if (@event is InputEventKey escapeEvent &&
            escapeEvent.Pressed &&
            escapeEvent.Keycode == Key.Escape &&
            Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
            return;
        }

        if (@event is InputEventMouseButton mouseButton)
        {
            HandleMouseButton(mouseButton);
            return;
        }

        if (@event is InputEventMouseMotion mouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            _yaw -= mouseMotion.Relative.X * MouseSensitivity;
            float maxPitch = Mathf.DegToRad(MaxPitchDegrees);
            _pitch = Mathf.Clamp(_pitch - mouseMotion.Relative.Y * MouseSensitivity, -maxPitch, maxPitch);
            ApplyLookRotation();
        }
    }

    private void ToggleMovementMode()
    {
        SetMovementMode(_currentMode == MovementMode.StreetView ? MovementMode.FreeFly : MovementMode.StreetView);
    }

    private void SetMovementMode(MovementMode mode, bool force = false)
    {
        if (!force && _currentMode == mode)
        {
            return;
        }

        _currentMode = mode;

        if (_currentMode == MovementMode.StreetView)
        {
            EnterStreetViewMode();
        }
        else
        {
            EnterFreeFlyMode();
        }

        GD.Print($"Movement mode: {_currentMode}. Toggle key: {ToggleModeKey}.");
    }

    private void EnterStreetViewMode()
    {
        if (_points.Count < 2)
        {
            GD.PushWarning("StreetView mode needs at least 2 navigation points. Falling back to FreeFly.");
            _currentMode = MovementMode.FreeFly;
            EnterFreeFlyMode();
            return;
        }

        _isMoving = false;
        _currentIndex = FindNearestPointIndex(_playerRig.GlobalPosition - Vector3.Up * EyeHeightMeters);
        SnapToPoint(_currentIndex);
        RefreshArrows();

        if (Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
    }

    private void EnterFreeFlyMode()
    {
        _isMoving = false;
        ClearArrows();
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    private void HandleMouseButton(InputEventMouseButton mouseButton)
    {
        if (!mouseButton.Pressed)
        {
            return;
        }

        if (mouseButton.ButtonIndex == MouseButton.Right)
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
            return;
        }

        if (mouseButton.ButtonIndex != MouseButton.Left || _currentMode != MovementMode.StreetView)
        {
            return;
        }

        Vector2 clickPosition = mouseButton.Position;
        if (Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            clickPosition = GetViewport().GetVisibleRect().Size * 0.5f;
        }

        TryClickArrow(clickPosition);
    }

    private void TryClickArrow(Vector2 screenPosition)
    {
        if (_isMoving || _currentMode != MovementMode.StreetView)
        {
            return;
        }

        Vector3 rayOrigin = _camera.ProjectRayOrigin(screenPosition);
        Vector3 rayEnd = rayOrigin + _camera.ProjectRayNormal(screenPosition) * 200.0f;

        PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(rayOrigin, rayEnd);
        query.CollideWithAreas = true;
        query.CollideWithBodies = false;

        var hit = GetWorld3D().DirectSpaceState.IntersectRay(query);
        if (hit.Count == 0)
        {
            return;
        }

        GodotObject colliderObject = hit["collider"].AsGodotObject();
        if (colliderObject is not Area3D area || !area.HasMeta(TargetIndexMeta))
        {
            return;
        }

        int targetIndex = (int)(long)area.GetMeta(TargetIndexMeta);
        MoveToPoint(targetIndex);
    }

    private void MoveToPoint(int targetIndex)
    {
        if (_currentMode != MovementMode.StreetView ||
            _isMoving ||
            targetIndex == _currentIndex ||
            targetIndex < 0 ||
            targetIndex >= _points.Count)
        {
            return;
        }

        _isMoving = true;
        ClearArrows();

        Vector3 direction = _points[targetIndex].GlobalPosition - _points[_currentIndex].GlobalPosition;
        direction.Y = 0.0f;
        if (direction.LengthSquared() > 0.0001f)
        {
            _yaw = Mathf.Atan2(-direction.X, -direction.Z);
            ApplyLookRotation();
        }

        Tween tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Sine);
        tween.SetEase(Tween.EaseType.InOut);
        tween.TweenProperty(_playerRig, "global_position", ToEyePosition(_points[targetIndex].GlobalPosition), MoveDurationSeconds);
        tween.Finished += () =>
        {
            _currentIndex = targetIndex;
            _isMoving = false;
            RefreshArrows();
        };
    }

    private void RefreshArrows()
    {
        ClearArrows();
        if (_currentMode != MovementMode.StreetView)
        {
            return;
        }

        List<int> neighbors = GetNearestNeighbors(_currentIndex);
        foreach (int targetIndex in neighbors)
        {
            SpawnArrow(targetIndex);
        }
    }

    private List<int> GetNearestNeighbors(int sourceIndex)
    {
        Vector3 source = _points[sourceIndex].GlobalPosition;
        var candidates = new List<(int Index, float Distance)>();

        for (int i = 0; i < _points.Count; i++)
        {
            if (i == sourceIndex)
            {
                continue;
            }

            float distance = source.DistanceTo(_points[i].GlobalPosition);
            if (distance <= MaxLinkDistanceMeters)
            {
                candidates.Add((i, distance));
            }
        }

        if (candidates.Count == 0)
        {
            for (int i = 0; i < _points.Count; i++)
            {
                if (i == sourceIndex)
                {
                    continue;
                }

                candidates.Add((i, source.DistanceTo(_points[i].GlobalPosition)));
            }
        }

        candidates.Sort((a, b) => a.Distance.CompareTo(b.Distance));
        int count = Mathf.Min(MaxNeighborCount, candidates.Count);
        var result = new List<int>(count);
        for (int i = 0; i < count; i++)
        {
            result.Add(candidates[i].Index);
        }

        return result;
    }

    private void SpawnArrow(int targetIndex)
    {
        Vector3 from = _points[_currentIndex].GlobalPosition;
        Vector3 to = _points[targetIndex].GlobalPosition;
        Vector3 direction = to - from;
        direction.Y = 0.0f;

        float distance = direction.Length();
        if (distance < 0.05f)
        {
            return;
        }

        Vector3 forward = direction / distance;
        Vector3 arrowPosition = from + forward * Mathf.Clamp(distance * 0.45f, 1.0f, 2.6f);
        arrowPosition.Y = ArrowHeightMeters;

        Area3D area = new Area3D();
        area.Name = $"ArrowTo{targetIndex:00}";
        area.InputRayPickable = true;
        area.SetMeta(TargetIndexMeta, targetIndex);
        _arrowRoot.AddChild(area);
        area.GlobalPosition = arrowPosition;
        area.LookAt(arrowPosition + forward, Vector3.Up);

        CollisionShape3D collision = new CollisionShape3D();
        collision.Shape = new CylinderShape3D
        {
            Radius = 0.45f,
            Height = 0.2f
        };
        collision.Position = new Vector3(0.0f, 0.1f, 0.0f);
        area.AddChild(collision);

        MeshInstance3D shaft = new MeshInstance3D();
        shaft.Mesh = new BoxMesh { Size = new Vector3(0.22f, 0.03f, 0.75f) };
        shaft.Position = new Vector3(0.0f, 0.03f, -0.28f);
        shaft.MaterialOverride = _arrowMaterial;
        area.AddChild(shaft);

        MeshInstance3D head = new MeshInstance3D();
        head.Mesh = new BoxMesh { Size = new Vector3(0.42f, 0.03f, 0.42f) };
        head.Position = new Vector3(0.0f, 0.03f, -0.72f);
        head.Rotation = new Vector3(0.0f, Mathf.DegToRad(45.0f), 0.0f);
        head.MaterialOverride = _arrowMaterial;
        area.AddChild(head);
    }

    private int FindNearestPointIndex(Vector3 position)
    {
        int bestIndex = 0;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < _points.Count; i++)
        {
            float distance = position.DistanceSquaredTo(_points[i].GlobalPosition);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private void CollectNavigationPoints()
    {
        _points.Clear();
        foreach (Node child in _navigationPointsRoot.GetChildren())
        {
            if (child is Marker3D marker)
            {
                _points.Add(marker);
            }
        }

        _points.Sort((a, b) => string.CompareOrdinal(a.Name.ToString(), b.Name.ToString()));
    }

    private void BuildArrowMaterial()
    {
        _arrowMaterial = new StandardMaterial3D();
        _arrowMaterial.AlbedoColor = new Color(0.121568f, 0.682353f, 0.972549f, 1.0f);
        _arrowMaterial.EmissionEnabled = true;
        _arrowMaterial.Emission = new Color(0.070588f, 0.439216f, 0.811765f, 1.0f);
        _arrowMaterial.Roughness = 0.25f;
    }

    private void SnapToPoint(int index)
    {
        _playerRig.GlobalPosition = ToEyePosition(_points[index].GlobalPosition);
    }

    private Vector3 ToEyePosition(Vector3 basePosition)
    {
        return basePosition + Vector3.Up * EyeHeightMeters;
    }

    private void ApplyLookRotation()
    {
        _playerRig.Rotation = new Vector3(0.0f, _yaw, 0.0f);
        _camera.Rotation = new Vector3(_pitch, 0.0f, 0.0f);
    }

    private void ClearArrows()
    {
        foreach (Node child in _arrowRoot.GetChildren())
        {
            child.QueueFree();
        }
    }
}
