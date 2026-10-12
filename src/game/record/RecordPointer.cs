using System;
using OpenTK.Mathematics;

namespace Cathedral.Game.Record;

/// <summary>
/// The drawn mouse of a recording. One gesture at a time: travel to a pixel along a gentle curve,
/// settle, press, hold, release, linger. Its position is a pure function of the footage clock, so the
/// recorder draws it smoothly at the video's frame rate even though the game is told about it only on
/// the CLI's 10 Hz tick (<see cref="RecordGate.Tick"/>).
/// </summary>
public sealed class RecordPointer
{
    private enum Phase { Idle, Moving, Settling, Pressed, Lingering }

    private Phase _phase = Phase.Idle;
    private Vector2 _from, _to, _ctrl;
    private double _t0, _moveDuration, _holdSeconds;
    private bool _press;
    private Action? _onPress, _onRelease, _onDone;

    /// <summary>Where the pointer rests between gestures.</summary>
    public Vector2 Position { get; private set; }

    public bool Visible { get; set; } = true;

    /// <summary>The last press, for the ripple drawn around it.</summary>
    public (double Time, Vector2 At)? LastPress { get; private set; }

    public bool Busy => _phase != Phase.Idle;

    /// <summary>Whether the button is down right now (the cursor is drawn pressed).</summary>
    public bool IsPressed(double now) => _phase == Phase.Pressed;

    public RecordPointer(Vector2 start) => Position = start;

    private const double SettleSeconds = 0.14;
    private const double TapSeconds = 0.09;
    private const double LingerSeconds = 0.22;

    /// <summary>
    /// Starts a gesture to <paramref name="to"/>. With <paramref name="press"/>, the button goes down
    /// after the pointer settles and comes up <paramref name="holdSeconds"/> later (a tap by default).
    /// The callbacks run on the game thread, from <see cref="Advance"/>.
    /// </summary>
    public void Start(Vector2 to, double now, bool press, double holdSeconds = TapSeconds,
                      Action? onPress = null, Action? onRelease = null, Action? onDone = null)
    {
        _from = Position;
        _to = to;
        _press = press;
        _holdSeconds = Math.Max(TapSeconds, holdSeconds);
        _onPress = onPress; _onRelease = onRelease; _onDone = onDone;

        float dist = (_to - _from).Length;
        _moveDuration = dist < 2 ? 0 : Math.Clamp(dist / RecordMode.CursorSpeed + 0.18, 0.28, 1.1);

        // A hand does not travel in a straight line: bow the path a little to one side, more for a
        // longer reach. Which side is fixed by the geometry, so a replayed script draws the same arc.
        var mid = (_from + _to) * 0.5f;
        var dir = _to - _from;
        var normal = dist > 0 ? new Vector2(-dir.Y, dir.X) / dist : Vector2.Zero;
        float side = (dir.X >= 0) == (dir.Y >= 0) ? 1f : -1f;
        _ctrl = mid + normal * side * Math.Min(dist * 0.12f, 60f);

        _t0 = now;
        _phase = _moveDuration > 0 ? Phase.Moving : Phase.Settling;
    }

    /// <summary>The pointer's position at <paramref name="now"/>, mid-gesture or at rest.</summary>
    public Vector2 PositionAt(double now)
    {
        if (_phase != Phase.Moving) return Position;
        double u = Math.Clamp((now - _t0) / _moveDuration, 0, 1);
        float e = (float)(u < 0.5 ? 4 * u * u * u : 1 - Math.Pow(-2 * u + 2, 3) / 2);   // ease in-out cubic
        float a = 1 - e;
        return a * a * _from + 2 * a * e * _ctrl + e * e * _to;
    }

    /// <summary>
    /// Moves the gesture on to <paramref name="now"/> and fires whatever press, release or completion
    /// fell due. Returns the position the game should be told about.
    /// </summary>
    public Vector2 Advance(double now)
    {
        switch (_phase)
        {
            case Phase.Moving:
                if (now - _t0 < _moveDuration) return PositionAt(now);
                Position = _to;
                _phase = Phase.Settling; _t0 = now;
                break;

            case Phase.Settling:
                Position = _to;
                if (now - _t0 < SettleSeconds) break;
                if (!_press) { Finish(); break; }
                _phase = Phase.Pressed; _t0 = now;
                LastPress = (now, _to);
                _onPress?.Invoke();
                break;

            case Phase.Pressed:
                if (now - _t0 < _holdSeconds) break;
                _onRelease?.Invoke();
                _phase = Phase.Lingering; _t0 = now;
                break;

            case Phase.Lingering:
                if (now - _t0 >= LingerSeconds) Finish();
                break;
        }
        return Position;
    }

    private void Finish()
    {
        _phase = Phase.Idle;
        var done = _onDone;
        _onPress = _onRelease = _onDone = null;
        done?.Invoke();
    }
}
