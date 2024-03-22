using Automation.Tasks;
using Cake.Frosting;

namespace Automation;

[IsDependentOn(typeof(PrettierCheck))]
[IsDependentOn(typeof(CSharpierCheck))]
[IsDependentOn(typeof(SpellCheck))]
public class Lint : FrostingTask<Context>;
