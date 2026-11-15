# Contributing

Read [implementation status](docs/implementation-status.md), [repository guidance](AGENTS.md) and the [architecture decision](docs/architecture/0001-evidence-mvp.md). These describe the public implementation scope; the maintainer's private product plan is not required to contribute. Keep changes scoped to a documented milestone. Discuss architectural changes in a feature proposal before adding modules or external dependencies.

Use .NET 10/C# 14, nullable references, XML comments on public APIs and central package versions. Keep Core independent of integrations. Add meaningful unit/integration tests, adapter contract coverage and compatibility tests for schema changes. Do not add empty projects.

Create a feature branch from an up-to-date `development`, make focused commits and open a pull request targeting `development`. Use a concise PR title that describes the resulting change; squash merges use that title as the commit message.

Run the restore/build/test/pack commands in [README.md](README.md). Enable `git config core.hooksPath eng/hooks`; the development commit gate checks fully staged code. The protected branch requires passing Windows/Linux CI, one approving review, resolved review threads and an up-to-date branch. See [branch checks](docs/contributors/branch-protection.md). A PR author cannot provide their own required approval.

Bug reports should include the PulseGrid revision, operating system, SDK version and minimal reproduction steps. Share only sanitized examples. Use [private reporting](https://github.com/Harsh3341/pulsegrid/security/advisories/new) for vulnerabilities.

Original contributions use the MIT license. Review transitive dependency licenses and vulnerabilities before release. SDK Source Link metadata should resolve to the hosted repository and immutable revision. Keep package READMEs and implementation status updated. Deliberately failing sample tests belong outside the main solution.
