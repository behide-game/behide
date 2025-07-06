namespace Behide;

using Microsoft.FSharp.Core;

public static class FSharpOptionExtension
{
    public static T? ToNullable<T>(this FSharpOption<T> opt) =>
        FSharpOption<T>.GetTag(opt) switch
        {
            FSharpOption<T>.Tags.Some => opt.Value,
            _ => default
        };
}
