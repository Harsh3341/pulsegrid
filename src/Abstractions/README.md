# PulseGrid.Abstractions

.NET 10 contracts for evidence adapters and bundle stores. Adapters sanitize input and report completeness; stores validate all reads and publish atomically.

After a release: `dotnet add package PulseGrid.Abstractions --version 0.1.0`.
Implement `IEvidenceAdapter` or `IBundleStore` to add integrations without coupling Core to infrastructure.
