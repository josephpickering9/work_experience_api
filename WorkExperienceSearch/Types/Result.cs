using Microsoft.AspNetCore.Mvc;
using Work_Experience_Search.Exceptions;

namespace Work_Experience_Search.Types;

public class Result<T>
{
    protected Result(T data)
    {
        Data = data;
    }

    protected Result(Exception error, ErrorType type)
    {
        Error = error;
        ErrorType = type;
    }

    public T? Data { get; }
    public Exception? Error { get; }
    private ErrorType ErrorType { get; }
    public bool IsSuccess => Error == null;

    public ActionResult ToResponse()
    {
        return IsSuccess ? ToSuccessResponse() : ToErrorResponse();
    }

    private ActionResult ToSuccessResponse()
    {
        return Data != null ? new OkObjectResult(Data) : new NoContentResult();
    }

    private ActionResult ToErrorResponse()
    {
        if (Error == null)
        {
            return new ObjectResult(new ProblemDetails { Status = 400, Title = "Bad Request" }) { StatusCode = 400 };
        }

        var (statusCode, title) = ErrorType switch
        {
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "Not Found"),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "Conflict"),
            ErrorType.BadRequest => (StatusCodes.Status400BadRequest, "Bad Request"),
            ErrorType.Unauthorized => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            ErrorType.Forbidden => (StatusCodes.Status403Forbidden, "Forbidden"),
            _ => (StatusCodes.Status400BadRequest, "Bad Request")
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = Error.Message
        };

        return new ObjectResult(problemDetails) { StatusCode = statusCode };
    }
}

public class Success<T>(T data) : Result<T>(data);

public class Failure<T>(string message) : Result<T>(new Exception(message), ErrorType.None);

public class NotFoundFailure<T>(string message = "Item not found.") : Result<T>(new NotFoundException(message), ErrorType.NotFound);

public class ConflictFailure<T>(string message) : Result<T>(new ConflictException(message), ErrorType.Conflict);

public class BadRequestFailure<T>(string message) : Result<T>(new Exception(message), ErrorType.BadRequest);

public class UnauthorizedFailure<T>(string message) : Result<T>(new Exception(message), ErrorType.Unauthorized);

public class ForbiddenFailure<T>(string message) : Result<T>(new Exception(message), ErrorType.Forbidden);

public enum ErrorType
{
    None,
    NotFound,
    Conflict,
    BadRequest,
    Unauthorized,
    Forbidden
}
