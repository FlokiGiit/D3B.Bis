# D3B.Bis (D426)

## D426: D3B.Bis continues D3B in a repository of its own, its projects and namespaces under D3B.Bis, its files in a folder of its own, D3B's formats kept

*2026-11-13, outside the steps. The owner's choice: to go on in C# on .NET 10 for Windows while D3B is written again on another stack, in an
independent repository, with `D3B.Bis` as the root of the names. The rest is the lead's. Builds D10, D24, D25, D100, D171, D172, D296, D410.*

- **Found, in the code.** No project sets a `RootNamespace` or an `AssemblyName`: both come from the project file's name, so the projects, the
  assemblies, the namespaces, the `InternalsVisibleTo` names, the `avares://` and `clr-namespace` of the XAML and the base names of the resources
  (`Localizer`, `FightTexts`, `HarvestTexts`) are one name. A pack's index stores the full name of each entry's type and a read asks for it
  (`GamePack.Read`); `PackFormatTests` hashes those names into the layout it records for each format. Nothing else stores a .NET name: the
  database's history table holds migration ids, the studio's assets hold their own ids (`d3b.harvest`), the mapping's digest reads the proto
  packages (`d3b.game`), which are not namespaces, and the app's preferences hold configuration keys.
- **Found, on the owner's machine, 2026-11-13.** `%LOCALAPPDATA%\D3B` holds D3B's database, project, preferences and captures, and D3B 0.9.1,
  installed in `%LOCALAPPDATA%\D3B.App`, was running on them. The same folder also holds an `uninstall.exe` and a `data` folder of JSON files
  that D3B's rewrite installed on 2026-11-11, where the packs used to be. The release `client-3.7.13.17` of `AerafalDev/D3B` holds the pack of
  format 12 and the mapping of format 17, which this code reads.
- **Made, the names.** Every project, assembly and namespace is `D3B.Bis.<Module>`, the solution `D3B.Bis.slnx`, the tools `D3B.Bis.Scripts.*`,
  the hooks `D3B.Bis.Hooks`; Velopack's id and the executable are `D3B.Bis.App`, so that D3B and D3B.Bis install side by side; the product, the
  window's title and the home page say D3B.Bis. The license, its copyright and the authors of the build's properties are D3B's, unchanged.
- **Made, the formats kept.** `PackTypeNames` gives the name a pack stores for a type: its full name with `D3B.` where the namespace says
  `D3B.Bis.`. The layout recorded for format 12 is the same hash as before, so D3B's published packs are read and a pack written now is the same
  bytes. The magics `D3B-PACK` and `D3B-CAPT`, the extensions `.d3bcap` and `.d3breport`, a report's code, `d3b.db`, the behaviors' ids, the
  proto packages and `D3B_REPORT_WEBHOOK` are formats too, and stay.
- **Made, the folder.** D3B.Bis keeps its files in `%LOCALAPPDATA%\D3B.Bis`. `PreviousInstall.Import`, at the first start of the app or the host,
  copies D3B's database through SQLite's backup, the studio's project and the app's preferences, each only where D3B.Bis has none. D3B's files
  are only read; the read of its database, in WAL mode, left SQLite's empty `-wal` and `-shm` beside it, which the next write of D3B clears.
  Tested on temporary folders, and seen once on the owner's machine while D3B ran: 1 account, 2 characters, 539 graphics and 28 runs came across.
- **The earlier entries.** Their project names and paths follow the rename, so that what they point at is found; what they say of
  `%LOCALAPPDATA%\D3B` and of the name D3B is as it was written. The docs still say D3B for short.
- **Rejected.**
  - Keeping `%LOCALAPPDATA%\D3B`: two lines of code would migrate one database each its own way, and an uninstaller that is not ours lives in
    that folder.
  - Moving D3B's files instead of copying them: D3B would lose them while it is still installed.
  - Storing the new type names in the packs: it is a new format for no new content, and D3B's published packs would no longer be read.
  - Renaming the magics, the extensions and the behaviors' ids: every capture, report and profile made so far would need a second reader.
  - Rewriting D3B to D3B.Bis throughout the docs and the app's texts: 30 decision files say D3B as a record, and the short name reads the same.
