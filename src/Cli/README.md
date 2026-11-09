# PulseGrid.Cli

.NET 10 local-first tool for capture, inspect, validate, export, import and list.

Build and pack the solution, then install locally:

```sh
dotnet tool install --tool-path ./artifacts/tools --add-source ./artifacts/packages --configfile NuGet.Config PulseGrid.Cli --version 0.1.0
```

Use `pulsegrid help`. Each command supports `--json`; progress goes to stderr. Capture ingests existing xUnit TRX reports and does not execute tests. No account or server is required.
