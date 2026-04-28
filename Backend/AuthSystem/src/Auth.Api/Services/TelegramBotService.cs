using Auth.Api.Options;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace Auth.Api.Services;

public class TelegramBotService
{
    private readonly HttpClient _httpClient;
    private readonly TelegramOptions _telegramOptions;
    private readonly ILogger<TelegramBotService> _logger;

    public TelegramBotService(
        HttpClient httpClient,
        IOptions<TelegramOptions> telegramOptions,
        ILogger<TelegramBotService> logger)
    {
        _httpClient = httpClient;
        _telegramOptions = telegramOptions.Value;
        _logger = logger;
    }

    public async Task<bool> SendMessageAsync(long chatId, string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_telegramOptions.BotToken))
        {
            _logger.LogWarning("Telegram BotToken topilmadi.");
            return false;
        }

        var url = $"https://api.telegram.org/bot{_telegramOptions.BotToken}/sendMessage";

        var payload = new
        {
            chat_id = chatId,
            text
        };

        var response = await _httpClient.PostAsJsonAsync(url, payload, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<IReadOnlyList<TelegramUpdate>> GetUpdatesAsync(long offset, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_telegramOptions.BotToken))
            return Array.Empty<TelegramUpdate>();

        var url = $"https://api.telegram.org/bot{_telegramOptions.BotToken}/getUpdates?timeout=25&offset={offset}";

        using var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Telegram getUpdates xatosi. StatusCode: {StatusCode}", response.StatusCode);
            return Array.Empty<TelegramUpdate>();
        }

        var data = await response.Content.ReadFromJsonAsync<TelegramApiResponse<List<TelegramUpdate>>>(cancellationToken: cancellationToken);
        return data?.Result ?? new List<TelegramUpdate>();
    }
}