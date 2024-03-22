using Automation;
using Cake.Frosting;

Directory.SetCurrentDirectory(Context.Workspaces);
CakeHost.Create().UseContext<Context>().Run(args.Concat(["--verbosity", "diagnostic"]));
