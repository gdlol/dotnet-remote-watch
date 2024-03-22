using Cake.Common.Tools.DotNet;
using Cake.Frosting;

namespace Automation.Tasks;

public class CSharpierCheck : FrostingTask<Context>
{
    public override void Run(Context context)
    {
        context.DotNetTool($"csharpier {Context.ProjectRoot} --check");
    }
}

public class CSharpierFormat : FrostingTask<Context>
{
    public override void Run(Context context)
    {
        context.DotNetTool($"csharpier {Context.ProjectRoot}");
    }
}
