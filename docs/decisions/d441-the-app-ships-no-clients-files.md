# The app ships no client's files (D441)

## D441: The app ships no client's files: the installed app prepares them from the client on its first start and at each update of Ankama's

*2026-11-17, outside the steps, on `fix/home-client-build`. The owner's choice among the lead's three, the lead's recommendation. Builds
D440; supersedes D172 (the app shipped one client build's pack, schema and mapping), and D173's release tied to a client build.*

- **Found.** The release workflow put beside the app the files of the build its `ClientVersion` named, taken from this repository's
  release `client-<version>` with `scripts/embed.cs`. D3B.Bis has published no such release (D426): a tag `v<version>` would have failed at
  that step. The home page, the status bar and the settings' About named that build, 3.7.13.17, while the host ran 3.7.14.18 from the
  files D440 prepared.
- **Made.** The workflow publishes and packs the app alone. `ClientVersion` leaves `src/D3B.Bis.App`, and `AppRelease` gives the version
  alone. The home page, the status bar and the About section name the build the host read: the one it runs for, else the one whose files
  it lacked, else "Client not read yet". The host still looks beside the app's executable after this PC's folder (`DataSettings.Locate`),
  and `scripts/embed.cs` still puts a build's files there, for whoever packs an app by hand.
- **Said before choosing.** Each installed app's first start, and each of Ankama's updates, takes about a minute and 3.4 GB of memory
  (D440), with no network; its mapping is made from pins alone, without the maintainer's captures. For it: nothing derived from the client
  is published any more, which ends the legal risk D10 and D24 accepted for the releases; the installer is about 110 MB lighter; an update
  of Ankama's needs no release of the app, only when its protocol changed.
- **Not settled here.** The repository is private (D426): an installed app finds no update and no report channel without a token.
- **Rejected.**
  - Publishing this repository's `client-<version>` releases and embedding them as before: a data release and an app release at every
    update of Ankama's, and the client's data on GitHub.
  - Taking AerafalDev/D3B's releases: a dependency on another repository being rewritten, which D440 meant to end.
