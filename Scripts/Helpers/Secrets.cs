namespace Behide;

public static class Secrets
{
    private static readonly (string, string)[] dotenvFile =
        Godot.FileAccess
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

    public static readonly string SignalingHubUrl = dotenvFile.First(x => x.Item1 == "SIGNALING_URL").Item2;
    public static readonly string RelayUsername = dotenvFile.First(x => x.Item1 == "RELAY_USERNAME").Item2;
    public static readonly string RelayPassword = dotenvFile.First(x => x.Item1 == "RELAY_PASSWORD").Item2;
}
