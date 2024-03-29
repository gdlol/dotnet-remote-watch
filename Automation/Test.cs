using Cake.Common.Tools.DotNet;
using Cake.Frosting;
using Path = System.IO.Path;

namespace Automation;

public class Test : FrostingTask<Context>
{
    public override void Run(Context context)
    {
        context.DotNetRun(Path.Combine(Context.ProjectRoot, "HotReload.Test"));
    }
}
