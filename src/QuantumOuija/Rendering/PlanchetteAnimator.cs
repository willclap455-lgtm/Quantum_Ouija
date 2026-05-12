using Microsoft.Xna.Framework;

namespace QuantumOuija.Rendering;

public sealed class PlanchetteAnimator
{
    private IReadOnlyList<Vector2> _waypoints = Array.Empty<Vector2>();
    private int _targetIndex;
    private float _elapsed;

    public PlanchetteAnimator(Vector2 initialPosition, float speedPixelsPerSecond, bool wobble)
    {
        CurrentPosition = initialPosition;
        SpeedPixelsPerSecond = speedPixelsPerSecond;
        Wobble = wobble;
    }

    public Vector2 CurrentPosition { get; private set; }
    public bool IsRunning { get; private set; }
    public bool CompletedThisFrame { get; private set; }
    public float SpeedPixelsPerSecond { get; set; }
    public bool Wobble { get; set; }

    public Vector2 RenderPosition
    {
        get
        {
            if (!Wobble)
            {
                return CurrentPosition;
            }

            var wobble = new Vector2(MathF.Sin(_elapsed * 3.1f), MathF.Cos(_elapsed * 2.3f)) * 1.8f;
            return CurrentPosition + wobble;
        }
    }

    public void StartPath(IReadOnlyList<Vector2> waypoints)
    {
        if (waypoints.Count == 0)
        {
            return;
        }

        _waypoints = waypoints;
        CurrentPosition = waypoints[0];
        _targetIndex = Math.Min(1, waypoints.Count);
        IsRunning = waypoints.Count > 1;
        CompletedThisFrame = waypoints.Count <= 1;
    }

    public void Update(GameTime gameTime)
    {
        CompletedThisFrame = false;
        _elapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (!IsRunning)
        {
            return;
        }

        var remainingDistance = SpeedPixelsPerSecond * (float)gameTime.ElapsedGameTime.TotalSeconds;

        while (remainingDistance > 0 && _targetIndex < _waypoints.Count)
        {
            var target = _waypoints[_targetIndex];
            var delta = target - CurrentPosition;
            var distance = delta.Length();

            if (distance <= 0.001f)
            {
                CurrentPosition = target;
                _targetIndex++;
                continue;
            }

            if (distance <= remainingDistance)
            {
                CurrentPosition = target;
                remainingDistance -= distance;
                _targetIndex++;
                continue;
            }

            CurrentPosition += Vector2.Normalize(delta) * remainingDistance;
            remainingDistance = 0;
        }

        if (_targetIndex >= _waypoints.Count)
        {
            IsRunning = false;
            CompletedThisFrame = true;
        }
    }
}
