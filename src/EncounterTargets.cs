using System;
using HarmonyLib;
using SuperFantasyKingdom;
using SuperFantasyKingdom.Clickable;
using SuperFantasyKingdom.Spawner;
using SuperFantasyKingdom.Spells;
using UnityEngine;
using UnityEngine.Localization;

namespace ScoutsMap
{
    internal static class EncounterTargets
    {
        private static readonly AccessTools.FieldRef<WorldSpell, SpellType> SpellId = Field<WorldSpell, SpellType>("m_SpellType");
        private static readonly AccessTools.FieldRef<WorldSpell, LocalizedString> SpellError = Field<WorldSpell, LocalizedString>("stringError");
        private static readonly AccessTools.FieldRef<WorldMonster, LocalizedString> ProvokeAnother = Field<WorldMonster, LocalizedString>("provokeAnother");
        private static readonly AccessTools.FieldRef<WorldMonster, LocalizedString> ProvokeImpossible = Field<WorldMonster, LocalizedString>("provokeImpossible");
        private static readonly AccessTools.FieldRef<WorldMonster, LocalizedString> ProvokeDay = Field<WorldMonster, LocalizedString>("provokeDay");
        private static readonly AccessTools.FieldRef<WorldMonster, LocalizedString> ProvokeRush = Field<WorldMonster, LocalizedString>("provokeRush");
        private static readonly AccessTools.FieldRef<WorldRelic, string> RelicOffer = Field<WorldRelic, string>("m_Relic");
        private static readonly AccessTools.FieldRef<WorldItem, string> ItemOffer = Field<WorldItem, string>("m_Relic");
        private static readonly AccessTools.FieldRef<DropRelic, string> RelicDrop = Field<DropRelic, string>("m_Identifier");
        private static readonly AccessTools.FieldRef<DropUnit, string> UnitDrop = Field<DropUnit, string>("m_Unit");
        private static readonly AccessTools.FieldRef<WorldUnit, string> UnitOffer = Field<WorldUnit, string>("m_UnitIdentifier");
        private static readonly AccessTools.FieldRef<WorldUnit, LocalizedString> UnitError = Field<WorldUnit, LocalizedString>("stringError");
        private static readonly AccessTools.FieldRef<WorldHero, string> HeroId = Field<WorldHero, string>("m_Hero");
        private static readonly AccessTools.FieldRef<WorldHero, int> HeroKills = Field<WorldHero, int>("m_Kills");
        private static readonly AccessTools.FieldRef<WorldHero, int> HeroRequired = Field<WorldHero, int>("m_Required");
        private static readonly AccessTools.FieldRef<WorldHero, LocalizedString> HeroError = Field<WorldHero, LocalizedString>("stringRequired");
        private static readonly AccessTools.FieldRef<Trap, string> Captured = Field<Trap, string>("m_Captured");
        private static readonly AccessTools.FieldRef<BuildingWorldPirates, int> PirateReturn = Field<BuildingWorldPirates, int>("m_IsPlundering");
        private static readonly AccessTools.FieldRef<BuildingWorldWonder, bool> WonderPending = Field<BuildingWorldWonder, bool>("m_SpawnTomorrow");
        private static readonly AccessTools.FieldRef<BuildingWorldWonder, GameObject> WonderReady = Field<BuildingWorldWonder, GameObject>("question");
        private static readonly AccessTools.FieldRef<BuildingHospital, LocalizedString> HospitalError = Field<BuildingHospital, LocalizedString>("stringNoDead");
        private static readonly AccessTools.FieldRef<BuildingHospital, LocalizedString> HospitalTitle = Field<BuildingHospital, LocalizedString>("stringHospital");
        private static readonly AccessTools.FieldRef<BuildingTrade, ResourceAmount> TradeCost = Field<BuildingTrade, ResourceAmount>("cost");
        private static readonly AccessTools.FieldRef<WorldResource, ResourceType> Resource = Field<WorldResource, ResourceType>("resource");
        private static readonly AccessTools.FieldRef<Destroyable, RaceEvent> SalvageRequirement = Field<Destroyable, RaceEvent>("canUpgrade");
        private static readonly AccessTools.FieldRef<UnitQuest, string> CageUnit = Field<UnitQuest, string>("entityIdentifier");
        private static readonly AccessTools.FieldRef<UnitQuest, string> CageAchievement = Field<UnitQuest, string>("requiredAchievement");
        private static readonly AccessTools.FieldRef<UnitQuest, GameObject> CageHint = Field<UnitQuest, GameObject>("questionMark");
        private static readonly AccessTools.FieldRef<UnitQuest, GameObject> CageReadyIcon = Field<UnitQuest, GameObject>("exclamationMark");

