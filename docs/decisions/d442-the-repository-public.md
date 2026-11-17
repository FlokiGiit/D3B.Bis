# The repository public (D442)

## D442: FlokiGiit/D3B.Bis is made public, then released as v0.10.0, the first app without the client's files

*2026-11-17, outside the steps, chosen by the owner as the lead recommended it. Builds D174, D410, D426, D441; settles D441's "not
settled here".*

- **Found.** The repository was private (D426): an installed app reads its updates from the repository's releases with no token
  (Velopack's GitHub source), and its report channel from a release asset (D410), so neither worked. Checked on 2026-11-17 on the history
  of `origin/main` and the branch of D441, as D174 did: no token (the only webhook URLs are the tests' fakes), no data extracted from the
  client, no capture and no binary, the largest file `docs/decisions.md`; the authors' addresses are GitHub's noreply ones; no release
  and no secret on the repository.
- **Made, in order, each on the owner's go.** D441's pull request merged; the repository made public; the tag `v0.10.0` pushed, which
  has the release workflow publish the app alone (D441). A minor version, since D441 changes what a release holds; `v0.9.2` is D3B's tag.
- **What it changes.** Anyone reads the code, its history and its releases, and can copy them, for good. No data derived from the client
  is published (D441), so D24's risk narrows to a notice against the code. The report channel's webhook, once its asset is published,
  is readable by anyone, as D410 accepted. CI keeps running forks' pull requests with a read-only token and no secret.
- **Rejected.**
  - Staying private: the app for the owner alone, with no update and no report for other players.
  - A public repository for the releases alone: a token the owner makes and the workflow holds, a second repository to keep, for
    code that was public as D3B.
