namespace ScalyTails.Web.Models;

// Wraps an API response so ViewModels can tell a real empty result from a network/auth failure.
public class ApiResult<T>
{
    public T?     Data    { get; init; }
    public bool   Success { get; init; }
    public string Error   { get; init; } = "";

    public static ApiResult<T> Ok(T data)        => new() { Data = data, Success = true };
    public static ApiResult<T> Fail(string error) => new() { Success = false, Error = error };
}
