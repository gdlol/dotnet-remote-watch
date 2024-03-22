using Automation.Tasks;
using Cake.Common.Tools.Command;
using Cake.Core;
using Cake.Core.IO;
using Cake.Frosting;
using Path = System.IO.Path;

namespace Automation;

[IsDependentOn(typeof(Pack))]
public class InstallRemoteWatch : FrostingTask<Context>
{
    public override void Run(Context context)
    {
        int exitCode = context.Command(
            ["dotnet"],
            out string _,
            ProcessArgumentBuilder.FromString("tool list drw --global"),
            expectedExitCode: 1
        );
        if (exitCode == 0)
        {
            context.Command(["dotnet"], ProcessArgumentBuilder.FromString("tool uninstall drw --global"));
        }
        context.Command(["dotnet"], "nuget disable source nuget.org");
        try
        {
            context.Command(
                ["dotnet"],
                new ProcessArgumentBuilder()
                    .Append("tool")
                    .Append("install")
                    .Append("drw")
                    .Append("--prerelease")
                    .Append("--global")
                    .AppendSwitch("--add-source", Path.Combine(Context.ProjectRoot, "Bin"))
            );
        }
        finally
        {
            context.Command(["dotnet"], "nuget enable source nuget.org");
        }
    }
}
