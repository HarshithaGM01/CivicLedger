# Reproduction, limitations, and feedback

## Evidence baseline

The public commit `dd3c76adebf1598ee7b7a34c2df00bcb1f6ecd9b` has a successful
[GitHub Actions run](https://github.com/HarshithaGM01/CivicLedger/actions/runs/27162947793).
That result applies to the original commit, not subsequent local changes.

Local validation on 2026-10-12 using SDK 9.0.318: Release build and all
15 tests passed (0 failed, 0 skipped). The added regression suite covers
fractional-cent amounts, inclusive date boundaries, rejected-operation state,
and stale-write rollback using SQLite. It also exposed an EF tracking issue:
application-assigned entity IDs are now explicitly configured with
`ValueGeneratedNever` so newly appended expenses and audit entries are inserted.
The modified CI workflow has not run on GitHub; changes remain local.

## Clean reproduction

Use a disposable checkout and .NET 9 SDK. Run from the repository root:

```sh
dotnet restore CivicLedger.sln
dotnet build CivicLedger.sln --no-restore --configuration Release
dotnet test CivicLedger.sln --no-build --configuration Release --logger trx
dotnet run --project src/CivicLedger.Api --no-launch-profile --urls http://127.0.0.1:5080
```

Open http://127.0.0.1:5080 and verify http://127.0.0.1:5080/api/health.
Create a grant, activate it, add an in-period expense, restart the server, and
confirm the expense and audit entry remain. Use synthetic data only.
The app seeds sample grants automatically. Risk scores depend on the current
date; use the API's `asOf` parameter for a repeatable risk query.

The concurrency-token change adds a database column. `EnsureCreated` does not
upgrade existing databases. Use a fresh disposable demo database for this
revision; retain a backup of any data you need. A migration is required before
upgrading a database whose contents must be preserved.

## Safety and known limits

- There is no authentication, role authorization, or tenant isolation. Anyone
  who can reach the API can read and modify demo data. Bind to loopback; do not
  use real financial or personal information or expose this prototype publicly.
- Money now rejects nonpositive values and fractional cents rather than silently
  rounding. No currency conversion or multi-currency accounting is implemented.
- Optimistic concurrency now rejects stale grant writes. A regression test
  checks that a losing write does not persist its expense or audit entry.
  This does not prove correctness under every simultaneous SQLite lock scenario.
- There is no request idempotency key. Repeating an expense request can create
  a duplicate when sufficient budget remains. Do not blindly retry a request
  after an ambiguous timeout.
- Audit entries are application records, not tamper-proof records, and do not
  identify an authenticated actor.
- Risk scores are heuristics, not a trained model or a validated compliance
  decision. Stored status is refreshed on expense writes, not by a scheduler.
- Reads load full collections. Pagination, production load tests, schema
  migrations, and backup/restore validation remain future work.
- The UI uses externally loaded resources; a fully offline UI is not promised.
- Domain tests do not establish API authentication, browser security, or complete
  end-to-end coverage. No security audit is claimed.

## Genuine feedback study (not yet conducted)

Recruit 5–10 willing reviewers: for example, nonprofit budget volunteers,
student organization treasurers, or people who manage small project budgets.
Ask each to use synthetic data for these tasks without coaching:

1. Create and activate a grant.
2. Record an expense and explain the remaining budget.
3. Try an over-budget expense and explain the rejection.
4. Find the audit entry and interpret the risk explanation.

Record consent, task completion, errors, confusion, and one requested change.
Use anonymous participant IDs. Ask whether the workflow matches their actual
process; do not count recruited reviewers as municipal customers. Prioritize
one repeated problem, implement a fix, and invite the same people to retest.

| Participant | Task | Completed unaided? | Observation | Proposed change | Retest result |
| --- | --- | --- | --- | --- | --- |
| To be collected | | | | | |

No users have been contacted and no feedback or adoption is claimed here.
