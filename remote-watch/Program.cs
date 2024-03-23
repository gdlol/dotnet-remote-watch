using System.Diagnostics;
using System.Reflection;
using Microsoft.DotNet.Cli.Utils;
using Constants = HotReload.Common.Constants;

if (Environment.GetEnvironmentVariable(Constants.RemoteWatch.BuildClient) == "1")
{
    string targetDir = EnvironmentVariable.Load(Constants.RemoteWatch.TargetDir);
    string targetAssemblyName = EnvironmentVariable.Load(Constants.RemoteWatch.TargetAssemblyName);
    string rid = EnvironmentVariable.Load(Constants.RemoteWatch.RuntimeIdentifier);

    Logger.Log("Sending target assembly name to remote-watch...");
    await TargetAssemblyName.SendAsync(targetAssemblyName);

    if (
        File.Exists(Path.Combine(targetDir, $"{Constants.HotReloadClient}"))
        || File.Exists(Path.Combine(targetDir, $"{Constants.HotReloadClient}.exe"))
    )
    {
        Logger.Log($"{Constants.HotReloadClient} exists in {targetDir}, skipping build.");
        return 0;
    }

    Logger.Log($"Building {Constants.HotReloadClient}...");
    var result = Command
        .Create(
            "dotnet",
            [
                "publish",
                .. (string.IsNullOrEmpty(rid) ? Enumerable.Empty<string>() : ["--runtime", rid]),
                "--output",
                targetDir,
                "--property:ImplicitUsings=enable",
                "--property:Nullable=enable",
                "--property:LangVersion=12.0",
                Path.Combine(AppContext.BaseDirectory, Constants.HotReloadClient, $"{Constants.HotReloadClient}.csproj")
            ]
        )
        .Execute();
    return result.ExitCode;
}
else
{
    if (Environment.GetEnvironmentVariable(Constants.DotNet.Watch) == "1")
    {
        // StartupHook is now forwarding DOTNET_HOTRELOAD_NAMEDPIPE_NAME to Grand parent process (below).
        await new TaskCompletionSource().Task;
        return 0;
    }
    else
    {
        string assemblyPath = Assembly.GetExecutingAssembly().Location;

        // Injects RunCommand and RunArguments to MSBuild, so that dotnet-watch will launch this App with
        // environment variable DOTNET_WATCH=1 instead of starting the actual App.
        Environment.SetEnvironmentVariable(Constants.DotNet.StartupHooks, assemblyPath);
        Environment.SetEnvironmentVariable(Constants.MSBuild.RunCommand, "dotnet");
        Environment.SetEnvironmentVariable(
            Constants.MSBuild.RunArguments,
            ArgumentEscaper.EscapeAndConcatenateArgArrayForProcessStart([assemblyPath])
        );
        Environment.SetEnvironmentVariable(
            Constants.RemoteWatch.Targets,
            Path.Combine(AppContext.BaseDirectory, "RemoteWatch.targets")
        );

        TargetAssemblyName.Listen();
        NamedPipeForwarder.Start();

        using var dotnetWatch = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = ArgumentEscaper.EscapeAndConcatenateArgArrayForProcessStart(
                    ["watch", .. Environment.GetCommandLineArgs().Skip(1)]
                ),
                UseShellExecute = false,
            }
        };
        AppDomain.CurrentDomain.ProcessExit += (_, _) => dotnetWatch.Kill(entireProcessTree: true);
        dotnetWatch.Start();
        await dotnetWatch.WaitForExitAsync();

        return dotnetWatch.ExitCode;
    }
}
