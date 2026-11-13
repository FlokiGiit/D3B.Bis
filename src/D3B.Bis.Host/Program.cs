using D3B.Bis.Bot.Hosting;
using D3B.Bis.Storage;

// Before the host reads its database: what D3B kept on this machine comes across at the first start.
PreviousInstall.Import(PreviousInstall.PreviousRoot, PreviousInstall.Root);

return await BotHost.RunAsync(BotHost.CreateBuilder(args).AddModule<StorageModule>(), CancellationToken.None);
