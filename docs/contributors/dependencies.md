# Dependency choices

Production XML parsing, compression, hashing, streaming and CLI parsing use the .NET 10 base libraries. JsonSchema.Net 7.3.3 supplies draft 2020-12 validation; its runtime dependencies Json.More.Net and JsonPointer.Net are MIT licensed. Core has no third-party dependency.

Tests use xUnit, xunit.runner.visualstudio and Microsoft.NET.Test.Sdk; these do not become product runtime dependencies. Versions are pinned in Directory.Packages.props. NuGet vulnerability auditing is enabled during restore. Review license/audit results again at release time because advisory data and dependencies can change.

SDK Source Link integration consumes Git origin/revision metadata. The hosted repository is https://github.com/Harsh3341/pulsegrid. Local packs are pre-release verification artifacts, not published releases.

Dependabot checks NuGet packages and GitHub Actions weekly on Monday at 19:00 IST and opens pull requests against `development`, with at most three open version-update PRs per ecosystem. Reviews and required CI still apply; updates are not automatically merged. GitHub Actions are pinned to immutable commit IDs with version comments so dependency updates can track them. Workflow tokens are read-only, checkout does not persist credentials, and CI cancels superseded runs and limits each job to 20 minutes.

GitHub dependency alerts, automated security fixes, secret scanning and push protection are enabled. Vulnerability reports use the repository's private reporting channel.
