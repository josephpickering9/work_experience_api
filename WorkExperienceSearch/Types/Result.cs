using Microsoft.AspNetCore.Mvc;

namespace Work_Experience_Search.Types;

public abstract class Result
{
    public bool Success { get; protected set; }
    public bool Failure => !Success;

    public ActionResult ToErrorResponse()
    {
        return Failure
            ? ((Failure)this).ToErrorResponse()
            : throw new Exception($"You can't access this when .{nameof(Failure)} is false");
    }
}

public abstract class Result<T> : Result
{
    private T _data;

    protected Result(T data)
    {
        Data = data;
        _data = data;
    }

    public T Data
    {
        get => Success
            ? _data
            : throw new Exception($"You can't access .{nameof(Data)} when .{nameof(Success)} is false");
        set => _data = value;
    }
}

public class Success : Result
{
    public Success()
    {
        Success = true;
    }
}

public class Success<T> : Result<T>
{
    public Success(T data) : base(data)
    {
        Success = true;
    }
}

public class Failure : Result, IFailure
{
    public Failure(string message, ErrorType errorType = ErrorType.BadRequest) : this(message, Array.Empty<Error>(),
        errorType)
    {
    }

    private Failure(string message, IReadOnlyCollection<Error> errors, ErrorType errorType = ErrorType.BadRequest)
    {
        Message = message;
        Success = false;
        Errors = errors ?? Array.Empty<Error>();
        ErrorType = errorType;
    }

    public string Message { get; set; }
    public IReadOnlyCollection<Error> Errors { get; }
    public ErrorType ErrorType { get; }

    public new ActionResult ToErrorResponse()
    {
        return ErrorType switch
        {
            ErrorType.NotFound => new NotFoundObjectResult(Message),
            ErrorType.Conflict => new ConflictObjectResult(Message),
            ErrorType.BadRequest => new BadRequestObjectResult(Message),
            ErrorType.Unauthorized => new UnauthorizedObjectResult(Message),
            ErrorType.Forbidden => new ForbidResult(Message),
            _ => new BadRequestObjectResult(Message)
        };
    }
}

public class Failure<T> : Result<T>, IFailure
{
    public Failure(string message, ErrorType errorType = ErrorType.BadRequest) : this(message, Array.Empty<Error>(),
        errorType)
    {
    }

    private Failure(string message, IReadOnlyCollection<Error> errors, ErrorType errorType = ErrorType.BadRequest) :
        base(default)
    {
        Message = message;
        Success = false;
        Errors = errors ?? Array.Empty<Error>();
        ErrorType = errorType;
    }

    public string Message { get; set; }
    public IReadOnlyCollection<Error> Errors { get; }
    public ErrorType ErrorType { get; }

    public ActionResult ToErrorResponse()
    {
        return ErrorType switch
        {
            ErrorType.NotFound => new NotFoundObjectResult(Message),
            ErrorType.Conflict => new ConflictObjectResult(Message),
            ErrorType.BadRequest => new BadRequestObjectResult(Message),
            ErrorType.Unauthorized => new UnauthorizedObjectResult(Message),
            ErrorType.Forbidden => new ForbidResult(Message),
            _ => new BadRequestObjectResult(Message)
        };
    }
}

public class NotFoundFailure<T>(string message = " not found.") : Failure<T>(message, ErrorType.NotFound);
public class ConflictFailure<T>(string message) : Failure<T>(message, ErrorType.Conflict);
public class BadRequestFailure<T>(string message) : Failure<T>(message, ErrorType.BadRequest);
public class UnauthorizedFailure<T>(string message) : Failure<T>(message, ErrorType.Unauthorized);
public class ForbiddenFailure<T>(string message) : Failure<T>(message, ErrorType.Forbidden);

internal interface IFailure
{
    string Message { get; }
    IReadOnlyCollection<Error> Errors { get; }
    ErrorType ErrorType { get; }
}

public class Error
{
    public Error(string details) : this(null, details)
    {
    }

    private Error(string code, string details)
    {
        Code = code;
        Details = details;
    }

    public string Code { get; }
    public string Details { get; }
}

public enum ErrorType
{
    None,
    NotFound,
    Conflict,
    BadRequest,
    Unauthorized,
    Forbidden,
}
