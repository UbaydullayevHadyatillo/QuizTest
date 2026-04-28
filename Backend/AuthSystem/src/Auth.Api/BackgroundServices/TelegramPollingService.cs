using Auth.Api.Services;

namespace Auth.Api.BackgroundServices;

public class TelegramPollingService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<TelegramPollingService> _logger;

    public TelegramPollingService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<TelegramPollingService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        long offset = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();

                var telegramBotService = scope.ServiceProvider.GetRequiredService<TelegramBotService>();
                var processor = scope.ServiceProvider.GetRequiredService<TelegramUpdateProcessor>();

                var updates = await telegramBotService.GetUpdatesAsync(offset, stoppingToken);

                foreach (var update in updates.OrderBy(x => x.UpdateId))
                {
                    await processor.ProcessAsync(update, stoppingToken);
                    offset = update.UpdateId + 1;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Telegram polling service xatolikka uchradi.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}