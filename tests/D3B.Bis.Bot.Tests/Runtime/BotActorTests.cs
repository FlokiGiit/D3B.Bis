using D3B.Bis.Bot.Banks;
using D3B.Bis.Bot.Characters;
using D3B.Bis.Bot.Runtime;
using D3B.Bis.Engine.World;
using D3B.Bis.Network.Relay;
using D3B.Bis.Protocol.Game;
using Google.Protobuf;
using Microsoft.Extensions.Logging.Abstractions;

namespace D3B.Bis.Bot.Tests.Runtime;

public sealed class BotActorTests : IAsyncDisposable
{
    // Measured on the code that posted its stop, then completed its inbox: an ask fell between the two at 40 to 58 stops
    // of 100 on an idle machine (20 runs) and 39 to 56 beside 24 busy loops (10 runs), so a hundred stops miss the race in
    // fewer than one run in 10^20.
    private const int Stops = 100;

    // Each wait asked and not ended is tried again at every ask after it: few are enough to meet the stop.
    private const int AsksAStop = 64;

    private static readonly Dictionary<string, IMessage> _byItself = new()
    {
        ["confirmation"] = new MapMovementConfirmRequest(),
        ["map information"] = new MapInformationRequest { MapId = TestPlay.MapId },
        ["leaving a dialog"] = new DialogLeaveRequest(),
    };

    private readonly TestPlay _play = new();

    public static TheoryData<string> WhatTheClientSendsByItself()
    {
        return [.. _byItself.Keys];
    }

    public ValueTask DisposeAsync()
    {
        return _play.DisposeAsync();
    }

    [Fact]
    public async Task TheBotKeepsTheIdOfItsLastGameConnectionAfterItEnds()
    {
        var (connection, bot) = await _play.EnterAsync(300);

        Assert.Equal(connection.Session.Id, bot.ConnectionId);

        connection.Close();
        await bot.ProcessedAsync();

        Assert.False(bot.IsConnected);
        Assert.Equal(connection.Session.Id, bot.ConnectionId);
    }

    [Fact]
    public async Task AnExpectationGetsTheMessageOnceTheStateHoldsIt()
    {
        var (connection, bot) = await _play.EnterAsync(300);
        var kamas = bot.RunAsync(
            async (context, token) =>
            {
                var update = context.Expect((side, message, state) => message is KamasUpdateEvent && state.Kamas == 42);

                await context.SendAsync(new MapChangeRequest { MapId = 1 }, token);

                return (await update.WaitAsync(TimeSpan.FromSeconds(30), token) as KamasUpdateEvent)?.Kamas;
            },
            TestContext.Current.CancellationToken);

        await connection.SentAsync(1);
        connection.Server(new KamasUpdateEvent { Kamas = 41 });
        connection.Server(new KamasUpdateEvent { Kamas = 42 });

        Assert.Equal(42, await kamas);
    }

    [Fact]
    public async Task AnExpectationEndsAfterItsDelayByTheBotsClock()
    {
        var (_, bot) = await _play.EnterAsync(300);
        var wait = new KamasWait();
        var kamas = bot.RunAsync(wait.RunAsync, TestContext.Current.CancellationToken);

        await wait.ExpectingAsync(bot);
        _play.Time.Advance(TimeSpan.FromSeconds(29));
        await Task.Delay(20, TestContext.Current.CancellationToken);

        Assert.False(kamas.IsCompleted);

        _play.Time.Advance(TimeSpan.FromSeconds(1));

        Assert.Null(await kamas);
    }

    [Fact]
    public async Task OneBehaviorRunsAtATime()
    {
        var (connection, bot) = await _play.EnterAsync(300);
        var wait = new KamasWait();
        var first = bot.RunAsync(wait.RunAsync, TestContext.Current.CancellationToken);

        await wait.ExpectingAsync(bot);

        await Assert.ThrowsAsync<InvalidOperationException>(() => bot.RunAsync(new KamasWait().RunAsync, TestContext.Current.CancellationToken));

        connection.Server(new KamasUpdateEvent { Kamas = 7 });

        Assert.Equal(7, await first);
        await bot.ProcessedAsync();
        Assert.False(bot.IsActing);
    }

