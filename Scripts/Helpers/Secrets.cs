using System.Linq;
using Godot;

namespace Behide;

public static class Secrets
{
    private static readonly (string, string)[] DotenvFile =
        FileAccess
            .GetFileAsString("res://.env")
            .ReplaceLineEndings("\n")
            .Split("\n")
            .Where(line => line.Contains('='))
            .Select(line =>
            {
                var arr = line.Split('=', 2);
                return (arr[0], arr[1]);
            })
            .ToArray();

    public static readonly string SignalingHubUrl = DotenvFile.First(x => x.Item1 == "SIGNALING_URL").Item2;
    public static readonly string RelayUsername = DotenvFile.First(x => x.Item1 == "RELAY_USERNAME").Item2;
    public static readonly string RelayPassword = DotenvFile.First(x => x.Item1 == "RELAY_PASSWORD").Item2;
}
