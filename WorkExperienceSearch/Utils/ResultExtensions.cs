using Work_Experience_Search.Exceptions;
using Work_Experience_Search.Types;

namespace Work_Experience_Search.Utils;

public static class ResultExtensions
{
    public static T ExpectSuccess<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            return result.Data;
        }
        else
        {
            throw new InvalidOperationException("Expected success but was failure");
        }
    }

    public static String ExpectFailure<T>(this Result<T> result)
    {
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException("Expected failure but got null exception.");
        }
        else
        {
            return result.Error?.Message ?? "No error message";
        }
    }
}
