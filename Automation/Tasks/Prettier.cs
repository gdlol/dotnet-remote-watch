using Cake.Common.Tools.Command;
using Cake.Frosting;

namespace Automation.Tasks;

public class PrettierCheck : FrostingTask<Context>
{
    public override void Run(Context context)
    {
        context.Command(["pnpm"], $"prettier --check {Context.ProjectRoot}");
    }
}

public class PrettierFormat : FrostingTask<Context>
{
    public override void Run(Context context)
    {
        context.Command(["pnpm"], $"prettier --write {Context.ProjectRoot}");
    }
}
