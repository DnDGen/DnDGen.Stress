# Quickstart: Moving the Build and Release to GitHub Actions

The steps outside the repository, in the order they're done relative to the merge (FR-012), then
the checks that prove the feature works. For the next DnDGen package, the same steps apply with
its own names ([spec.md](spec.md) User Story 4).

## Prerequisites

- The branch `003-github-actions` with the workflow and scripts committed and pushed.
- Admin on DnDGen/DnDGen.Stress, owner of `DnDGen.Stress` on nuget.org, and admin of the DnDGen
  Azure DevOps project.
- The .NET 10 SDK, Pester 5, and the `gh` CLI, signed in.

## Before the pull request

1. **GitHub environment.** In DnDGen/DnDGen.Stress → Settings → Environments, create `Prod`. Under
   Deployment branches and tags, choose Selected branches and add `main`. No required reviewers
   ([data-model.md](data-model.md)).
2. **Trusted Publishing policy.** On nuget.org, signed in as `cidthecoatrack` → Trusted
   Publishing → Create: Owner `cidthecoatrack`, Repository Owner `DnDGen`, Repository
   `DnDGen.Stress`, Workflow File `release.yml`, Environment `Prod`. If the policy was created with
   `build.yml`, edit that field.
3. **Baseline coverage report for 5.0.5.** From a clean checkout:

   ```bash
   git worktree add ../stress-5.0.5 5.0.5
   ```

   ```bash
   dotnet test ../stress-5.0.5/DnDGen.Stress.Tests/DnDGen.Stress.Tests.csproj -c Stress --collect "Code Coverage;Format=cobertura" --settings ../stress-5.0.5/CodeCoverage.runsettings --results-directory ../stress-5.0.5/TestResults
   ```

   Copy the `*.cobertura.xml` it reports to `coverage.cobertura.xml`, and remove the local folder
   prefix from its `filename` attributes (it can contain your user name, and the release is
   public), leaving paths like `DnDGen.Stress\Stressor.cs`. Confirm its root shows `lines-covered`
   equal to `lines-valid` and `branches-covered` equal to `branches-valid`, then:

   ```bash
   gh release upload 5.0.5 coverage.cobertura.xml --repo DnDGen/DnDGen.Stress
   ```

   ```bash
   git worktree remove ../stress-5.0.5
   ```

## The pull request

4. **Open it** (title and description in
   [contracts/repository-changes.md](contracts/repository-changes.md)). Only `check` runs, and it
   passes: version `5.0.6` is greater than `5.0.5`, and coverage matches 5.0.5's report.
5. **Require the check.** In Settings → Rules → Rulesets, open `main`: add "Require status checks
   to pass" with `check`, leave "Require branches to be up to date" off, and set Enforcement to
   Active.
6. **Validate the gates** on the pull request, each a temporary commit, reverted after:
   - Set `<Version>` to `5.0.5`: `check` fails in its version step, naming `5.0.5` and `5.0.5`,
     and the merge button is blocked (FR-007, FR-016).
   - Add `Assert.Fail("deliberate")` to a test: `check` fails, and the test's name and message are
     in the job's log (FR-002).
   - Delete one unit test that alone covers a branch, such as
     `StressorTests.ThrowExceptionIfOptionsAreNotValid`, and lower `StressorTests.TestCount` by one,
     since the suite counts its own tests and other tests fail otherwise: `check` fails in its
     coverage step, stating both figures (FR-003).
   - Confirm every run lists only `check`, with no skipped `publish` or `release` (FR-002, FR-006).
   - Not exercised: a publish of a version NuGet.org already has (spec, User Story 1 scenario 6).
     It holds by design, since `publish` passes no `--skip-duplicate` and `release` needs
     `publish`; exercising it would mean publishing a throwaway version.
7. **Pester**: locally, `Invoke-Pester -Path .github/scripts` passes, in Windows PowerShell and, if
   installed, PowerShell 7.

## Before merging

8. **Retire Azure DevOps.** In the DnDGen Azure DevOps project, open each of the
   `DnDGen.Stress - Build` pipeline and its release pipeline → ⋯ → Settings → Disabled. This must
   happen before the merge, or both systems try to release 5.0.6 (User Story 3).

## Merge and verify

9. **Merge.** On `main`, the `Release` workflow runs `check`, `publish`, and `release` in order.
10. **Verify the release (SC-001):**
    - NuGet.org lists `DnDGen.Stress` 5.0.6, and its README shows the new build badge.
    - The 5.0.6 GitHub release has generated notes, `DnDGen.Stress.5.0.6.nupkg`, and
      `coverage.cobertura.xml` attached, and tag `5.0.6` points at the merge commit.
    - Only one 5.0.6 exists everywhere, and Azure DevOps shows no new run.
    - The README badge on GitHub is green and links to the `Release` workflow.
11. **Search (SC-005):** `git grep -n -e "dev.azure.com" -e "cicd/" -- ":!specs"` finds nothing.
    (`.gitignore`'s standard Azure publish entries aren't about Azure Pipelines and stay.)

## After the first release

12. **Revoke the old credentials (SC-002).** On nuget.org, delete the API key Azure DevOps used.
    In Azure DevOps, delete the `NuGet.org` and `github.com_cidthecoatrack` service connections,
    and delete the two pipelines once nothing else needs their history.
