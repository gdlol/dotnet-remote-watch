using System.Diagnostics;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using HotReload.Common;

await TestCase.RunAsync(
    "Hot reload with client initiated exit.",
    async (testProjectPath, watchServer, watchClient, pingListener, token) =>
    {
        await TestCase.ReceivePingAsync(pingListener, 1, token);

        string program = await File.ReadAllTextAsync(Path.Combine(testProjectPath, "Program.cs"), token);
        await File.WriteAllTextAsync(
            Path.Combine(testProjectPath, "Program.cs"),
            program.Replace("const byte value = 1;", "const byte value = 2;"),
            token
        );

        await TestCase.ReceivePingAsync(pingListener, 2, token);

        watchClient.KillTree();
        await watchServer.WaitForExitAsync(token);
    }
);

await TestCase.RunAsync(
    "Server initiated exit.",
    async (testProjectPath, watchServer, watchClient, pingListener, token) =>
    {
        await TestCase.ReceivePingAsync(pingListener, 1, token);

        watchServer.KillTree();
        await watchClient.WaitForExitAsync(token);
    }
);

{
    using var pidListener = new TcpListener(IPAddress.Loopback, TestCase.PidPort);
    pidListener.Start();
    await TestCase.RunAsync(
        "Restart on file change.",
        async (testProjectPath, watchServer, watchClient, pingListener, token) =>
        {
            await TestCase.ReceivePingAsync(pingListener, 1, token);

            Console.WriteLine("Terminating test program...");
            using var pidClient = await pidListener.AcceptTcpClientAsync(token);
            using var pidStream = pidClient.GetStream();
            var reader = PipeReader.Create(pidStream);
            int pid = await reader.ReadIntAsync(token);
            using var testProgram = Process.GetProcessById(pid);
            testProgram.Kill();
            await testProgram.WaitForExitAsync(token);

            string program = await File.ReadAllTextAsync(Path.Combine(testProjectPath, "Program.cs"), token);
            string newProgram = program.Replace("const byte value = 1;", "const byte value = 2;");
            using var cts = new CancellationTokenSource();
            var update = Task.Run(
                async () =>
                {
                    while (true)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(3), cts.Token);
                        Console.WriteLine("Updating program...");
                        await File.WriteAllTextAsync(
                            Path.Combine(testProjectPath, "Program.cs"),
                            newProgram,
                            cts.Token
                        );
                    }
                },
                token
            );
            await TestCase.ReceivePingAsync(pingListener, 2, token);
            await cts.CancelAsync();
            try
            {
                await update;
            }
            catch (OperationCanceledException) { }
        }
    );
}

{
    using var pidListener = new TcpListener(IPAddress.Loopback, TestCase.PidPort);
    pidListener.Start();
    await TestCase.RunAsync(
        "Rude edit.",
        async (testProjectPath, watchServer, watchClient, pingListener, token) =>
        {
            await TestCase.ReceivePingAsync(pingListener, 1, token);

            using var pidClient = await pidListener.AcceptTcpClientAsync(token);
            using var pidStream = pidClient.GetStream();
            var reader = PipeReader.Create(pidStream);
            int pid = await reader.ReadIntAsync(token);
            using var testProgram = Process.GetProcessById(pid);

            Console.WriteLine("Updating program...");
            string program = await File.ReadAllTextAsync(Path.Combine(testProjectPath, "Dummy.cs"), token);
            await File.WriteAllTextAsync(
                Path.Combine(testProjectPath, "Dummy.cs"),
                program.Replace("internal class Dummy2 { }", "internal class Dummy3 { }"),
                token
            );

            await testProgram.WaitForExitAsync(token);

            await TestCase.ReceivePingAsync(pingListener, 1, token);
        }
    );
}

Console.WriteLine("Done.");
