using MockConsoleApp;

Logger.Log(LogLevel.INFO, "This is Information.");
Logger.Log(LogLevel.Err, "This is Error!");

Class1.DoSomthing();

Task.Run(() =>
{
    Thread.Sleep(10);
    Logger.Log(LogLevel.Information, "Try logging different thread.");
});
Task.Run(() =>
{
    Thread.Sleep(10);
    Logger.Log(LogLevel.Info, "Try logging different thread too.");
});

Console.ReadKey();