using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace QuantumOuija.Rendering;

public sealed class TrailRenderer
{
    private readonly List<TrailSample> _samples = new();
    private float _sampleAccumulator;

    public float SampleIntervalSeconds { get; init; } = 0.018f;
    public float LifetimeSeconds { get; init; } = 3.6f;
    public int MaxSamples { get; init; } = 1800;

    public void Clear() => _samples.Clear();

    public void Update(GameTime gameTime, Vector2 currentPosition, bool isMoving)
    {
        var elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

        for (var i = _samples.Count - 1; i >= 0; i--)
        {
            var sample = _samples[i] with { Age = _samples[i].Age + elapsed };
            if (sample.Age >= LifetimeSeconds)
            {
                _samples.RemoveAt(i);
            }
            else
            {
                _samples[i] = sample;
            }
        }

        if (!isMoving)
        {
            return;
        }

        _sampleAccumulator += elapsed;
        if (_sampleAccumulator >= SampleIntervalSeconds)
        {
            _sampleAccumulator = 0;
            _samples.Add(new TrailSample(currentPosition, 0));
            if (_samples.Count > MaxSamples)
            {
                _samples.RemoveRange(0, _samples.Count - MaxSamples);
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, BoardViewTransform transform)
    {
        for (var i = 1; i < _samples.Count; i++)
        {
            var previous = _samples[i - 1];
            var current = _samples[i];
            var alpha = 1f - MathHelper.Clamp(current.Age / LifetimeSeconds, 0, 1);
            var color = new Color(150, 230, 210) * (alpha * 0.58f);
            spriteBatch.DrawLine(
                pixel,
                transform.BoardToScreen(previous.Position),
                transform.BoardToScreen(current.Position),
                color,
                3f);
        }
    }

    private readonly record struct TrailSample(Vector2 Position, float Age);
}