    [Fact]
    public async Task ABehaviorRunsOnlyWhileAGameConnectionPlaysTheCharacter()
    {
        var (connection, bot) = await _play.EnterAsync(300);

        connection.Close();
        await bot.ProcessedAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => bot.RunAsync(new KamasWait().RunAsync, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ThePlayerActingInterruptsTheBehaviorAndPausesTheBotUntilResumed()
    {
        var (connection, bot) = await _play.EnterAsync(300);
        var wait = new KamasWait();
        var behavior = bot.RunAsync(wait.RunAsync, TestContext.Current.CancellationToken);

        await wait.ExpectingAsync(bot);
        connection.Client(new InteractiveUseRequest { ElementId = 1, SkillInstanceId = 2 });

        var interrupted = await Assert.ThrowsAsync<BotInterruptedException>(() => behavior);

        Assert.Equal(Interruption.Player, interrupted.Reason);
        Assert.True(bot.IsPaused);
        await Assert.ThrowsAsync<InvalidOperationException>(() => bot.RunAsync(new KamasWait().RunAsync, TestContext.Current.CancellationToken));

        bot.Resume();
        await bot.ProcessedAsync();

        Assert.False(bot.IsPaused);

        var again = new KamasWait();
        var resumed = bot.RunAsync(again.RunAsync, TestContext.Current.CancellationToken);

        await again.ExpectingAsync(bot);
        connection.Server(new KamasUpdateEvent { Kamas = 9 });

        Assert.Equal(9, await resumed);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ThePlayerTeleportingOrOpeningTheHavenBagInterruptsTheBot(bool teleporting)
    {
        var (connection, bot) = await _play.EnterAsync(300);
        var wait = new KamasWait();
        var behavior = bot.RunAsync(wait.RunAsync, TestContext.Current.CancellationToken);

        await wait.ExpectingAsync(bot);
        connection.Client(teleporting
            ? new TeleportRequest { MapId = TestPlay.EastMapId, Source = TeleporterType.Map }
            : new HavenBagEnterRequest { OwnerId = TestPlay.CharacterId });

        Assert.Equal(Interruption.Player, (await Assert.ThrowsAsync<BotInterruptedException>(() => behavior)).Reason);
        Assert.True(bot.IsPaused);
    }

    [Theory]
    [MemberData(nameof(WhatTheClientSendsByItself))]
    public async Task WhatTheClientSendsByItselfDoesNotInterruptTheBot(string request)
    {
        var (connection, bot) = await _play.EnterAsync(300);
        var wait = new KamasWait();
        var behavior = bot.RunAsync(wait.RunAsync, TestContext.Current.CancellationToken);

        await wait.ExpectingAsync(bot);
        connection.Client(_byItself[request]);
        connection.Server(new KamasUpdateEvent { Kamas = 3 });

        Assert.Equal(3, await behavior);
        Assert.False(bot.IsPaused);
    }

    [Fact]
    public async Task ThePlayerLeavingTheBankInterruptsTheBot()
    {
        var (connection, bot) = await _play.EnterAsync(396, TestPlay.InsideMapId);

        connection.Server(new ExchangeStartedWithStorageEvent { Type = ExchangeType.Storage });

        var wait = new KamasWait();
        var behavior = bot.RunAsync(wait.RunAsync, TestContext.Current.CancellationToken);

        await wait.ExpectingAsync(bot);
        connection.Client(new DialogLeaveRequest());

        Assert.Equal(Interruption.Player, (await Assert.ThrowsAsync<BotInterruptedException>(() => behavior)).Reason);
        Assert.True(bot.IsPaused);
    }

    [Fact]
    public async Task TheClientLeavingByItselfOnceTheServerEndedTheBankDoesNotInterruptTheBot()
    {
        var (connection, bot) = await _play.EnterAsync(396, TestPlay.InsideMapId);

        connection.Server(new ExchangeStartedWithStorageEvent { Type = ExchangeType.Storage });

        var wait = new KamasWait();
        var behavior = bot.RunAsync(wait.RunAsync, TestContext.Current.CancellationToken);

        await wait.ExpectingAsync(bot);

        // The relay shows the server's end before the client gets it, so the bot's loop sees it before the client's requests.
        connection.Server(new ExchangeLeaveEvent { Type = DialogType.Storage });
        connection.Client(new DialogLeaveRequest());
        connection.Client(new DialogLeaveRequest());
        connection.Server(new KamasUpdateEvent { Kamas = 4 });

        Assert.Equal(4, await behavior);
        Assert.False(bot.IsPaused);
    }

    [Fact]
    public async Task ThePlayerActingWhileTheBotIdlesDoesNotPauseIt()
    {
        var (connection, bot) = await _play.EnterAsync(300);

        connection.Client(new MapMovementRequest { MapId = TestPlay.MapId, KeyMovements = { 300, 301 } });
        await bot.ProcessedAsync();

        Assert.False(bot.IsPaused);
    }

    [Fact]
    public async Task TheConnectionClosingInterruptsTheBehaviorWithoutPausingTheBot()
    {
        var (connection, bot) = await _play.EnterAsync(300);
        var wait = new KamasWait();
        var behavior = bot.RunAsync(wait.RunAsync, TestContext.Current.CancellationToken);

        await wait.ExpectingAsync(bot);
        connection.Close();

        var interrupted = await Assert.ThrowsAsync<BotInterruptedException>(() => behavior);

        Assert.Equal(Interruption.Disconnected, interrupted.Reason);
        Assert.False(bot.IsPaused);
    }

    [Fact]
    public async Task AFightBeginningInterruptsTheBehaviorWithoutPausingTheBot()
    {
        var (connection, bot) = await _play.EnterAsync(300);
        var wait = new KamasWait();
        var behavior = bot.RunAsync(wait.RunAsync, TestContext.Current.CancellationToken);

        await wait.ExpectingAsync(bot);
        connection.Server(new ContextDestroyEvent());
        connection.Server(new ContextCreationEvent { Context = ContextCreationEvent.Types.GameContext.Fight });

        Assert.Equal(Interruption.Fight, (await Assert.ThrowsAsync<BotInterruptedException>(() => behavior)).Reason);
        Assert.False(bot.IsPaused);
        Assert.True(bot.State.IsFighting);
    }

    [Fact]
    public async Task AWaitEndsOnceItsConditionHoldsAndAtOnceWhenItHoldsAlready()
    {
        var (connection, bot) = await _play.EnterAsync(300);

        await bot.WaitUntilAsync(b => b.State.Cell is 300, TestContext.Current.CancellationToken);

        var waiting = bot.WaitUntilAsync(b => b.State.Kamas == 5, TestContext.Current.CancellationToken);

        await bot.ProcessedAsync();
        connection.Server(new KamasUpdateEvent { Kamas = 4 });
        await bot.ProcessedAsync();

        Assert.False(waiting.IsCompleted);

        connection.Server(new KamasUpdateEvent { Kamas = 5 });

        await waiting;
    }

    [Fact]
    public async Task AWaitEndsWithItsTokenOrTheBot()
    {
        var (_, bot) = await _play.EnterAsync(300);

        using var stop = new CancellationTokenSource();

        var cancelled = bot.WaitUntilAsync(_ => false, stop.Token);
        var stopped = bot.WaitUntilAsync(_ => false, TestContext.Current.CancellationToken);

        await stop.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelled);

        await bot.DisposeAsync();

        // An activity's run takes a wait the stop ends for a stop by this name, the one ObjectDisposedException.ThrowIf gives.
        Assert.Equal(typeof(BotActor).FullName, (await Assert.ThrowsAsync<ObjectDisposedException>(() => stopped)).ObjectName);
        Assert.Equal(typeof(BotActor).FullName, (await Assert.ThrowsAsync<ObjectDisposedException>(() => bot.WaitUntilAsync(_ => true, TestContext.Current.CancellationToken))).ObjectName);
    }

    [Fact]
    public async Task StoppingTheBotInterruptsItsBehavior()
    {
        var (_, bot) = await _play.EnterAsync(300);
        var wait = new KamasWait();
        var behavior = bot.RunAsync(wait.RunAsync, TestContext.Current.CancellationToken);

        await wait.ExpectingAsync(bot);
        await bot.DisposeAsync();

        var interrupted = await Assert.ThrowsAsync<BotInterruptedException>(() => behavior);

        Assert.Equal(Interruption.Stopped, interrupted.Reason);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => bot.RunAsync(new KamasWait().RunAsync, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AWaitAskedWhileTheBotStopsEndsWithIt()
    {
        var waits = await AskedWhileStoppingAsync(bot => bot.WaitUntilAsync(_ => false, TestContext.Current.CancellationToken));

        // The stop is over: a wait still pending is one the bot took and nothing will end.
        Assert.DoesNotContain(waits, wait => !wait.IsCompleted);
        Assert.All(waits, wait => Assert.IsType<ObjectDisposedException>(wait.Exception?.InnerException));
    }

    [Fact]
    public async Task AStartAskedWhileTheBotStopsEndsWithIt()
    {
        var starts = await AskedWhileStoppingAsync(bot => bot.RunWhenReadyAsync((_, _) => Task.FromResult(true), TestContext.Current.CancellationToken));

        // A start the stop ended is told on another thread: one the bot took and nothing ends would wait for ever.
        var ended = Task.WhenAll(starts.Select(async start => await Record.ExceptionAsync(() => start)));
        var failures = await ended.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);

        Assert.All(failures, failure => Assert.True(failure is ObjectDisposedException or BotInterruptedException { Reason: Interruption.Stopped }));
    }

    [Fact]
    public async Task TheCallerStoppingTheBehaviorIsNoInterruption()
    {
        var (_, bot) = await _play.EnterAsync(300);
        var wait = new KamasWait();

        using var stop = new CancellationTokenSource();

        var behavior = bot.RunAsync(wait.RunAsync, stop.Token);

        await wait.ExpectingAsync(bot);
        await stop.CancelAsync();

        var cancelled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => behavior);

        Assert.IsNotType<BotInterruptedException>(cancelled);
        Assert.False(bot.IsPaused);
    }

    /// <summary>Stops bots while a thread asks of each, and gives everything asked.</summary>
    /// <remarks>
    /// Nothing but a second thread can ask between two statements of a stop, so stop and asks are raced, bot after bot:
    /// whichever comes first, what was asked must end. The asking thread asks from the stop's signal until it is refused.
    /// </remarks>
    private async Task<List<Task>> AskedWhileStoppingAsync(Func<BotActor, Task> ask)
    {
        var asked = new List<Task>();

        for (var stop = 0; stop < Stops; stop++)
        {
            var bot = Idle();
            var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var stopping = false;
            var asking = Task.Run(
                () =>
                {
                    ready.SetResult();

                    // Spinning, not waiting: its first ask must come as the stop goes, within the time a thread takes to wake.
                    while (!Volatile.Read(ref stopping))
                        Thread.SpinWait(1);

                    for (var asks = 0; asks < AsksAStop; asks++)
                    {
                        var task = ask(bot);

                        asked.Add(task);

                        if (task.IsFaulted)
                            break;
                    }
                },
                TestContext.Current.CancellationToken);

            await ready.Task.WaitAsync(TestContext.Current.CancellationToken);
            Volatile.Write(ref stopping, true);
            await bot.DisposeAsync();
            await asking;
        }

        return asked;
    }

    /// <summary>A bot no game connection plays, made as the play's bots make theirs and stopped by its test.</summary>
    private BotActor Idle()
    {
        return new BotActor(
            CharacterState.Chosen(TestPlay.Key, TestPlay.Chosen().Success.Character),
            _play.Maps,
            new Lazy<WorldPathfinder>(() => new WorldPathfinder(_play.World.Graph, _play.World.Atlas)),
            new Lazy<BankDirectory>(() => new BankDirectory(_play.Tables.Banks, _play.World.Graph, _play.World.Atlas)),
            () => BotDelays.Measured,
            _play.Time,
            NullLogger<BotActor>.Instance);
    }

    /// <summary>A behavior that waits for a kamas update from the server, and gives the kamas it holds.</summary>
    private sealed class KamasWait
    {
        private readonly TaskCompletionSource _expecting = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<long?> RunAsync(BotContext context, CancellationToken cancellationToken)
        {
            var update = context.Expect((side, message, _) => side is RelayDirection.FromServer && message is KamasUpdateEvent);

            // The delay starts when the wait does, before the test moves the clock.
            var waiting = update.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);

            _expecting.TrySetResult();

            return (await waiting as KamasUpdateEvent)?.Kamas;
        }

        /// <summary>Waits until the bot's loop has the behavior's expectation: a frame shown after it reaches it.</summary>
        public async Task ExpectingAsync(BotActor bot)
        {
            await _expecting.Task;
            await bot.ProcessedAsync();
        }
    }
}
