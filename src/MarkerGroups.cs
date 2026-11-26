using System;
using BepInEx.Configuration;

namespace ScoutsMap
{
    internal enum MarkerGroup
    {
        Expansions, Shops, Trades, OpenQuests, QuestTurnIns, Restoration, Services, Treasure,
        Encounters, Monsters, Recruiting, Magic, Rewards, RareFinds, UnitCages
    }

    internal sealed class MarkerGroups
    {
        private readonly ConfigEntry<bool>[] entries = new ConfigEntry<bool>[Enum.GetValues(typeof(MarkerGroup)).Length];

        internal MarkerGroups(ConfigFile config)
        {
            Bind(MarkerGroup.Expansions, "Road, city and water expansions.");
            Bind(MarkerGroup.Shops, "Markets, guild shops and merchant buildings.");
            Bind(MarkerGroup.Trades, "Trading Posts and resource exchanges.");
            Bind(MarkerGroup.OpenQuests, "Discovered unfinished quests and quest boards with open objectives.");
            Bind(MarkerGroup.QuestTurnIns, "Quest sites and quest boards with an objective ready to turn in.");
            Bind(MarkerGroup.Restoration, "Restorable plots, starting-resource sites and camps.");
            Bind(MarkerGroup.Services, "Guest callers, Witch bonfires, hospitals and other world services.");
            Bind(MarkerGroup.Treasure, "Treasure chests and item offers.");
            Bind(MarkerGroup.Encounters, "Hermits and capturable monsters.");
            Bind(MarkerGroup.Monsters, "Monsters available to provoke or temporarily blocked.");
            Bind(MarkerGroup.Recruiting, "Recruit, unit reward and hero encounters.");
            Bind(MarkerGroup.Magic, "Spells and relics.");
            Bind(MarkerGroup.Rewards, "World rewards, pirate expeditions and Wonders.");
            Bind(MarkerGroup.RareFinds, "XP apples, shards, Super Metal and high-value salvage.", false);
            Bind(MarkerGroup.UnitCages, "Discovered achievement-based unit cages.", false);

            void Bind(MarkerGroup group, string description, bool enabled = true) =>
                entries[(int)group] = config.Bind("Markers", group.ToString(), enabled, description);
        }

        internal bool Enabled(MarkerGroup group) => entries[(int)group].Value;

        internal static MarkerGroup Get(MarkerIcon icon, bool questTurnIn)
        {
            switch (icon)
            {
                case MarkerIcon.Road: return MarkerGroup.Expansions;
                case MarkerIcon.Shop: return MarkerGroup.Shops;
                case MarkerIcon.Trade:
                case MarkerIcon.TradingPost: return MarkerGroup.Trades;
                case MarkerIcon.Quest: return questTurnIn ? MarkerGroup.QuestTurnIns : MarkerGroup.OpenQuests;
                case MarkerIcon.Restore: return MarkerGroup.Restoration;
                case MarkerIcon.Treasure:
                case MarkerIcon.Item: return MarkerGroup.Treasure;
                case MarkerIcon.Hermit:
                case MarkerIcon.Capture: return MarkerGroup.Encounters;
                case MarkerIcon.Provoke: return MarkerGroup.Monsters;
                case MarkerIcon.Recruit:
                case MarkerIcon.Hero: return MarkerGroup.Recruiting;
                case MarkerIcon.Spell:
                case MarkerIcon.Relic: return MarkerGroup.Magic;
                case MarkerIcon.Reward:
                case MarkerIcon.Pirate:
                case MarkerIcon.Wonder: return MarkerGroup.Rewards;
                case MarkerIcon.Apple:
                case MarkerIcon.Rare:
                case MarkerIcon.Salvage: return MarkerGroup.RareFinds;
                case MarkerIcon.Cage: return MarkerGroup.UnitCages;
                default: return MarkerGroup.Services;
            }
        }
    }
}
