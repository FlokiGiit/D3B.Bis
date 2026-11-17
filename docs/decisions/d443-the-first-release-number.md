# The first release's number (D443)

## D443: D3B.Bis's first release is v1.0.0, not v0.10.0: D3B's tags reach v0.14.0

*2026-11-17, outside the steps, chosen by the owner as the lead recommended it. Refines D442 (its version).*

- **Found.** Pushing `v0.10.0` failed on the owner's machine: AerafalDev/D3B, a remote of this clone, had published `v0.10.0` to `v0.14.0`,
  outside this repository's history, which the clone had fetched. The lead had recommended `v0.10.0` thinking D3B stopped at 0.9.x.
- **Made.** The first release of D3B.Bis is `v1.0.0`, on `main`, a number of its own that no tag of D3B's takes. The clone fetches no tag
  from `upstream` any more (`remote.upstream.tagOpt --no-tags`), and D3B's tags outside this history were deleted from it. The apps
  install apart (`D3B.App` and `D3B.Bis.App`), so the numbers never mix updates.
- **Rejected.**
  - `v0.15.0`: after D3B's last, which D3B may take too.
  - `v0.10.0` all the same: two different v0.10.0, and D3B's back at each fetch from `upstream`.
