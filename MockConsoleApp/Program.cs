using MockConsoleApp;

Logger.Log(LogLevel.INFO, "Main", "This is Information.");
Logger.Log(LogLevel.Err, "Main", "This is Error!");

Class1.DoSomthing();

Task.Run(() =>
{
    Thread.Sleep(10);
    Logger.Log(LogLevel.Information, "Main", "Try logging different thread.");
});
Task.Run(() =>
{
    Thread.Sleep(10);
    Logger.Log(LogLevel.Info, "Main", "Try logging different thread too.");
});

Console.ReadKey();