        private static readonly Func<WorldUnit, int> RecruitPrice =
            AccessTools.MethodDelegate<Func<WorldUnit, int>>(AccessTools.DeclaredMethod(typeof(WorldUnit), "GetCoinCost"));
        private static readonly Func<BuildingHospital, int> RevivePrice =
            AccessTools.MethodDelegate<Func<BuildingHospital, int>>(AccessTools.DeclaredMethod(typeof(BuildingHospital), "GetCosts"));

        private static AccessTools.FieldRef<T, F> Field<T, F>(string name) =>
            AccessTools.FieldRefAccess<T, F>(name);

        internal static MarkerIcon? ForPickup(ClickableSprite target, bool rareFinds)
        {
            switch (target)
            {
                case DropChest _: return MarkerIcon.Treasure;
                case WorldHermit _: return MarkerIcon.Hermit;
                case WorldRewardGiver _: return MarkerIcon.Reward;
                case WorldSpell _: return MarkerIcon.Spell;
                case WorldMonster _: return MarkerIcon.Provoke;
                case Trap _: return MarkerIcon.Capture;
                case DropRelic _:
                case WorldRelic _: return MarkerIcon.Relic;
                case WorldItem _: return MarkerIcon.Item;
                case DropUnit _:
                case WorldUnit _: return MarkerIcon.Recruit;
                case WorldApple _ when rareFinds: return MarkerIcon.Apple;
                case WorldSuperMetal _ when rareFinds: return MarkerIcon.Rare;
                case WorldResource resource when rareFinds && Resource(resource) == ResourceType.Shards:
                    return MarkerIcon.Rare;
                case Destroyable salvage when rareFinds && IsHighValueSalvage(salvage):
                    return MarkerIcon.Salvage;
                default: return null;
            }
        }

        internal static MarkerIcon? ForSite(ClickableSprite target, bool unitCages)
        {
            switch (target)
            {
                case BuildingQuest _:
                case BuildingBoard _: return MarkerIcon.Quest;
                case WorldHero _: return MarkerIcon.Hero;
                case BuildingWorldPirates _: return MarkerIcon.Pirate;
                case BuildingWorldWonder _: return MarkerIcon.Wonder;
                case BuildingWitch _: return MarkerIcon.Witch;
                case BuildingHospital _: return MarkerIcon.Hospital;
                case BuildingTrade _: return MarkerIcon.Trade;
                case UnitQuest _ when unitCages: return MarkerIcon.Cage;
                default: return null;
            }
        }

