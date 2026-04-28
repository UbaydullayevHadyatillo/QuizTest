using Auth.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Auth.Api.Services;

public class TelegramUpdateProcessor
{
    private readonly AppDbContext _dbContext;
    private readonly TelegramBotService _telegramBotService;
    private readonly ILogger<TelegramUpdateProcessor> _logger;

    public TelegramUpdateProcessor(
        AppDbContext dbContext,
        TelegramBotService telegramBotService,
        ILogger<TelegramUpdateProcessor> logger)
    {
        _dbContext = dbContext;
        _telegramBotService = telegramBotService;
        _logger = logger;
    }

    public async Task ProcessAsync(TelegramUpdate update, CancellationToken cancellationToken = default)
    {
        var text = update.Message?.Text?.Trim();
        var chatId = update.Message?.Chat?.Id;

        if (string.IsNullOrWhiteSpace(text) || chatId is null)
            return;

        if (text.Equals("/start", StringComparison.OrdinalIgnoreCase))
        {
            await _telegramBotService.SendMessageAsync(
                chatId.Value,
                "Salom. Saytdan olingan link kodni yuborish uchun quyidagicha yozing:\n/link ABCD1234",
                cancellationToken);

            return;
        }

        if (text.StartsWith("/link ", StringComparison.OrdinalIgnoreCase))
        {
            var code = text["/link ".Length..].Trim().ToUpperInvariant();
            await LinkUserAsync(chatId.Value, code, cancellationToken);
            return;
        }

        await _telegramBotService.SendMessageAsync(
            chatId.Value,
            "Noma'lum buyruq. Foydalanish: /link ABCD1234",
            cancellationToken);
    }

    private async Task LinkUserAsync(long chatId, string code, CancellationToken cancellationToken)
    {
        var linkRequest = await _dbContext.TelegramLinkRequests
            .Include(x => x.User)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(
                x => x.LinkCode == code && !x.IsUsed,
                cancellationToken);

        if (linkRequest is null)
        {
            await _telegramBotService.SendMessageAsync(chatId, "Link kodi topilmadi yoki allaqachon ishlatilgan.", cancellationToken);
            return;
        }

        if (linkRequest.ExpiresAt <= DateTime.UtcNow)
        {
            await _telegramBotService.SendMessageAsync(chatId, "Link kodi eskirgan. Saytdan qayta oling.", cancellationToken);
            return;
        }

        linkRequest.IsUsed = true;
        linkRequest.UsedAt = DateTime.UtcNow;

        linkRequest.User.TelegramChatId = chatId;
        linkRequest.User.IsTelegramVerified = true;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} Telegram bilan bog'landi. ChatId: {ChatId}", linkRequest.UserId, chatId);

        await _telegramBotService.SendMessageAsync(
            chatId,
            $"Muvaffaqiyatli bog'landi.\nUsername: {linkRequest.User.UserName}\nEndi saytga kirishda Telegram kod ishlatiladi.",
            cancellationToken);
    }
}