using Automation.Tasks;
using Cake.Frosting;

namespace Automation;

[IsDependentOn(typeof(PrettierCheck))]
[IsDependentOn(typeof(DotNetFormatCheck))]
[IsDependentOn(typeof(CSharpierCheck))]
[IsDependentOn(typeof(CSpell))]
public class Lint : FrostingTask<Context>;