        internal static MarkerState Read(ClickableSprite target)
        {
            switch (target)
            {
                case BuildingQuest quest:
                    if (!quest.IsDone() || quest.IsFinished() || quest.GetProgressPercentage() < 1f)
                        return MarkerState.Hidden;
                    if (quest is BuildingDungeon && (DaytimeManager.Instance.IsNight() ||
                        (DaytimeManager.Instance.CurrentDay() == 28 && !DaytimeManager.Instance.IsMorningOrLoading())))
                        return MarkerState.Restricted;
                    break;
                case BuildingBoard board:
                    if (!board.IsCityFinished() && !board.IsCombatFinished() && !board.IsDefenseFinished())
                        return MarkerState.Hidden;
                    break;
                case BuildingWorldPirates pirates:
                    int day = DaytimeManager.Instance.CurrentDay();
                    int returning = PirateReturn(pirates);
                    if (returning <= 0 || returning > day || day <= 1 ||
                        !RaceDataManager.Instance.HasRaceEventFlag(RaceEvent.PiratesHelped))
                        return MarkerState.Hidden;
                    break;
                case BuildingWorldWonder wonder:
                    if (!WonderPending(wonder) || !IsShowing(WonderReady(wonder)))
                        return MarkerState.Hidden;
                    break;
                case BuildingWitch _:
                    if (RaceDataManager.Instance.GetBonfiresUnlocked() >= 4)
                        return MarkerState.Hidden;
                    return RaceDataManager.Instance.GetStars() >= WitchCost().amount
                        ? MarkerState.Available : MarkerState.Cost;
                case WorldSpell spell:
                    var spellData = GetSpell(spell);
                    if (spellData == null)
                        return MarkerState.Hidden;
                    return SpellManager.Instance.CanAddSpell(spellData.IsCombatSpell())
                        ? MarkerState.Available : MarkerState.Restricted;
                case WorldMonster monster:
                    if (string.IsNullOrEmpty(monster.GetEntityType()))
                        return MarkerState.Hidden;
                    return ProvokeRestriction() == ProvokeBlock.None ? MarkerState.Available : MarkerState.Restricted;
                case Trap trap:
                    if (string.IsNullOrEmpty(Captured(trap)))
                        return MarkerState.Hidden;
                    break;
                case WorldItem item:
                    if (GetItem(item) == null)
                        return MarkerState.Hidden;
                    break;
                case WorldRelic relic:
                    if (GetRelic(RelicOffer(relic)) == null)
                        return MarkerState.Hidden;
                    break;
                case WorldUnit recruit:
                    var unit = GetUnit(UnitOffer(recruit));
                    if (unit == null)
                        return MarkerState.Hidden;
                    if (!UnitManager.Instance.CanAddUnit(unit, canDouble: true))
                        return MarkerState.Restricted;
                    break;
                case WorldHero hero:
                    if (GetHero(hero) == null)
                        return MarkerState.Hidden;
                    return HeroKills(hero) >= HeroRequired(hero) ? MarkerState.Available : MarkerState.Restricted;
                case BuildingHospital _:
                    if (!HasReviveTarget())
                        return MarkerState.Restricted;
                    break;
                case WorldApple _:
                    return UnitManager.Instance.GetLowestLevelUnit() != null
                        ? MarkerState.Available : MarkerState.Restricted;
                case UnitQuest cage:
                    var unlocked = PermanentDataManager.Instance;
                    var achievement = CageAchievement(cage);
                    if (string.IsNullOrEmpty(achievement) || unlocked.GetSolvedUnitQuests().Contains(achievement) ||
                        (!IsShowing(CageHint(cage)) && !IsShowing(CageReadyIcon(cage))))
                        return MarkerState.Hidden;
                    return unlocked.GetUnlockedAchievements().Contains(achievement)
                        ? MarkerState.Available : MarkerState.Restricted;
                case Destroyable salvage:
                    if (SalvageRequirement(salvage) != RaceEvent.None &&
                        !RaceDataManager.Instance.HasRaceEventFlag(SalvageRequirement(salvage)))
                        return MarkerState.Restricted;
                    break;
            }

            var price = Cost(target);
            return price.type == ResourceType.None || ResourceManager.Instance.CanBeBuilt(price)
                ? MarkerState.Available : MarkerState.Cost;
        }

