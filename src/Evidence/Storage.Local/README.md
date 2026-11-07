# PulseGrid.Evidence.Storage.Local

.NET 10 filesystem bundle store implementing `IBundleStore`. Supports atomic creation, complete validation, validated copy and selective evidence extraction.

After a release: `dotnet add package PulseGrid.Evidence.Storage.Local --version 0.1.0`.
Use `LocalBundleStore`. No database or remote service. Existing outputs are never replaced. Artifacts are streamed; archive entries are strictly allowlisted and bounded. See the bundle format guide for limits.
