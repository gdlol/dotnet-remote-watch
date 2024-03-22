using Automation.Tasks;
using Cake.Frosting;

namespace Automation;

[IsDependentOn(typeof(PrettierFormat))]
[IsDependentOn(typeof(CSharpierFormat))]
public class Format : FrostingTask<Context>;