        internal static (string Title, string Reason) Describe(ClickableSprite target, MarkerState state)
        {
            string title = target switch
            {
                BuildingQuest quest => quest.GetTitle(),
                BuildingBoard _ => "Quest board",
                WorldSpell spell => GetSpell(spell)?.GetTitle() ?? "Spell pickup",
                WorldMonster monster => GetUnit(monster.GetEntityType())?.GetName() ?? "Monster",
                Trap trap => GetUnit(Captured(trap))?.GetName() ?? "Captured monster",
                DropRelic relic => GetRelic(RelicDrop(relic))?.GetTitle() ?? "Dropped relic",
                WorldRelic relic => GetRelic(RelicOffer(relic))?.GetTitle() ?? "Relic offer",
                WorldItem item => GetItem(item)?.GetTitle() ?? "Item offer",
                DropUnit drop => GetUnit(UnitDrop(drop))?.GetName() ?? "Unit reward",
                WorldUnit recruit => GetUnit(UnitOffer(recruit))?.GetName() ?? "Recruitable unit",
                WorldHero hero => GetHero(hero)?.GetName() ?? "Hero encounter",
                BuildingWorldPirates _ => "Pirate return",
                BuildingWorldWonder _ => "Wonder reward",
                BuildingWitch _ => "Witch’s bonfire",
                BuildingHospital hospital => Localize(HospitalTitle(hospital), "Hospital"),
                BuildingTrade trade => trade.name.StartsWith("TradeAlchemist", StringComparison.Ordinal)
                    ? "Alchemist" : "Resource trade",
                WorldApple _ => "XP apple",
                WorldSuperMetal _ => "Super Metal",
                WorldResource _ => "Shards",
                UnitQuest cage => GetUnit(CageUnit(cage))?.GetName() ?? "Unit cage",
                _ => "Coastal salvage"
            };

            if (state == MarkerState.Restricted)
                return (title, Restriction(target));
            ResourceAmount price = Cost(target);
            if (price.type != ResourceType.None)
                return (title, FormatCost(price, state == MarkerState.Cost));
            string reason = target switch
            {
                BuildingQuest _ => "Ready to turn in",
                BuildingBoard _ => "Ready to turn in",
                BuildingWorldPirates _ => "Ready to collect",
                BuildingWorldWonder _ => "Ready to collect",
                Trap _ => "Ready to collect",
                WorldMonster monster => $"Provoke (level {monster.GetLevel()})",
                WorldHero _ => "Ready to unlock",
                UnitQuest _ => "Achievement complete",
                _ => null
            };
            return (title, reason);
        }

        private static string Restriction(ClickableSprite target)
        {
            switch (target)
            {
                case WorldSpell spell:
                    return Localize(SpellError(spell), "No spell slot available");
                case WorldMonster monster:
                    return ProvokeRestriction() switch
                    {
                        ProvokeBlock.AlreadyProvoked => Localize(ProvokeAnother(monster), "Another monster is already provoked"),
                        ProvokeBlock.BossDead => Localize(ProvokeImpossible(monster), "Provocation is unavailable"),
                        ProvokeBlock.AfterWork => Localize(ProvokeDay(monster), "Provoke during work hours"),
                        ProvokeBlock.Rush => Localize(ProvokeRush(monster), "Unavailable during a glory rush"),
                        _ => "Provocation is unavailable"
                    };
                case WorldUnit unit:
                    string name = GetUnit(UnitOffer(unit))?.GetName() ?? "unit";
                    var unitError = UnitError(unit);
                    return unitError == null || unitError.IsEmpty
                        ? "This unit cannot join your roster" : unitError.GetLocalizedString(name);
                case WorldHero hero:
                    int remaining = HeroRequired(hero) - HeroKills(hero);
                    var requirement = HeroError(hero);
                    return requirement == null || requirement.IsEmpty
                        ? $"Defeat {remaining} more enemies"
                        : requirement.GetLocalizedString(remaining, GetHero(hero)?.GetName() ?? "Hero");
                case BuildingHospital hospital:
                    return Localize(HospitalError(hospital), "No unit can be revived");
                case BuildingDungeon _:
                    return DaytimeManager.Instance.IsNight() ? "Unavailable at night" : "Available in the morning";
                case WorldApple _:
                    return "No unit can gain experience";
                case UnitQuest _:
                    return "Complete the unit’s achievement";
                default:
                    return "A discovery requirement is still unmet";
            }
        }

        private static ResourceAmount Cost(ClickableSprite target) => target switch
        {
            WorldItem item => new ResourceAmount(ResourceType.Faith, GetItem(item)?.GetCost() ?? 0),
            WorldRelic relic => new ResourceAmount(ResourceType.Faith, GetRelic(RelicOffer(relic))?.GetCost() ?? 0),
            WorldUnit recruit => new ResourceAmount(ResourceType.Coins, RecruitPrice(recruit)),
            BuildingHospital hospital => new ResourceAmount(ResourceType.Coins, RevivePrice(hospital)),
            BuildingTrade trade => TradeCost(trade),
            BuildingWitch _ => WitchCost(),
            WorldSuperMetal _ => new ResourceAmount(ResourceType.Shards, 12),
            _ => default
        };

