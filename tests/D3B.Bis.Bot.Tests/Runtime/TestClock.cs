using Microsoft.Extensions.Time.Testing;

namespace D3B.Bis.Bot.Tests.Runtime;

/// <summary>A clock a test moves, which knows the waits asked of it.</summary>
/// <remarks>
/// Moving the clock ends only the waits already asked: one asked a moment later, by a bot that runs beside the test, waits
/// for a move that never comes. What a test sees of a bot, a status, a note or a frame sent, comes before the bot asks its
/// next wait, so a test that means to end a wait first waits for it here (<see cref="AskedAsync"/>).
/// </remarks>
internal sealed class TestClock : FakeTimeProvider
{
    private readonly Lock _lock = new();
    private readonly List<Wait> _waits = [];

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var wait = new Wait(this, callback);

        wait.Timer = base.CreateTimer(wait.Ring, state, dueTime, period);

        // Known once the clock holds it, not before: a test that saw it sooner would move the clock under a timer still
        // being made, which would then count its delay from the moved clock.
        wait.EndsAfter(dueTime);

        lock (_lock)
            _waits.Add(wait);

        return wait;
    }

    /// <summary>Waits until a wait that ends a delay from now is asked of the clock: moving the clock then ends it.</summary>
    /// <remarks>
    /// A wait is told by when it ends alone: two that end at the same instant, as a display time and a time between two
    /// steps of 500 ms asked together, are one to the test.
    /// </remarks>
    public async Task AskedAsync(TimeSpan delay)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);

        while (!IsAsked(delay))
        {
            Assert.True(DateTime.UtcNow < deadline, $"No wait of {delay} was asked of the clock in time.");
            await Task.Delay(5, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>Moves the clock by a wait's delay, once that wait is asked of it.</summary>
    public async Task EndAsync(TimeSpan delay)
    {
        await AskedAsync(delay);
        Advance(delay);
    }

    private bool IsAsked(TimeSpan delay)
    {
        var end = GetUtcNow() + delay;

        lock (_lock)
            return _waits.Exists(wait => wait.End == end);
    }

    private void Forget(Wait wait)
    {
        lock (_lock)
            _waits.Remove(wait);
    }

    /// <summary>A timer of the clock's, and when it next rings if nothing changes it.</summary>
    private sealed class Wait : ITimer
    {
        private readonly TestClock _clock;
        private readonly TimerCallback _callback;

        // A wait of no time rings while the clock makes it, before its end is set: it has none.
        private bool _rang;

        public DateTimeOffset? End { get; private set; }

        public ITimer? Timer { get; set; }

        public Wait(TestClock clock, TimerCallback callback)
        {
            _clock = clock;
            _callback = callback;
        }

        public void EndsAfter(TimeSpan dueTime)
        {
            // Read before the lock is taken: reading the clock rings the timers due, and what waited on them runs there.
            var now = _clock.GetUtcNow();

            lock (_clock._lock)
                End = _rang || dueTime == Timeout.InfiniteTimeSpan ? null : now + dueTime;
        }

        // A timer that repeats is known until it first rings: the tests end waits asked once.
        public void Ring(object? state)
        {
            lock (_clock._lock)
            {
                _rang = true;
                End = null;
            }

            _callback(state);
        }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            lock (_clock._lock)
                _rang = false;

            var changed = Timer!.Change(dueTime, period);

            EndsAfter(dueTime);

            return changed;
        }

        public void Dispose()
        {
            _clock.Forget(this);
            Timer?.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            _clock.Forget(this);

            if (Timer is not null)
                await Timer.DisposeAsync();
        }
    }
}
