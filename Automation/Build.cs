using Cake.Common.Tools.DotNet;
using Cake.Frosting;

namespace Automation;

public class Build : FrostingTask<Context>
{
    public override void Run(Context context)
    {
        context.DotNetBuild(
            Context.ProjectRoot,
            new() { MSBuildSettings = new() { Properties = { ["TreatWarningsAsErrors"] = ["true"] } } }
        );
    }
}
