# Development branch checks

Local commits on `development` run the Release build and tests when code, schemas or build settings change. Enable the repository hook with `git config core.hooksPath eng/hooks`. Restore dependencies first. Code/configuration must be fully staged so checks cover the proposed commit. Hooks are a developer aid and can be bypassed; server rules provide enforcement.

CI defines `build-test (ubuntu-latest)` and `build-test (windows-latest)` for pushes and pull requests targeting `development`. Both restore, build, test and pack.

After connecting a GitHub remote, a repository administrator can apply the versioned ruleset:

```sh
gh api --method POST repos/OWNER/REPOSITORY/rulesets --input .github/development-ruleset.json
```

It requires one approving review, resolved discussions, an up-to-date branch, both CI checks, linear history and prevents deletion or force pushes. Review the repository's available GitHub protection features before applying. Origin is https://github.com/Harsh3341/pulsegrid.git; server enforcement must be applied with repository administrator authorization.
