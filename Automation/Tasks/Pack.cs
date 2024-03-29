using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Frosting;
using Microsoft.DotNet.Cli.Utils;

namespace Automation.Tasks;

public class Pack : FrostingTask<Context>
{
    public override void Run(Context context)
    {
        context.CleanDirectory(Context.PackageOutputPath);
        string authors =
            Environment.GetEnvironmentVariable("AUTHORS")
            ?? Command.Create("git", ["config", "user.name"]).CaptureStdOut().Execute().StdOut.Trim();
        if (string.IsNullOrWhiteSpace(authors))
        {
            authors = "remote-watch";
        }
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
                        ["PackageReadmeFile"] = ["ReadMe.md"]
                    }
                }
            }
        );
    }
}
