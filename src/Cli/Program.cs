using PulseGrid.Cli;

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};
return await new CliApplication(Console.Out, Console.Error).RunAsync(args, cancellation.Token);
