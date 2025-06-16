using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Frosting;
using Microsoft.DotNet.Cli.Utils;

namespace Automation.Tasks;

public class Pack : AsyncFrostingTask<Context>
{
    private static async Task<string> GetReadMePathAsync()
    {
        string readMePath = Path.Combine(Context.PackageOutputPath, "ReadMe.md");
        string text = await File.ReadAllTextAsync(Path.Combine(Context.ProjectRoot, "ReadMe.md"));
        string assetsPrefix = Environment.GetEnvironmentVariable("README_ASSETS_PREFIX") ?? string.Empty;
        text = text.Replace("Assets/", $"{assetsPrefix}Assets/");
        await File.WriteAllTextAsync(readMePath, text);
        return readMePath;
    }

    public override async Task RunAsync(Context context)
    {
        context.CleanDirectory(Context.PackageOutputPath);
        string authors =
            Environment.GetEnvironmentVariable("AUTHORS")
            ?? Command.Create("git", ["config", "user.name"]).CaptureStdOut().Execute().StdOut.Trim();
        if (string.IsNullOrWhiteSpace(authors))
        {
            authors = "remote-watch";
        }
        string readMePath = await GetReadMePathAsync();
        try
        {
            Environment.SetEnvironmentVariable("ReadMePath", readMePath);
            context.DotNetPack(
                Path.Combine(Context.ProjectRoot, "remote-watch"),
                new()
                {
                    MSBuildSettings = new()
                    {
                        Properties =
                        {
                            ["PackageOutputPath"] = [Context.PackageOutputPath],
                            ["Authors"] = [authors],
                            ["PackageDescription"] = ["dotnet remote watch"],
                            ["PackageLicenseExpression"] = ["MIT"],
                            ["PackageRequireLicenseAcceptance"] = ["true"],
                            ["PackageTags"] = ["remote watch hot reload"],
                            ["PackageReadmeFile"] = ["ReadMe.md"],
                        },
                    },
                }
            );
        }
        finally
        {
            File.Delete(readMePath);
        }
    }
}
