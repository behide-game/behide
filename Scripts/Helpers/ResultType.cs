using Microsoft.FSharp.Core;

namespace Behide.Types;

public static class CustomFSharpResultExtensions
{
    public static bool HasValue<T, TE>(this FSharpResult<T, TE> result, out T value)
    {
        value = result.ResultValue;
        return result.IsOk;
    }

    public static bool HasError<T, TE>(this FSharpResult<T, TE> result, out TE error)
    {
        error = result.ErrorValue;
        return result.IsError;
    }
}
