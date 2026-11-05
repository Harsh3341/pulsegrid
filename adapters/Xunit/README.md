# PulseGrid.Evidence.Xunit

Captures existing xUnit TRX results from `dotnet test --logger trx`. No xUnit runtime dependency and no command execution.

After a release: `dotnet add package PulseGrid.Evidence.Xunit --version 0.1.0`.
Use `TrxEvidenceAdapter.CaptureAsync(stream)`. TRX is framework-neutral; this adapter is validated against xUnit/VSTest. Native xUnit XML and Microsoft.Testing.Platform are separate future integrations.

Diagnostics, logs and parameterized display names are excluded. Only stable ASCII class/method identifiers are retained; unavailable identifiers make results incomplete. Limit: 10,000 results, 64 MiB XML; DTDs and external entities are prohibited.
