using Automation.Tasks;
using Cake.Common.Tools.Command;
using Cake.Common.Tools.DotNet;
using Cake.Frosting;
using Path = System.IO.Path;

namespace Automation;

public class Restore : FrostingTask<Context>
{
    public override void Run(Context context)
    {
        context.Command(["pnpm"], "install");
        context.DotNetRestore(Path.Combine(Context.ProjectRoot, "Traversal"));
    }
}
