# The marketplace dropped (D439)

## D439: The owner drops the marketplace: the bot neither follows prices nor sells, the work of D427 to D438 stays on branches never merged, the fixes found on the way are kept

*2026-11-17, step 11, after HDV 5 bis. The owner decided: "on va delete toute la feature hdv en fait, c'est mieux à la main"; asked
what to keep, he chose to drop it all, the following of prices too. What is kept and how is the lead's. Builds D10, D172, D426.*

- **Dropped.** The marketplace's sessions HDV 1 to 6 made, on the branches `feat/market-protocol` to `feat/market-travel`, its
  protocol measured and named (`Game/market.proto`, 13 pins, mapping format 18), its following in the character's state, a ledger
  and a Market tab, simulated proposals and lot plans, then a bot that sold and lowered its lots alone through one guard, and a
  page of the editor. They are recorded in D427 to D438, which live on those branches only: none is merged, and this branch starts
  from `refactor/d3b-bis-namespaces` without them. The numbers are not used again.
- **Kept.** What those sessions fixed outside the marketplace, replayed on this branch without their marketplace lines: the six
  unsteady tests and their causes (`TestClock`, the report's progress, the harvest view's dispatcher, the frame recorder's order),
  the stop race of `BotActor.DisposeAsync`, and the stopped activity that ended `Failed` (status, "Known debt").
- **Made easier, the client's updates.** The mapping's format stays 17, the one D3B's releases ship: when Ankama updates the
  client, `scripts/embed.cs --client-version <version> --output "%LOCALAPPDATA%\D3B.Bis\data" --repository AerafalDev/D3B` takes
  the new build's pack, schema and mapping from D3B's release `client-<version>`, with no mapping made here. Format 18 would have
  needed a mapping of each build made here, pins carried over by hand.
- **Left in the owner's database.** The trials applied the marketplace's migrations to `%LOCALAPPDATA%\D3B.Bis\d3b.db`: its tables
  and their history rows stay, read by nothing. The migrations this code knows are applied as before.
- **Rejected.**
  - Keeping the following of prices alone, the Market tab read only: mapping format 18 at each client update, for prices the owner
    reads in the game.
  - Reverting the marketplace on top of the branches: a history of the feature and its undoing in `main`, for code never merged.
  - Deleting the branches on `origin` now: they hold the measures of D427, which a later wish for the marketplace would read
    first. The owner may delete them; nothing here needs them.
