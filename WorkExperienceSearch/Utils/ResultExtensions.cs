using Work_Experience_Search.Types;

namespace Work_Experience_Search.Utils;

public static class ResultExtensions
{
    public static void ExpectSuccess(this Result result)
    {
        if (result.Failure) throw new InvalidOperationException("Expected success but was failure");
    }

    public static T ExpectSuccess<T>(this Result<T> result)
    {
        if (result.Success)
        {
            return result.Data;
        }
        else
        {
            throw new InvalidOperationException("Expected success but was failure");
        }
    }

    public static String ExpectFailure(this Result result)
    {
        if (!result.Success)
        {
            return "";
        }
        else
        {
            throw new InvalidOperationException("Expected failure but was success");
        }
    }

    public static String ExpectFailure<T>(this Result<T> result)
    {
        if (!result.Success)
        {
            throw new InvalidOperationException("Expected failure but got null exception.");
        }
        else
        {
            return "";
        }
    }
}
