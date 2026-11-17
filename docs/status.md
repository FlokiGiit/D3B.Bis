# Status

Where the project stands, on one page. Read it first: it says what exists, what is not measured, what comes next and what is
owed. The reasons are in [the decisions](decisions/README.md), found through [decisions-index.md](decisions-index.md); the steps and
their sessions are in [roadmap.md](roadmap.md); how the code is built is in [architecture.md](architecture.md). Whoever closes
a session updates this page, the roadmap and the index.

As of 2026-11-17, after D442 (the repository public, released as v0.10.0), D441 (the app ships no client's files: its release is the app alone, which prepares them on its first start), D440 (the app prepares the installed client's pack, schema and mapping itself when Ankama updates the client, from `src/D3B.Bis.Extraction`, which the tools now wrap; it downloads nothing), D439 (the owner drops the marketplace: its work, D427 to D438, stays on branches never merged; the unsteady tests, the bot's stop race and the stopped activity they found are fixed here), D426 (D3B.Bis continues D3B in a repository of its own: projects and namespaces `D3B.Bis.*`, files in `%LOCALAPPDATA%\D3B.Bis`, D3B's formats and published packs kept), D425 (the harvest's tour goes into another zone of a map, out and back through the world graph), D424 (a resource in another zone of its map is out of reach, no longer used from afar), D421 (7r6, a step of a list repeats and plays another list), D420 (the Pandawa no longer swaps Sobre and Saoul instead of striking), D419 (7r4, following a run), D418 (7r5b, the profile as an asset editor), D415 (7r5 part 1, the zones read by the harvest and the hunt), D417 (the harvest's tour weighs the character's level) and D416 (it leaves the houses of a town alone), D414 (hints on the world map), D413 (7r3, the Sequencer) and D408 (the fight AI after the players' reports, D407, summons a last resort; the home page, D406; banking by general settings, D405; D394, session 7d4, merged into 7r2's branch). App 0.8.1, client build 3.7.13.17, pack format 11 published, 12 extracted and not published (D414), mapping format 17.

## What exists

- **A bot that plays through the official client** (D23): the relay between the client and Ankama's servers, the readable
  protocol and its per-build mapping, the data pack extracted from the client (steps 1 to 3).
- **The run time** (step 4): one bot per character, walking, changing maps, using elements, banking, zaaps and the haven bag,
  activities, recovery.
- **Harvesting and the hunt** (steps 6 and 9): both choose their place by the character's levels and go through several
  sub-areas in turn (D391, D392); fights are played by one AI for every breed (D244), with the rules and effects of a fight
  measured on the owner's captures.
- **The desktop app as a studio** (step 7, up to 7d4, 7r2 and 7r3): dark, docked, seen as a game engine (D397: the world's map is the
  centre once a route or a zone is selected, with the characters, routes and zones on it, an Outliner and a Details panel; the studio opens on a home page, D406), the project's assets in files,
  behaviors and profiles as variants, the world's map drawn from the client's images, routes with their actions, a profile
  playing a list of routes and steps made in the app (D386), zones painted on the same map (D394), Undo and Redo in the route, the
  list of steps and the profile editors, and rows dragged or moved by the keys (D394). A route's steps are set in the Sequencer
  under the world (D413): a ruler of steps, a track a kind of action, a clip drawn across the steps it covers and stretched, moved,
  copied, deleted by the pointer or the keys, several steps selected at once, the Details setting a clip's values once for all its steps.
  The world's map also draws the client's own hints, zaaps, dungeons, workshops, banks and more, with a tick for each category (D414).
  A profile opens as an asset editor (D418): four categories, the rare settings folded under "Advanced · N changed" with an amber dot, the world's
  map beside the form to pick the zone or the sub-areas of its place.
- **A versioned Claude Code setup** (D398): `.claude/` holds C# hooks that ask before a push or a `gh` write and refuse
  what the client produces, skills for C#, the studio's UI, tests, Git and the rest, and two agents. The hooks are tested in
  `tests/D3B.Bis.Hooks.Tests`; the write guard and the Git guard were seen refusing inside a session, the "ask" answers and the
  Stop hook's block not yet.
- **The client's files made by the app** (D440): when the host lacks the installed build's pack, schema or mapping, the home page's
  Update extracts the pack, reads the schema and makes the mapping from the pins of the latest committed build, without captures, in
  the app's process, shows the step and its progress, then offers the restart; a mapping that no longer holds turns the banner red,
  "the game's protocol changed", with what failed in `preparation.log`. The tools `extract.cs`, `schema.cs` and `mapping.cs` are
  command lines over the same library, their files the same byte for byte on 3.7.14.18.
- **The app ships no client's files** (D441): the release workflow packs the app alone; the home page, the status bar and the About section
  name the build the host read. No release has been made since: the next tag `v<version>` is the first without data.
- **Published**: the installer and updates through Velopack (D171); the client build's pack, schema and mapping as the release
  `client-3.7.13.17` (D365).

## Not measured in game

- The client's files made by the app (D440) were seen on the owner's machine on 2026-11-17, the app built from its code and pointed at an empty
  `Data:Directory`: the banner named 3.7.14.18, Update showed the three steps and their share, the button hidden meanwhile, and in about 55 s
  the pack, schema and mapping were in place, the same bytes as the tools', `preparation.log` beside them, the working set peaking at 3.47 GB
  and back to 740 MB once collected. The owner pressed Restart; what the app did after it was not looked at. Not seen: a mapping that no
  longer holds (red banner) or a client misread, except in headless windows; an installed app, whose installer CI has not yet built with the
  library.
- D3B.Bis (D426) was built, tested and started on the owner's machine while D3B 0.9.1 ran: the window opened, the pack of format 12 and the
  mapping of format 17 fetched from D3B's release were read, D3B's database, project and preferences came across, and the host stopped at the
  relay's address, which D3B held. Not done: a session through a client with D3B closed, an installer packed and installed beside D3B's, an
  update, a report sent. The repository is `FlokiGiit/D3B.Bis` (`ClientRelease.DefaultRepository`), public since D442, its first release
  `v0.10.0`, which ships no client's files (D441); it has no `client-<version>` release, so the tools that read one take D3B's with
  `--repository AerafalDev/D3B`. The report channel's asset is not published yet.

- The harvest and the zones of a map (D424): a player's report ("it tries but cannot, unless it stands in front of the resource or comes back from
  another side"), read in the code and measured on the pack (864 maps with more than one zone, 3,673 element and zone pairs on 607 maps), fixed and
  tested on the fake game, not seen in game: that the server refuses a use from a cell two steps away, and that the tour no longer fails on those
  maps. To look at in the client: maps 0, 3333, 3845, 133890, 134403 or 134404, a character in one zone and a resource beside the other's cells.
  D425 (the tour going to the other zone through the world graph, out and back by another map, once per tour) is tested on fake graphs and a fake
  game with a map of two zones, not seen in game. To look at, on the same maps: that the character leaves the map and comes back into the zone of the
  resource (the landing cell's zone is the graph's `toZone`, measured only on the pack), that the vertex of the other zone is the one the character
  stands in once there, the number of map changes the way takes (the cap of six is the lead's reckoning), and the debug lines "the tour will go to
  zone" and "leaves zone ... alone" in the log. A map whose other zone no way leads into is left alone, so is one the graph does not hold.

- The sending (BR4, D410) was run for real from a throwaway tool with the production sink: posts of 0.1 to 20 MiB of random bytes were taken by the owner's
  webhook and 21.5 MiB and more refused (the limit is Discord's, depends on the server's level and may change: a refusal is a result, and the app says to hand
  the file over); a cleaned report of a real capture came back from Discord byte for byte and read by `ReportReader`, with no mention made of an `@everyone` in its
  message; every post was deleted. Not done: a send from the app's dialog, the asset published and read from GitHub, a rate limit (the webhook's own limits were
  not reached), a webhook revoked.
- The report's dialog (BR3, D410, D411) is tested on view models, with captures the tests make, and in a headless window drawn in both languages
  (form, preview, written), and, since the fix after release v0.9.0, through the real `MainWindow` (Help, Report a bug, form, Preview). v0.9.0's Help item opened
  nothing: the window's switch over its dialogs had no case for the report. Now one table (`DialogKind`) registers and shows each dialog, and a test
  fails when a dialog view model (`IDialogViewModel`) has no row. The dialog sends with one button from the preview, and offers the code and the folder only when the send fails; it shows each character's portrait. The fix reaches players with the next release. Not opened by the owner from the Help menu, not tried on the capture of a real session through a running host
  (which bot, which connection, which capture, the profile and the assets it reaches), and its look is not judged against a mockup.
- The cleaner (BR2, D423) was run on 48 of the owner's real captures of 3.7.13.17: 45 were cleaned with no ticket, secret or name left in any
  output and 3 were refused for want of a character list the translation reads (the tool fails closed); on one of the largest, whose fights `scripts/fights.cs` replays with the same result as the original. What it leaves is said in D421
  (other players' names, numbers, timestamps, the GUIDs of unknown meaning, a name under 6 bytes in a frame too deep or cut short to walk); a
  next build's own opaque strings are found by shape only if they keep it, and the copy is searched for every secret it found before it is returned.
  The report is not yet made by the app, so no report was sent anywhere. `scripts/report.cs --replay` ran on a report of a real capture.
- Recording by default and its retention (BR1, D409) are tested on temporary files, the relay of the tests and view models, and the settings
  and the home page's notice in headless windows, not seen by the owner in the app and not run on a client. The first start of the host after
  the change deletes the game's captures older than 7 days or beyond 1 GB: the owner's 153 captures, which `scripts/fights.cs` replays, would
  go, so he sets `Network:Captures:Retention:Days` to 0 (or copies `captures\game`) before it. The 7 days and the 1 GB are D409's, the lead's reckoning.
- Repeats and sequences as steps (D421, 7r6) are tried on the fake game, view models and headless windows, not on a client and not by the owner
  in the app: a route followed three times (the journal saying its steps in the first pass only), a loop followed until pods at least 90 % (a lap
  long enough overshoots, the condition being read between passes), a list played inside a list with a fight in the middle of its second step, a
  block that holds a route, the flyout of the repeat token and of the condition (the headless window opens no popup), and how the tokens read under
  each step. The list is in D421; it joins the measure owed below.
- Following a run (D419, 7r4) is tried on the fake game, view models and headless windows, not on a client: whether the position told is the
  position lived with real travel (zaaps, `Busy` waits, a map with no path), a fight or a bank cutting the route and the run resuming on the
  same step, a long action while the playhead stays on its step, two accounts on one route, 250 ms against a map every three seconds. The
  list to look at, one by one, is in D419; it joins the measure already owed below.
- The hints on the map (D414) were seen only as headless renders of the real pack and tiles at four zooms, not in the app by the owner: how
  the pictures read, whether the thresholds and priorities (the lead's reckoning) thin them as they wish, the name over the hovered one. The
  pack of format 12 is extracted on the development machine and not published: the app of this change needs it (`scripts/release.cs --publish`,
  the owner's go).
- The zones as a place (D415) are tested on the fake game and in the view models, not in game and not seen by the owner in the app: a harvest or a hunt through a zone's maps, the field that picks it and the words under it, and what the French "Région" (the game's area, once "Zone") reads like to them.
- The profile as an asset editor (D418) is tested on view models and headless windows, not seen by the owner in the app: how the four categories and the "Advanced" folds read (the marks of each rare setting are the lead's application of D415's list), the map beside the form (its tiles were not drawn in tests: the source was empty), a click on the map against a drag, and the narrow band. A sub-area clicked outside the region chosen only raises the field's "not within" problem.
- The Sequencer (D413) is tested on view models and in the headless window, not yet seen by the owner in the app: how a drag feels
  on a long ruler, how the tracks read at the bottom panel's height, whether it is what they mean by practical; no screen reader was
  run on its grid.

- The routes' actions that talk to the game and the profiles playing a list of steps (7d3c, 7d3d): tried on the test game
  only; a real client with the owner is to do.
- The home page (D406) is tested on view models and a headless window, not yet seen by the owner in the app: its look and whether the six recent assets and the characters are what they expect.
- 7d4's editors (undo, dragging, the zones' tools) are tested on view models and headless windows, not yet by the owner's hand
  in the app: how a drag feels, the focus after a move by the keys, the zones painted at several zooms.
- The owner's reports from game answered since 2026-11-06 (D387 to D392): the hunt attacking every group, the tour leaving
  interiors out, the AI fleeing less, the rotation through ten sub-areas and the professions in turn. The numbers 5, 10, a
  quarter and 50 of D391 and D392 are the lead's reckoning.
- Banking by the general settings (D405): the share of pods and the zaap rule are tested on the fake game and in the app's
  view models. A bank journey that outlives a refused zaap is the lead's reading of a player's report (Incarnam to Astrub, 1,500
  kamas the character lacked), not seen in game; the text the server answers to a zaap that cannot be paid, and the cost of a
  zaap leaving Incarnam's world map, are not measured.
- The harvest leaving the houses of a town alone (D416) and weighing the character's level (D417): a player's report ("I still go into
  all the houses in Astrub"), read in the code and measured on the pack and the owner's learned graphics, tested on the fake game, not
  seen in game. A mine whose graphics no bot has learned is no longer discovered by a tour; an outdoor map is never left out for the
  level, only gone to later.
- The players' reports answered by D407: the new curve of the character's life (above 60% a point is worth a quarter) and the
  plan with the fewest actions among equals, replayed on the captures but not played in game; what the server sends of an
  invisible fighter (no capture holds one: the AI keeps its last cell and `Combatant.IsVisible` is never unset); the damage of a
  long push against a wall (the captures hold pushes of 1 to 4 cells, the pack's *Panache DoPou* pushes 10); the players'
  *Chafer* and *Arakne* summons, which no breed of the pack casts.
- The Pandawa swapping Sobre and Saoul with Bombance or Picole instead of striking (D420): a player's report, reproduced on the imagined fight of the pack's spells and fixed in the judge (a state held as the search began is no gain), not seen in game: no capture holds a Pandawa casting either, so what the server sends back of the swap (the two states, the AP given back, state 1) is read in the pack. The player is to say whether it strikes now.
- Step 9's mechanisms: the Roublard against big enemies with `Fights:Alone`, the Féca's glyphs, an Osamodas's summons and the
  fight map's preview, an aggression, stairs and portals, the inactivity popup, what the client shows once its turn's end goes
  unanswered (D367).

## Next

0. **The client's files made by the app** (D440) are on `feat/client-update-in-app`, not delivered: its pull request waits for the owner's go.
0. **The harvest going to another zone of its map** (D424 (b), D425) is made in code and tests: to look at in the client with the maps listed above.
0. **The refactoring and tidy-up session** (D399 to D401) is mostly done: `decisions.md` is cut in files by group of the index,
   `client.md` has its game connection apart, `AssetsViewModel`, `BotActor`, `HarvestActivity`, `WorldMapView` and `Imagination`
   are under 900 lines, BenchmarkDotNet is gone. The two files that stayed over 900 lines, `FightJudge` and `RouteEditorViewModel`, are cut too.
0. **Bug reports with the player's capture** (D409 to D411, D422, D423): BR1 and BR2 are made. The game connection is recorded by default
   with its retention and the notice (`Network:Captures:Game`, `CaptureRetention`); what names the account in a capture was measured
   ([client-game.md](client-game.md#the-game-what-names-the-account)) and the cleaner hides it by value (`CaptureScrubber`, D423); a report is
   written and read as a zip with a manifest (`ReportWriter`, `ReportReader`) and `scripts/report.cs` unpacks one. BR3 is made: Help, Report a
   bug writes the file with its code, and BR4 is made: a click sends it to a private Discord webhook (`DiscordReportSink`), read from the asset
   `webhook.txt` of the release `report-channel`, which `scripts/release.cs --report-webhook` publishes (not yet published: the owner's go).
   What is left is to see it in the app and to publish the asset.
1. **7r4, following a run, and 7r6, repeats and sequences as steps, are made in code** (D419, D421), to try in game with the owner's lists; then 7e.
   7r6 left: the condition read between two steps of a route, and, or and not in an "until" (7e's), a pass of a repeated one-way route on the card,
   the lists that repeat a route shown on the route. What 7r3 and 7r4 leave: the map's frame, a panel taller for the Sequencer, the steps already
   passed dimmed on the map, and recording in the toolbar, which waits for the measure in game.
2. **A measure in game with the owner**, before anything else of step 7: the list above, and what 7d4b will record.
3. **Session 7d4b**: recording while playing and Simplify (D299, D394), once the events it reads are measured.
4. **7e and 7e2**: the canvas of behaviors, then 7f to 7i (nodes and planning, debugging, the AI's fight preferences, sharing).
   The breeds, set aside since 9f5, come back once these are done (D306).
5. **Then** step 9's remaining mechanisms, and steps 10 to 13 (several accounts, crafting, the marketplace dropped by the owner (D439), treasure hunts and
   dungeons, the owner's profession scripts).

The measure in game is worth a session of its own before 7e grows the code further.

## Open questions

Q5 (the fight formulas left: shields, AP and MP removed, erosion), Q12 (what quests, breed and the server's codes gate),
Q16 (casts and walks the server refuses that the rules allow), Q17 (a build that changes its schema). The table is at the end of
[the open questions](decisions/open-questions.md#open-questions).

## Known debt

- **The readers of the client have no tests** (D440): moved from `scripts/` to `src/D3B.Bis.Extraction` (about 7,000 lines), they read
  only a real client, which neither the tests nor CI have. A change to them is checked by running the tools on the installed client and
  comparing the pack's, schema's and mapping's SHA-256 with the previous code's. The row of `capstone.dll` in `THIRD-PARTY-NOTICES.md`
  names its authors and license without a notice to check them against: the package ships none.

- The tests were not steady on the owner's machine (seen 2026-11-13 with the game and D3B running, on D3B's binaries of `df9a242` as on
  D3B.Bis's, so it predates D426). Diagnosed on 2026-11-13 and fixed the day after (branch `fix/steady-tests`, 2,375 tests): each defect
  was reproduced in a loop and its cause shown by a probe, since removed. Six tests had four causes, each in the test and fixed there,
  `src/` unchanged; a fifth cause is the product's, fixed the same day on the branch `fix/bot-stop-race` (the last item). After the
  fixes `dotnet test` was green ten runs in ten in the foreground.
  - **A fake clock moved before the wait it should end is asked.** `FakeTimeProvider.Advance` rings the timers already made; one made
    after is due later and never rings. `BehaviorActivityTests.AListThatRepeatsReportsItsFirstStepAgainWhenItStartsOver` advanced once
    the run said `Stepping`, which `ActivityRun` says before the step asks for its wait (red 8 runs in 20 of the test alone).
    `FollowRouteTests.ACharacterIsFollowingItsRouteOnlyWhileTheRoutePlaysInTheListOfSteps` did it on the route's position, then awaited
    the run's end with no limit: it was the process of `D3B.Bis.Studio.Tests` that never ended.
    `BotDelaysTests.ARequestOnANewMapWaitsTheDisplayTimeChosenForTheBotsAlreadyMade` advanced while `BotContext.SendAsync` was between
    reading the clock and making its timer. Fixed: `TestPlay.Time` is a `TestClock`, a fake clock that knows the timers asked of it; a
    test ends a wait with `EndAsync`, or waits for it with `AskedAsync` before a smaller move (the list's test: green 40 runs in 40).
    Converted with the three, for the same pattern: the eight other advances of `BehaviorActivityTests` made on a note or a status,
    `MapWalkTests.AWalkOnANewMapWaitsUntilTheClientDrewIt`, `BotDelaysTests.ADisplayTimeRaisedWhileARequestWaitsHoldsItLonger`,
    `ActivityRunTests.TheNextStepWaitsTheTimeBetweenTwoActions` and `AFightBegunBetweenTwoStepsIsTheActivitysAsOneThatCutAStep`.
  - **A value written from the thread pool and read at once.**
    `ReportSendTests.AReportSentSaysSoWithItsCodeCannotBeSentTwiceAndCarriesTheMessageAndTheManifest` ran with no synchronization
    context, so the dialog's `Progress<long>` posted `SendProgress` to the pool and the test read 0 (red in 15 full runs in 15 of
    `D3B.Bis.App.Tests`). Fixed: the class carries `[UiThread]` and the test waits for the posted value before it asserts it, the
    assertion unchanged (green 12 full runs in 12).
  - **An async lambda given to the headless session's synchronous `Dispatch`.**
    `HarvestViewsTests.AGivenProfileIsReadOnlyAndItsCopyIsTheProjectsOwnWithItsValues`: Avalonia.Headless shuts the test's dispatcher
    down as the lambda returns its pending task, and a dispatcher shut down drops what is posted to it. When the save's write ended
    asynchronously its continuation was lost: it was the process of `D3B.Bis.App.Tests` that never ended (2 full runs in 11). Fixed:
    the file has the `Func<Task<T>>` helper, which pumps the dispatcher until the task ends, as five other test files have.
  - **An order the product does not promise.** `FrameRecorderTests.ABotsFramesAreRecordedFromItsConnectionsFirstFrameBothSidesAndD3BsOwn`
    answered as the server as soon as the frame was on the fake wire, and `GameSession.SendAsync` tells the observers once the frame
    went (D115): the answer was recorded first (4 runs in 20 under load). Fixed: the test answers once D3B's frame is recorded; D115
    is unchanged.
  - **Advances of a fake clock not converted, on purpose.** The loops that move the clock until what they wait for happened ring a
    timer made late at their next turn: `TestWalks.SentAsync` and `AdvanceUntilDoneAsync`, `MapWalkTests.AdvanceUntilDoneAsync`,
    `FightPlayTests.SentAsync`, `BehaviorSequencesTests.AdvanceUntilAsync`, and the loops of `WorldTravelTests`, `HarvestActivityTests`,
    `HarvestActivityZonesTests`, `FollowRouteTests.ALoopFollowedByAStepThatRepeatsPlaysOneLapForEachPassThenTheNextStep` and
    `BehaviorActivityTests` (two, each after a converted advance). The advances after which a test asserts that nothing happened stay
    true with a timer made late, for a weaker reason: `ElementUseTests.AHarvestWalksNextToTheElementThenUsesItUntilItsEnd`,
    `FightPlayTests.OutOfAnActivityTheBotLeavesTheFightToThePlayer` and `ThePlayersCastPausesTheBotAndTheFightIsThePlayers`, and the
    advance made while the bot is paused in `BehaviorActivityTests.AnActionAnInterruptionCutIsPlayedAgainButNotWhatWasPlayedBefore`.
    `BotJournalTests.AWalkAskedForItselfIsWrittenOnceOverWithHowItEndedAndHowLongItTook` moves the clock to time a walk, and no wait
    ends on it; `BotActorTests.AnExpectationEndsAfterItsDelayByTheBotsClock` asks its wait before it signals. `TestPlay.EnterAsync`
    moves the clock by the display time once the bot took the map in, for every test that enters a map: it ends no wait, but a bot
    whose behavior already waited to send would make its timer late; no test has one today. `TestClock` tells a wait by when it
    ends alone, so two waits that end at the same instant are one to a test; no converted test has two. The other clocks have no bot
    waiting on them (regrowths, the NPC learner, captures, storage, the app's views).
  - **A bot stopped while something asked to wait on it never ended: the product's, fixed on 2026-11-14 (`fix/bot-stop-race`).**
    `BotActor.DisposeAsync` posted `Stopping`, then completed the inbox: a wait or a start posted between the two was taken by the
    loop after `OnStopping` had failed the others, and nothing ended it. `FightPilot.RunAsync`, woken as a test's fight ends or
    pauses, asked its next wait there, and `Bots.DisposeAsync` awaited the pilot for ever: `FightPlayTests`'s
    `TheTurnsEndTheClientSendsByItselfIsWithheldAndTheAIPlaysOn`, `ThePlayersTurnsEndGoesToTheServerAndPausesTheBot` and
    `ThePlayersCastPausesTheBotAndTheFightIsThePlayers` never ended. `Bots` is the host's own (`BotsModule`): a host disposing its
    services went through the same code. Fixed: the stop is the inbox's completion alone; what was posted before is read, the loop
    ends the behavior, the waits and the parked starts as it ends, and a post made after is refused (architecture, "The actor"; D9
    unchanged). `BotActorTests.AWaitAskedWhileTheBotStopsEndsWithIt` and `AStartAskedWhileTheBotStopsEndsWithIt` race a thread's
    asks against 100 stops, no delay added: on the old code an ask fell between the two statements at 40 to 58 stops of 100 on an
    idle machine (20 runs) and 39 to 56 under load, so both were red at every run; green 20 in 20 with the fix. They race real
    threads: forcing the order would need a seam between two statements the fixed code no longer has. Not measured on a runner
    of two cores. Under load, the executable under `timeout`
    beside 12 busy loops: the old code hung 1 run in 55, the fix 0 in 50; beside 24 busy loops the old code hung 0 in 55. The
    rate depends on the load, and the earlier 3 in 55 was not met again: those runs bound the race loosely, the two tests are
    the proof. `ActivityRunTests.TheBotStoppingEndsTheActivityWhetherItStepsOrWaits`, seen never ending once, is the same race in
    its waiting case: the test stops the bot once the run says `Waiting`, which the run says before it posts its start; a probe
    that put that start after `Stopping` left the run never ending on the old code. Not seen in a loop.
  - **A wait the stop ends was not taken for a stop by an activity's run: found by the review of `fix/bot-stop-race`, fixed on
    2026-11-14 (`fix/stopped-activity`).** `BotActor` failed a wait with `ObjectDisposedException(nameof(BotActor), …)`, whose
    `ObjectName` is `BotActor`, and `ActivityRun` takes a stop for one only when `ObjectName` is `typeof(BotActor).FullName`, as
    `ObjectDisposedException.ThrowIf` names it. An activity that fights its fights, stopped while `FoughtAsync` waits for the fight's
    end, therefore ended `Failed` with the exception as its `Failure`, not `Stopped`. `BotActor` now names the stopped bot by its
    type's full name, as `ThrowIf(…, this)` does, for both the wait refused after the stop and the waits the stop ends.
    `ActivityRunTests.TheBotStoppingWhileTheActivityWaitsForItsFightsEndStopsTheActivity` shows it, seen `Failed` on the old code;
    `BotActorTests.AWaitEndsWithItsTokenOrTheBot` holds the name for both, seen red with the refused wait's old name put back.
    `ActivityRun`'s two filters are the only code that reads that name. A step's own wait was never hit, since `RunBehaviorAsync`
    turns what an interrupted behavior meets into `BotInterruptedException`.

  The loops ran the test executables with xunit's `-longRunning`, alone, every project at once as `dotnet test` does, and beside busy
  loops for the load. A `dotnet test` is run in the foreground: beside the Stop hook's `dotnet build`, a test process loses its
  assemblies.

- `D3B.Bis.Bot` is about 17,900 lines in 224 files and `D3B.Bis.Studio` about 6,300 in 141 (D393). The border is the compiler's, one way:
  `D3B.Bis.Studio` references `D3B.Bis.Bot`, which knows nothing of it. Left over: `D3B.Bis.Studio.Routes` reaches the harvest and the fight
  through `MapHarvest` and `MapFight`, now public, so a module of `D3B.Bis.Bot` cannot declare nodes (the node API is the studio's):
  a future module in a project of its own can. Nine members and types of `D3B.Bis.Bot` became public so that the studio could use them
  (`BotContext.Actor`, `BotActor.Time`, `Log`, `PauseAfterStep`, `SettingDefinition.TryRead`, `ActivitySettingsBuilder`'s
  constructor and `Build`, `MapHarvest`, `MapFight`): a narrower seam would be a task of its own.
- Files over 900 lines (D399): none any more. `FightJudge` (890, from 1,365: its pure turn arithmetic is `JudgeTurns`, its damage tables and landing caches `JudgeTables`, its odds `JudgeOdds`; its score's terms, threat, approach, marks and buffs stay in it, cut only with the replay of the captured fights, `scripts/fights.cs --turns`, compared before and after) and `RouteEditorViewModel` (886, from 1,238: its texts, map scene, step rows, action forms, undo history, path planning, typed anchor, trace and world menu are cut). `AssetsViewModel` (885), `Imagination` (864, its pure fighter helpers are `ImaginedFighters`), `WorldMapView` (848, its hit test is `WorldMapHitTesting`), `BotActor` (723) and `HarvestActivity` (709) are cut. `D3B.Bis.Bot` stays one project: its core (`Runtime`, `Activities`, `Banks`, `Movement`, `Journal`, `Characters`) refers to itself both ways, `Harvesting` and `Fighting` are the leaves.
- Undo has two histories: the route editor keeps its own snapshots (D375), the list of steps, the profile and the zone editors use
  `UndoHistory` (D394). Moving the route onto it is a change of no use by itself; do it when the route editor is next opened for another reason.
- The zones are read by the harvest and the hunt as a place (D415) and by their editor; a zone is not yet a filter of the Automatic place, and the nodes and conditions of 7f do not read them.
- `GroupJudge.OddsOf`, `Beats`, `Winnable` and `Share` are called by nothing in `src/` since D387, only by
  `scripts/fights/HuntBench.cs` and their tests. D387 kept them on purpose, for that bench: if the bench is dropped, they go
  with their tests; `Allows` and `Character` are alive.
- `docs/decisions/` is cut in files by group of the index, about 100 KB at most each (D401): read the index, then the file of the area worked on.
