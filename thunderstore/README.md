# Scout’s Map

Scout’s Map adds illustrated markers for discovered destinations and encounters. Hover to read the destination name, price, or current requirement. Left-click an available marker to center the camera. Use the game’s interaction at the destination to open, buy, recruit, or claim.

A marker sits directly above its visible target when the HUD leaves room. An off-screen target uses a directional marker. Shops and buildings use their own sprites where available, and frame shapes distinguish routes, shops, sites, people, loot, magic, and monsters.

A dark badge with a visible lock indicates a temporary block. Shop setup timers, unaffordable offers, unavailable spell slots, and unfinished hero requirements can all produce locked markers. The position of an undiscovered static destination stays hidden.

## Markers

- **Expansions and shops** include roads, city expansion, water access, and visible shops.
- **Open quests and turn-ins** include discovered quest sites, dungeons, schools, and quest boards. Hover an open quest to check its progress.
- **Restoration** includes damaged kingdom buildings, unopened starting-resource sites, resource plots, and occupied camps. Fixed-cost repairs show their price or requirement.
- **World encounters** include chests, hermits, rewards, spells, captured monsters, relic drops, item and relic offers, unit rewards, recruits, and hero encounters.
- **Monsters to provoke** show the monster’s portrait and level. Locked markers show the current provocation restriction.
- **Services and markets** include the Black Market, Trading Post, Witch bonfires, hospitals, guest callers, and the Undead Alchemist.
- **Expeditions and rewards** include pirate expeditions and returns, and Wonder purchases and rewards.

Set a category to `false` under `[Markers]` in `BepInEx/config/gkj.scoutsmap.cfg` to hide its Scout badges. Changes made through a BepInEx configuration UI take effect when applied. Restart the game after editing the file by hand.

The enabled categories are `Expansions`, `Shops`, `Trades`, `OpenQuests`, `QuestTurnIns`, `Restoration`, `Services`, `Treasure`, `Encounters`, `Monsters`, `Recruiting`, `Magic`, and `Rewards`. `RareFinds` and `UnitCages` are off by default.

Markers use the game’s safe frame and scale. In gamepad mode they remain visible and the game keeps ownership of controller focus. Scout’s Map stores no save data.
