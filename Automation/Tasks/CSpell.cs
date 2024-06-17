using Cake.Common.Tools.Command;
using Cake.Frosting;

namespace Automation.Tasks;

public class CSpell : FrostingTask<Context>
{
    public override void Run(Context context)
    {
        context.Command(["pnpm"], $"cspell {Context.ProjectRoot}");
    }
}
