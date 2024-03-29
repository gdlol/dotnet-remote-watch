using Automation;
using Cake.Frosting;

Directory.SetCurrentDirectory(Context.Workspaces);
return CakeHost.Create().UseContext<Context>().Run(args.Concat(["--verbosity", "diagnostic"]));
