using D3B.Bis.Core.Modules;

namespace D3B.Bis.Core.Tests.Modules;

internal sealed class RequiringModule : IModule
{
    public void Build(BotBuilder bot)
    {
        bot.AddModule<RequiredModule>();
    }
}
