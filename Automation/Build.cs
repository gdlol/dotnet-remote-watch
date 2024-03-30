using Cake.Common.Tools.DotNet;
using Cake.Frosting;
using Path = System.IO.Path;

namespace Automation;

public class Build : FrostingTask<Context>
{
    public override void Run(Context context)
    {
        string traversalPath = Path.Combine(Context.ProjectRoot, "Traversal");
        context.DotNetClean(traversalPath);
        context.DotNetBuild(
            traversalPath,
            new() { MSBuildSettings = new() { Properties = { ["TreatWarningsAsErrors"] = ["true"] } } }
        );
    }
}
