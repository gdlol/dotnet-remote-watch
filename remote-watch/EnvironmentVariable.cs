internal static class EnvironmentVariable
{
    public static string Load(string name) =>
        Environment.GetEnvironmentVariable(name)
        ?? throw new InvalidOperationException($"Environment variable {name} is not set.");
}
