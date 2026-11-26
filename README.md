# Scout’s Map

Scout’s Map is a BepInEx navigation mod for Super Fantasy Kingdom. See the [package README](thunderstore/README.md) for controls and marker behavior. Version 0.1.1 is a local review build.

## First build coverage

| Destination | Native discovery and lifecycle | Availability |
| --- | --- | --- |
| Road, city or water expansion | Uses an existing `UIArrowRoad` and its registered road. The road must have parts left and its arrow must be active in the current mode. | Checks day, glory, kingdom level, stars and affordability. Where the game supplies a localized failure message, the tooltip uses it. |
| Shops | Uses native registered `BuildingShop` and `BuildingObjectShop` targets. Requires an uncovered tile and a visible native sprite. | A visible setup countdown shows a locked marker. An interactable shop with an enabled collider shows an available marker. Other closed or sold-out shops are omitted. |
| Treasure, hermits and world rewards | Enhances native, untyped `UIMarker` arrows for `DropChest`, `WorldHermit` and `WorldRewardGiver`. | Shows the badge while the target is interactable and its native marker exists. |

Some native event arrows disappear as soon as the player hovers over the destination. Scout’s Map follows that lifetime. When a destination is near the center or there is no clear space in the marker frame, the Scout’s Map badge is hidden.

Other events retain their original behavior. General road-locked buildings, specialized NPC calls and offers, quest steps, voyages without a native road arrow, and other collectible types need their own discovery and eligibility checks.

## Build and import locally

Use the installed Windows game and a BepInEx 5 profile. The project defaults to the D: Steam library and the Daybook Dev Thunderstore profile. Override these paths with `-p:GameDir=...` and `-p:ProfileDir=...` as needed.

```powershell
dotnet build ScoutsMap.csproj -c Release
dotnet run --project checks/LayoutChecks.csproj -c Release
python package.py
```

Import `dist/ScoutsMap-0.1.1.zip` as a local mod in Thunderstore Mod Manager. The archive contains the plugin and package metadata. The build references game and loader DLLs from the installed dependencies.
