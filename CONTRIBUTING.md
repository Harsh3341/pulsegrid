# Contributing

Read PULSEGRID_SPEC.md, AGENTS.md and the ADRs. Keep changes scoped to a roadmap milestone. Discuss architectural changes before adding modules or external dependencies.

Use .NET 10/C# 14, nullable references, XML comments on public APIs and central package versions. Keep Core independent of integrations. Add meaningful unit/integration tests, adapter contract coverage and compatibility tests for schema changes. Do not add empty projects.

Run the restore/build/test/pack commands in README.md. Enable `git config core.hooksPath eng/hooks`; the development commit gate checks fully staged code. Pull requests target development and must pass Windows/Linux CI and receive review once the remote ruleset is enabled.

Original contributions use the MIT license. Review transitive dependency licenses and vulnerabilities before release. SDK Source Link metadata should resolve to the hosted repository and immutable revision. Keep package READMEs and implementation status updated. Deliberately failing sample tests belong outside the main solution.
