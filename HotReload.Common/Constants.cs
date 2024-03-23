namespace HotReload.Common;

public static class Constants
{
    public const string DeltaApplierDllName = "Microsoft.Extensions.DotNetDeltaApplier.dll";

    public const string HotReloadClient = "HotReload.Client";

    public static class DotNet
    {
        public const string Watch = "DOTNET_WATCH";

        public const string StartupHooks = "DOTNET_STARTUP_HOOKS";

        public const string HotReloadNamedPipeName = "DOTNET_HOTRELOAD_NAMEDPIPE_NAME";

        public const string HotReloadPort = "DOTNET_HOTRELOAD_PORT";

        public const string ModifiableAssemblies = "DOTNET_MODIFIABLE_ASSEMBLIES";
    }

    public static class MSBuild
    {
        public const string RunCommand = "RunCommand";

        public const string RunArguments = "RunArguments";
    }

    public static class RemoteWatch
    {
        public const string Targets = "RemoteWatchTargets";

        public const string BuildClient = "RemoteWatchBuildClient";

        public const string TargetDir = "RemoteWatchTargetDir";

        public const string RuntimeIdentifier = "RemoteWatchRuntimeIdentifier";

        public const string TargetAssemblyName = "RemoteWatchTargetAssemblyName";

        public const string TargetAssemblyNamedPipeName = "RemoteWatchTargetAssemblyNamedPipeName";

        public const string NamedPipeName = "RemoteWatchNamedPipeName";

        public const string PidChannel = "RemoteWatchPidChannel";

        public const string HealthChannel = "RemoteWatchHealthChannel";
    }
}
