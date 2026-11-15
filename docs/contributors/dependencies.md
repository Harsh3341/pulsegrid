# Dependency choices

Production XML parsing, compression, hashing, streaming and CLI parsing use the .NET 10 base libraries. JsonSchema.Net 7.3.3 supplies draft 2020-12 validation; its runtime dependencies Json.More.Net and JsonPointer.Net are MIT licensed. Core has no third-party dependency.

Tests use xUnit, xunit.runner.visualstudio and Microsoft.NET.Test.Sdk; these do not become product runtime dependencies. Versions are pinned in Directory.Packages.props. NuGet vulnerability auditing is enabled during restore. Review license/audit results again at release time because advisory data and dependencies can change.

SDK Source Link integration consumes Git origin/revision metadata. The hosted repository is https://github.com/Harsh3341/pulsegrid. Local packs are pre-release verification artifacts, not published releases.
