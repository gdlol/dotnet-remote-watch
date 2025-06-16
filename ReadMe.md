# dotnet-remote-watch

[NuGet Badge]: https://img.shields.io/nuget/v/drw
[NuGet URL]: https://www.nuget.org/packages/drw
[AppVeyor Badge]: https://img.shields.io/appveyor/build/gdlol/dotnet-remote-watch/main
[AppVeyor URL]: https://ci.appveyor.com/project/gdlol/dotnet-remote-watch/branch/main
[DotNet Badge]: https://img.shields.io/badge/.NET-net8.0-blue
[DotNet URL]: https://dot.net
[License Badge]: https://img.shields.io/github/license/gdlol/dotnet-remote-watch

[![NuGet Badge][NuGet Badge]][NuGet URL]
[![AppVeyor Badge][AppVeyor Badge]][AppVeyor URL]
[![DotNet Badge][DotNet Badge]][DotNet URL]
[![License Badge][License Badge]](LICENSE)

Turns `dotnet watch` into a client-server model, so that the server (watcher) and the client (App) do not have to be run in the same host.

The primary intended use case is to run `dotnet watch` inside a dev container, and have the client run on the host, which does not necessarily have the .NET SDK/runtime installed. Hot-reloading Windows desktop apps (e.g., WPF, WinUI) with dev containers is now possible.

With some custom port forwarding, it should be possible to hot-reload an application on a remote machine or container.

## Installation

```shell
dotnet tool install --global drw
```

## Usage

1. Add the following section to project file:

   ```xml
   <Import Project="$(RemoteWatchTargets)" Condition="'$(RemoteWatchTargets)' != ''" />
   ```

1. Launch server:

   ```shell
   dotnet remote-watch
   ```

   `remote-watch` is supposed to be used in the same way as `dotnet watch` and it will launch and forward all arguments to `dotnet watch` after initialization.

1. Launch `HotReload.Client` generated in the output directory. It will launch the target App after connecting to `remote-watch`.
1. Update source code and observe the changes.

## Environment variables

- `DOTNET_HOTRELOAD_HOST`: The hostname/IP address of the server, defaults to `localhost`.
- `DOTNET_HOTRELOAD_PORT`: The port to use for the client-server communication, default is 3000.

## MSBuild

Consider setting the following MSBuild properties in the project file or environment variables:

- `SelfContained`: Set to true so that the client can run on an environment without the .NET SDK/runtime installed.
- `RuntimeIdentifier`: Set the target runtime identifier if it's different from where the watcher runs.

## How it works

`remote-watch` intercepts the named pipe connection between `dotnet-watch` and `Microsoft.Extensions.DotNetDeltaApplier.dll` (injected into the target application via StartupHooks). The server forwards the intercepted data to `HotReload.Client` through a TCP connection (`tcp://localhost:$DOTNET_HOTRELOAD_PORT`), and `HotReload.Client` forwards the data to the target application through another named pipe.

Note that the internal implementation of `dotnet-watch` may change in the future.

### dotnet watch

![dotnet-watch](Assets/dotnet-watch.svg)

### dotnet remote-watch

![dotnet-remote-watch](Assets/dotnet-remote-watch.svg)
