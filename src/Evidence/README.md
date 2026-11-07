# PulseGrid.Evidence

.NET 10 application services for local evidence capture. `CaptureService` accepts an `IEvidenceAdapter` and `IBundleStore`; it has no filesystem archive or test framework dependencies.

After a release: `dotnet add package PulseGrid.Evidence --version 0.1.0`.
Capture one explicitly selected result file with `CaptureAsync(source, output)`. The output must not already exist. Optional revision/branch metadata is explicit.