        private static ResourceAmount WitchCost() =>
            new ResourceAmount(ResourceType.Stars, RaceDataManager.Instance.GetBonfiresUnlocked() + 12);

        internal static string FormatCost(ResourceAmount price, bool blocked)
        {
            var data = RaceManager.Instance.GetResourceData(price.type);
            string resource = data != null ? data.GetName() : price.type.ToString();
            return blocked ? $"Requires {price.amount} {resource}" : $"{price.amount} {resource}";
        }

        private static bool HasReviveTarget()
        {
            foreach (var unit in UnitManager.Instance.GetAllUnits())
            {
                if (unit != null && unit.IsDead() && !unit.IsHero() && unit.HasRanks() &&
                    unit.GetGridPosition() >= 0 && unit.GetGridPosition() != 98 && unit.GetGridPosition() != 99)
                    return true;
            }
            return false;
        }

        private static bool IsHighValueSalvage(Destroyable target) =>
            target.name.StartsWith("DestroyableFloatsamChest", StringComparison.Ordinal) ||
            target.name.StartsWith("DestroyableFloatsamShards", StringComparison.Ordinal) ||
            target.name.StartsWith("DestroyableSuperMetal", StringComparison.Ordinal);

        private static bool IsShowing(GameObject indicator) => indicator != null && indicator.activeInHierarchy;

        private enum ProvokeBlock { None, AlreadyProvoked, BossDead, AfterWork, Rush, Unknown }

        private static ProvokeBlock ProvokeRestriction()
        {
            if (ProvokedSpawner.Instance == null || BossSpawner.Instance == null || MonsterSpawner.Instance == null)
                return ProvokeBlock.Unknown;
            // Keep the same order as WorldMonster.AfterDialogue.
            if (ProvokedSpawner.Instance.IsProvoked())
                return ProvokeBlock.AlreadyProvoked;
            if (BossSpawner.Instance.IsBossDead())
                return ProvokeBlock.BossDead;
            if (!DaytimeManager.Instance.IsWorkTime())
                return ProvokeBlock.AfterWork;
            return !DaytimeManager.Instance.IsMorningOrLoading() && MonsterSpawner.Instance.GetBonusGlory() > 0f
                ? ProvokeBlock.Rush : ProvokeBlock.None;
        }

        internal static Sprite GetSymbol(ClickableSprite target) => target switch
        {
            WorldMonster monster => GetUnit(monster.GetEntityType())?.GetIcon(),
            WorldSpell spell => GetSpell(spell)?.GetIcon(),
            Trap trap => GetUnit(Captured(trap))?.GetIcon(),
            DropRelic relic => GetRelic(RelicDrop(relic))?.GetIcon(),
            WorldRelic relic => GetRelic(RelicOffer(relic))?.GetIcon(),
            WorldItem item => GetItem(item)?.GetIcon(),
            DropUnit drop => GetUnit(UnitDrop(drop))?.GetIcon(),
            WorldUnit recruit => GetUnit(UnitOffer(recruit))?.GetIcon(),
            WorldHero hero => GetHero(hero)?.GetIcon(),
            UnitQuest cage => GetUnit(CageUnit(cage))?.GetIcon(),
            _ => null
        };

        private static SpellBase GetSpell(WorldSpell target) => SpellManager.Instance.GetSpell(SpellId(target));
        private static Item GetItem(WorldItem target) =>
            string.IsNullOrEmpty(ItemOffer(target)) ? null : AddressablesManager.Instance.GetItem(ItemOffer(target));
        private static Unit GetUnit(string id) => string.IsNullOrEmpty(id) ? null : AddressablesManager.Instance.GetUnit(id);
        private static Relic GetRelic(string id) => string.IsNullOrEmpty(id) ? null : AddressablesManager.Instance.GetRelic(id);
        private static Hero GetHero(WorldHero target) =>
            string.IsNullOrEmpty(HeroId(target)) ? null : AddressablesManager.Instance.GetHero(HeroId(target));

        private static string Localize(LocalizedString text, string fallback) =>
            text == null || text.IsEmpty ? fallback : text.GetLocalizedString();
    }
}
