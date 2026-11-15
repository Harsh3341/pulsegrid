# Development branch checks

Local commits on `development` run the Release build and tests when code, schemas or build settings change. Enable the repository hook with `git config core.hooksPath eng/hooks`. Restore dependencies first. Code/configuration must be fully staged so checks cover the proposed commit. Hooks are a developer aid and can be bypassed; server rules provide enforcement.

CI defines `build-test (ubuntu-latest)` and `build-test (windows-latest)` for pushes and pull requests targeting `development`. Both restore, build, test, capture the deliberately failing sample and pack. These job names are required checks; preserve them when modifying CI.

The GitHub repository's default branch is `development`. The `Protect development` ruleset requires a pull request, resolved review threads, an up-to-date branch, both CI checks and linear history. It prevents branch deletion and force pushes and has no bypass actors. Required approving reviews are zero for the solo-maintainer workflow; authors may merge their own PRs after checks pass. Independent review is encouraged when another contributor is available.

Changing `.github/development-ruleset.json` does not automatically update GitHub. A repository administrator must apply the matching server setting. In GitHub, open Settings → Rules → Rulesets → Protect development, keep the pull-request rule enabled, set required approvals to zero (clear “Require approvals” if shown), and save. Keep the required status checks and all other protection rules enabled.

Open changes from a feature branch and merge after review and checks pass. Rebase merges preserve focused commits; squash merges combine a PR into one commit. Merge commits are disabled to preserve linear history. The repository deletes merged feature branches automatically and permits updating a PR branch from its base. Squash commit titles use the PR title; write it as a focused description of the change.

A repository administrator provisioning another repository can apply the versioned ruleset:

```sh
gh api --method POST repos/OWNER/REPOSITORY/rulesets --input .github/development-ruleset.json
```

Review the target repository's available GitHub protection features before applying. The current origin is https://github.com/Harsh3341/pulsegrid.git. Keep repository administration separate from normal feature development.
