namespace EasyTouch.Core.Models;

public abstract class Request
{
    public string Action { get; set; } = string.Empty;
}

public abstract class Response
{
    public bool Success { get; set; }
    public string? Error { get; set; }
}

public class SuccessResponse : Response
{
    public SuccessResponse()
    {
        Success = true;
    }
}

public class SuccessResponse<T> : Response
{
    public T? Data { get; set; }
    
    public SuccessResponse(T data)
    {
        Success = true;
        Data = data;
    }
    
    public SuccessResponse()
    {
        Success = true;
    }
}

public class ErrorResponse : Response
{
    public ErrorResponse(string error)
    {
        Success = false;
        Error = error;
    }
}
