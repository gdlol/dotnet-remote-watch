using Cake.Common.Tools.Command;
using Cake.Common.Tools.DotNet;
using Cake.Frosting;

namespace Automation;

public class Restore : FrostingTask<Context>
{
    public override void Run(Context context)
    {
        context.Command(["dotnet"], "tool restore");
        context.DotNetRestore(Context.ProjectRoot);
    }
}
