using D3B.Bis.Bot.Hosting;
using D3B.Bis.Storage;

return await BotHost.RunAsync(BotHost.CreateBuilder(args).AddModule<StorageModule>(), CancellationToken.None);
