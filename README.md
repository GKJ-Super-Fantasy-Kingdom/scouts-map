# Scout’s Map

Scout’s Map is a BepInEx navigation mod for Super Fantasy Kingdom. See the [package README](thunderstore/README.md) for controls and the optional marker settings. Version 0.3.0 is a local review build.

## Discovery and availability

| Destination | Native signal |
| --- | --- |
| Road, city, and water expansion | Existing road arrow and remaining road parts. Uses the game’s day, glory, kingdom level, stars, and affordability checks. |
| Shops | Registered building on an uncovered tile with a visible sprite. Uses the visible setup countdown and interactability. Sold-out or closed shops are omitted. |
| Chests, hermits, world rewards, spells, traps, offers, drops, and recruits | The stock world marker announces the encounter. The target must remain active. Prices, spell slots, and roster restrictions follow the game’s current rules. |
| Monsters to provoke | A native provocation encounter on an uncovered tile. The badge checks the native work hours, pending provocation, boss and glory rush gates. |
| Hero encounters | The game’s marker or the revealed hero’s active position. Remaining kills distinguish a quest in progress from an unlock ready to claim. |
| Quest sites and boards | An uncovered, interactable site or a native marker, with a finished objective and a reward still to claim. |
| Pirate returns and Wonder rewards | A returned pirate expedition or the Wonder’s reward-ready indicator. The badge clears once the reward is collected. |
| Witch bonfires, hospitals, and trades | A revealed, interactable site. Star, eligible unit, and resource checks determine whether to lock the badge. |
| Optional rare finds, coastal salvage, and cages | Stock pickup markers for rare finds and selected salvage. Cages require a revealed site and the game’s hint or achievement-ready indicator. |

The mod uses stock arrows to discover transient encounters and tracks each encounter while the target remains eligible. On-screen markers sit centered above targets when there is space inside the HUD-safe frame. Off-screen destinations and targets behind the HUD use directional badges. When markers would overlap, the marker with placement priority is shown.

Routine resources, decorative objects, undiscovered content, and native villain portraits keep their stock UI.

## Build and import locally

Use the installed Windows game and a BepInEx 5 profile. The project defaults to the D: Steam library and the Daybook Dev Thunderstore profile. Override these paths with `-p:GameDir=...` and `-p:ProfileDir=...` as needed.

```powershell
dotnet build ScoutsMap.csproj -c Release
dotnet run --project checks/LayoutChecks.csproj -c Release
./checks/CheckNativeFields.ps1
python package.py
```

Import `dist/ScoutsMap-0.3.0.zip` as a local mod in Thunderstore Mod Manager. The archive contains the plugin and package metadata. The build references game and loader DLLs from the installed dependencies.
