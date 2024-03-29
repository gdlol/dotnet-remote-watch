using Cake.Common.Tools.DotNet;
using Cake.Frosting;
using Path = System.IO.Path;

namespace Automation;

public class Build : FrostingTask<Context>
{
    public override void Run(Context context)
    {
        context.DotNetBuild(Path.Combine(Context.ProjectRoot, "Traversal"));
    }
}
