# Scout’s Map

Scout’s Map adds illustrated markers for discovered destinations and encounters. Hover to read the destination name, price, or current requirement. Left-click an available marker to center the camera. Use the game’s interaction at the destination to open, buy, recruit, or claim.

A marker sits directly above its visible target when the HUD leaves room. An off-screen target uses a directional marker. Shops and buildings use their own sprites where available, and frame shapes distinguish routes, shops, sites, people, loot, magic, and monsters.

A dark badge with a visible lock indicates a temporary block. Shop setup timers, unaffordable offers, unavailable spell slots, and unfinished hero requirements can all produce locked markers. The position of an undiscovered static destination stays hidden.

## Markers

- **Expansions and shops** include roads, city expansion, water access, and visible shops.
- **World encounters** include chests, hermits, rewards, spells, captured monsters, relic drops, item and relic offers, unit rewards, recruits, and hero encounters.
- **Monsters to provoke** show the monster’s portrait and level. Locked markers show the current provocation restriction.
- **Ready to collect** includes quest and board turn-ins, pirate returns, and Wonder rewards.
- **Services** include Witch bonfires, hospitals, and resource trade sites, such as the Undead Alchemist.

Optional markers can be enabled in `BepInEx/config/gkj.scoutsmap.cfg` under `[Markers]`. Set `RareFinds = true` for XP apples, shard finds, Super Metal, and high-value salvage. Set `UnitCages = true` for discovered achievement-based unit cages. Restart the game after changing either setting.

Markers use the game’s safe frame and scale. In gamepad mode they remain visible and the game keeps ownership of controller focus. Scout’s Map stores no save data.
