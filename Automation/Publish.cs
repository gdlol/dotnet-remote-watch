using Automation.Tasks;
using Cake.Frosting;

namespace Automation;

[IsDependentOn(typeof(Pack))]
public class Publish : FrostingTask<Context>;
