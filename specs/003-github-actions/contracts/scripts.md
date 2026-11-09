# Contract: Check scripts

Two PowerShell scripts in `.github/scripts/`, each with a Pester 5 test file beside it. They run in
Windows PowerShell 5.1 and PowerShell 7 ([research.md](../research.md) R4, R6). They take plain
inputs, so their tests need no network, build, or GitHub. The workflow gathers the inputs.

Each script writes nothing on success except one line saying what passed, and on failure throws
with a message that states both values, so the job fails and the log says why (II.3).

## `Assert-VersionIsNewer.ps1`

| Parameter | Meaning |
|---|---|
| `-Version` | The csproj `<Version>`, such as `5.0.6` |
| `-ReleasedVersions` | Every release tag, such as `5.0.5`, `v3.0.0` |

- Passes when `-Version` is greater than every released version, comparing as `[version]` after
  stripping a leading `v`.
- Fails with: `Version <Version> must be greater than the highest released version, <highest>.`
- With no released versions, passes (a new repository's first release).

Cases (II.2.b.iii, around the highest release `5.0.5`): `5.0.6`, `5.1.0`, and `6.0.0` pass;
`5.0.5` and `5.0.4` fail; `5.0.10` passes against `5.0.9`; `v3.0.0` counts as `3.0.0`; the highest
release is used whatever the order of the list; an empty list passes.

## `Assert-CoverageNotLower.ps1`

| Parameter | Meaning |
|---|---|
| `-ReportPath` | The change's Cobertura report |
| `-BaselinePath` | The highest release's `coverage.cobertura.xml`; may not exist |

- Reads `lines-covered`, `lines-valid`, `branches-covered`, and `branches-valid` from each root
  `coverage` element.
- Passes when the change's lines fraction and branches fraction are each equal to or greater than
  the baseline's, compared exactly by cross-multiplication.
- Fails with, for whichever dropped:
  `Line coverage dropped from <b covered>/<b valid> (<b %>) to <c covered>/<c valid> (<c %>).`
  and likewise for branches; both are reported when both dropped.
- Fails with `No coverage report for the latest release: <BaselinePath>.` when the baseline is
  missing.

Cases: equal passes; one more covered line passes; one fewer covered line fails; one fewer covered
branch fails; both dropping reports both; more valid lines at the same fraction passes (for example
430/430 against 215/215); a lower fraction with more covered lines fails (for example 216/217
against 215/215); a missing baseline fails; zero valid branches on both sides passes; a change that
adds an uncovered branch where the release had none fails, because 0/0 counts as 100%.
