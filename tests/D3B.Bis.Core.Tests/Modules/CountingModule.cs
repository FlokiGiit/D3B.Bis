using D3B.Bis.Core.Modules;

namespace D3B.Bis.Core.Tests.Modules;

internal sealed class CountingModule : IModule
{
    public int Builds { get; private set; }

    public void Build(BotBuilder bot)
    {
        Builds++;
    }
}
