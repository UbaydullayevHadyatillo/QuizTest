namespace Auth.Api.Common;

public record ServiceResult(bool Succeeded, string Message);

public record ServiceResult<T>(bool Succeeded, string Message, T? Data = default);