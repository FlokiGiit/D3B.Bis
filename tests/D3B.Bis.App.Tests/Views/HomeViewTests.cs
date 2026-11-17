using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using D3B.Bis.App.Hosting;
using D3B.Bis.App.Localization;
using D3B.Bis.App.Tests.Updates;
using D3B.Bis.App.Updates;
using D3B.Bis.App.ViewModels;
using D3B.Bis.App.Views;
using D3B.Bis.Bot.Hosting;
using D3B.Bis.Bot.Tests.Runtime;
using D3B.Bis.Studio.Assets;

namespace D3B.Bis.App.Tests.Views;

public sealed class HomeViewTests : IAsyncDisposable
{
    private readonly TestStore _store = new();

    private static async Task<(List<string> Texts, List<(string Label, bool Enabled)> Buttons, bool IsDanger)> ClientFilesBannerAsync(FakePreparationScript script, bool update, string drawing)
    {
        var root = Path.Combine(Path.GetTempPath(), "D3B.Bis.App.Tests", Guid.NewGuid().ToString("N"));
        var runner = new HostRunner(_ => Task.FromResult(new BotHostStart(null, "No pack for client 3.7.14.18.", "3.7.14.18", root, @"C:\Ankama\Dofus-dofus3")));
        var files = new ClientFiles(log => new FakePreparation(log, script));

        runner.Start();
        await Waiting.UntilAsync(() => runner.Status is HostStatus.Failed, TimeSpan.FromSeconds(10));

        if (update)
        {
            await files.PrepareAsync("3.7.14.18", @"C:\Ankama\Dofus-dofus3", root);
            Directory.Delete(root, true);
        }

        return await OnTheUIThreadAsync(() =>
        {
            var localizer = Localizer.Instance;

            localizer.Use(Localizer.French);

            var main = new MainViewModel(runner, localizer, null, null, null, files);
            var window = new MainWindow { DataContext = main, Width = 1440, Height = 900 };

            main.Refresh();
            window.Show();
            Dispatcher.UIThread.RunJobs();

            var home = window.GetVisualDescendants().OfType<HomeView>().Single(view => view.IsEffectivelyVisible);
            var texts = Texts(home);
            var buttons = home.GetVisualDescendants().OfType<Button>().Where(button => button.IsEffectivelyVisible && button.Content is string).Select(button => ((string)button.Content!, button.IsEffectivelyEnabled)).ToList();
            var danger = home.GetVisualDescendants().OfType<Border>().Any(border => border.IsEffectivelyVisible && border.Classes.Contains("Banner") && border.Classes.Contains("Danger"));

            Drawings.Draw(window, drawing);
            localizer.Use(Localizer.English);
            window.Close();

            return (texts, buttons, danger);
        });
    }

    private static List<string> Texts(Control root)
    {
        return [.. root.GetVisualDescendants().OfType<TextBlock>().Where(block => block.IsEffectivelyVisible).Select(block => block.Text).OfType<string>().Where(text => text.Length > 0)];
    }

    private static Task<T> OnTheUIThreadAsync<T>(Func<T> action)
    {
        return HeadlessUnitTestSession.GetOrStartForAssembly(typeof(HomeViewTests).Assembly).Dispatch(action, TestContext.Current.CancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return _store.DisposeAsync();
    }

    [Fact]
    public async Task AClientWhoseFilesAreMissingShowsItsVersionAndTheUpdateButtonInsteadOfTheFailure()
    {
        var result = await ClientFilesBannerAsync(new FakePreparationScript(), false, "home-client-files");

        Assert.Contains("Le client Dofus est passé en 3.7.14.18", result.Texts);
        Assert.Contains(("Mettre à jour", true), result.Buttons);
        Assert.DoesNotContain("Redémarrer", result.Buttons.Select(button => button.Label));
        Assert.DoesNotContain(result.Texts, text => text.Contains("No pack for client", StringComparison.Ordinal));
        Assert.False(result.IsDanger);
    }

    [Fact]
    public async Task AProtocolThatChangedShowsTheBannerAsADangerWithNoButton()
    {
        var result = await ClientFilesBannerAsync(new FakePreparationScript { MappingHolds = false }, true, "home-client-protocol-changed");

        Assert.Contains("Le protocole du jeu a changé", result.Texts);
        Assert.DoesNotContain(result.Buttons, button => button.Label is "Mettre à jour" or "Réessayer" or "Redémarrer");
        Assert.True(result.IsDanger);
    }

    [Fact]
    public async Task TheStudioOpensOnTheHomePageInBothLanguagesAndTheWorldAppearsOnceARouteIsSelected()
    {
        await _store.OpenedAsync();
        await _store.Assets.SaveAsync(new RouteAsset { Id = "north", Name = "North", Anchors = [new RouteAnchor { MapId = 1 }, new RouteAnchor { MapId = 2 }] }, TestContext.Current.CancellationToken);
        await _store.SeeAsync(TestPlay.Account, (TestPlay.CharacterId, "Aerafa", 200));

        var runner = await TestHosts.RunningAsync(_store.Host);
        var result = await OnTheUIThreadAsync(() =>
        {
            var localizer = Localizer.Instance;

            localizer.Use(Localizer.English);

            var main = new MainViewModel(runner, localizer);
            var window = new MainWindow { DataContext = main, Width = 1440, Height = 900 };

            main.Routes.Use(TestRouteWorld.Services(_store.Assets));
            main.Refresh();
            window.Show();
            Dispatcher.UIThread.RunJobs();

            var home = window.GetVisualDescendants().OfType<HomeView>().Single(view => view.IsEffectivelyVisible);
            var english = Texts(home);

            Drawings.Draw(window, "home-en");

            var worldAtStart = window.GetVisualDescendants().OfType<WorldView>().Any(view => view.IsEffectivelyVisible);

            main.Settings.UseFrenchCommand.Execute(null);
            Dispatcher.UIThread.RunJobs();

            var french = Texts(home);

            Drawings.Draw(window, "home-fr");
            localizer.Use(Localizer.English);
            main.Refresh();
            main.Outliner.Select(main.Outliner.Rows.Single(row => row.Key == "route:north"));
            Dispatcher.UIThread.RunJobs();

            var shown = (window.GetVisualDescendants().OfType<WorldView>().Any(view => view.IsEffectivelyVisible), home.IsEffectivelyVisible);

            window.Close();

            return (english, french, worldAtStart, shown);
        });

        Assert.Contains("D3B.Bis Studio", result.english);
        Assert.Contains("Quick actions", result.english);
        Assert.Contains("New route", result.english);
        Assert.Contains("Aerafa", result.english);
        Assert.Contains("North", result.english);
        Assert.DoesNotContain(result.english, text => text.StartsWith('[') && text.EndsWith(']'));
        Assert.Contains("Actions rapides", result.french);
        Assert.Contains("Nouvelle route", result.french);
        Assert.Contains("Récents", result.french);
        Assert.DoesNotContain(result.french, text => text.StartsWith('[') && text.EndsWith(']'));
        Assert.False(result.worldAtStart);
        Assert.Equal((true, false), result.shown);
    }
}
