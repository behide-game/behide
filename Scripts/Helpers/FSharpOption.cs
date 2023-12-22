namespace Behide;

using Microsoft.FSharp.Core;

public class Option<T>
{
    static public FSharpOption<T> FromNullable(T? nullable) =>
        nullable switch
        {
            null => FSharpOption<T>.None,
            _ => FSharpOption<T>.Some(nullable)
        };

    static public T? ToNullable(FSharpOption<T> opt) =>
        FSharpOption<T>.GetTag(opt) switch
        {
            FSharpOption<T>.Tags.Some => opt.Value,
            _ => default
        };

    static public bool IsSome(FSharpOption<T> opt) => FSharpOption<T>.get_IsSome(opt);
    static public bool IsNone(FSharpOption<T> opt) => FSharpOption<T>.get_IsNone(opt);
}
