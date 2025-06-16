using Automation.Tasks;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.NuGet.Push;
using Cake.Frosting;

namespace Automation;

[IsDependentOn(typeof(Pack))]
public class Publish : FrostingTask<Context>
{
    public override void Run(Context context)
    {
        string package = Directory.GetFiles(Context.PackageOutputPath).Single();
        string apiKey =
            Environment.GetEnvironmentVariable("NUGET_API_KEY")
            ?? throw new InvalidOperationException("NUGET_API_KEY is not set.");
        context.DotNetNuGetPush(
            package,
            new DotNetNuGetPushSettings
            {
                Source = "https://api.nuget.org/v3/index.json",
                ApiKey = apiKey,
                SkipDuplicate = true,
            }
        );
    }
}
