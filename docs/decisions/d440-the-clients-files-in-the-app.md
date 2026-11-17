# The client's files prepared by the app (D440)

## D440: The app prepares the installed client's pack, schema and mapping itself, in its own process, from a library the tools wrap; it downloads nothing

*2026-11-17, outside the steps, on `feat/client-update-in-app`. The owner chose that the app prepares a new build's files on the PC, without
a terminal and without another repository's release, then chose, among the lead's three ways, the library run in the app's process, and the
release downloader's removal. Builds D24, D426, D439; supersedes in part D10 (the readers of the client leave `scripts/` for `src/`, where
code has tests) and D172 (a build the app does not ship is no longer only said: the app makes its files).*

- **Found, on the owner's machine, client 3.7.14.18.** Run from `scripts/`, into a temporary folder: the extraction takes 58 s and peaks at
  3.4 GB of working set, the schema 7.5 s and 1.1 GB, the mapping 1 s and 58 MB. The pins of 3.7.13.17 carried over to 3.7.14.18, with no
  capture, give a mapping that holds: 224 messages paired, 99 by pin, the audit with no failure. This is what the owner's launcher on the
  Desktop already does with `dotnet run`, which an installed app cannot.
- **Found, what the tools would bring into the app.** The extraction needs AssetsTools.NET and AssetRipper.TextureDecoder (0.4 MB) and
  SkiaSharp, which Avalonia ships already; the schema needs Cpp2IL and what it pulls in (AsmResolver, Iced, Disarm, Capstone's wrapper),
  about 4 MB of assemblies and Capstone's native `capstone.dll`, 5.6 MB for win-x64: about 11 MB more in the installed app, all under MIT,
  Capstone's native library under BSD-3. The app is published self-contained, neither trimmed nor ahead of time: native AOT, which the
  file-based tools default to, binds nothing here. Cpp2IL keeps the client it opened in static state, one at a time in a process, reset once
  the schema is read; on x86-64 code it disassembles with Iced, managed code.
- **Made, the library.** `src/D3B.Bis.Extraction` holds what `scripts/extract`, `scripts/schema` and `scripts/unity` held, and the making of a
  mapping without captures, each step returning what it did instead of writing to the console; `scripts/extract.cs`, `scripts/schema.cs` and
  `scripts/mapping.cs` stay, the same options, as command lines over it. The pinned pairs of `src/D3B.Bis.Protocol/Pins` are resources of
  the protocol's assembly too, so that the installed app reads them.
- **Made, the app.** The home page's banner of a client Ankama updated keeps its button, its progress and its restart: Update extracts the
  pack, reads the schema, then makes the mapping from the pins of the latest build that has some, carried over to the installed one, without
  captures, on a thread of the app's own; the banner says the step and its progress, then offers the restart. A mapping that does not hold is
  said as such: the game's protocol changed and the code must follow it, which the app cannot do.
- **Made, all three or none.** The steps write under `.preparation` in the host's directory, and the three files join the build's directory
  (`BotHostStart.DataDirectory`) together, the mapping last, only once it holds. The host takes each file where it finds it first, this PC's
  before the app's (D172): a pack and a schema left there by a mapping that failed would be paired with the mapping a later app ships, and a
  schema of other bytes would fail its audit with no banner to say so. Found by the branch's review.
- **Made, the download gone.** `ClientReleaseDownloader` and its types leave `D3B.Bis.Data`, `Updates:ClientRepository` leaves the app, and
  `scripts/embed.cs` takes a release's files through the GitHub CLI again, as before this branch: the release workflow and the launcher have it.
- **Accepted debt.** The code moved from `scripts/` falls under `src/`'s rule of tests: what is new is tested (the steps in their order, the
  pins carried over, the progress, the mapping that does not hold); the readers of the client moved with it are checked by a run on the
  installed client, which neither the tests nor CI have.
- **Rejected.**
  - The app downloading the build's files from a release, made first on this branch: another repository's release, published after
    Ankama's update or never.
  - A child process, the app's own executable started with a verb, or a console executable shipped beside it: the memory given back and a
    crash kept apart, for a line protocol, a launcher and a second executable in the release; the app restarts once the files are made, and
    the library's surface lets the step move to a process later.
  - Keeping the downloader for `scripts/embed.cs` alone: code and tests in `src/` for a tool that the GitHub CLI served.
