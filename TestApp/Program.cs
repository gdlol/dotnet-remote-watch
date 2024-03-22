static void PrintMessage()
{
    Console.WriteLine("Hello, World!");
}

while (true)
{
    PrintMessage();
    await Task.Delay(TimeSpan.FromSeconds(3));
}
