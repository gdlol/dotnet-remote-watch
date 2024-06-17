using Cake.Common.Tools.DotNet;
using Cake.Frosting;

namespace Automation.Tasks;

public class DotNetFormatCheck : FrostingTask<Context>
{
    public override void Run(Context context)
    {
        context.DotNetMSBuild(
            Context.ProjectRoot,
            new() { Targets = { "GetTargetPath" }, Properties = { ["DotNetFormatCheck"] = ["true"] } }
        );
    }
}

public class DotNetFormat : FrostingTask<Context>
{
    public override void Run(Context context)
    {
        context.DotNetMSBuild(
            Context.ProjectRoot,
            new() { Targets = { "GetTargetPath" }, Properties = { ["DotNetFormat"] = ["true"] } }
        );
    }
}
