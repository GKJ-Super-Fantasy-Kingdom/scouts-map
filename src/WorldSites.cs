using HarmonyLib;
using SuperFantasyKingdom;
using SuperFantasyKingdom.Buildings;
using SuperFantasyKingdom.Clickable;
using SuperFantasyKingdom.Spawner;
using SuperFantasyKingdom.UI;
using SuperFantasyKingdom.Weather;
using UnityEngine;
using UnityEngine.Localization;

namespace ScoutsMap
{
    internal static class WorldSites
    {
        private static readonly AccessTools.FieldRef<BuildingSpotWorld, ResourceType> PlotResource =
            Field<BuildingSpotWorld, ResourceType>("resource");
        private static readonly AccessTools.FieldRef<RepairableBuilding, BuildingCity> RepairedBuilding =
            Field<RepairableBuilding, BuildingCity>("toSpawn");
        private static readonly AccessTools.FieldRef<BuildingWorldStartingResource, ResourceAmount[]> RestoreCosts =
            Field<BuildingWorldStartingResource, ResourceAmount[]>("cost");
        private static readonly AccessTools.FieldRef<BuildingWorldStartingResource, int> RestoreStars =
            Field<BuildingWorldStartingResource, int>("requiredStars");
        private static readonly AccessTools.FieldRef<BuildingWorldStartingResource, LocalizedString> RestoreTitle =
            Field<BuildingWorldStartingResource, LocalizedString>("stringTitle");
        private static readonly AccessTools.FieldRef<BuildingMiniBoss, ResourceType> MiniBossCost =
            Field<BuildingMiniBoss, ResourceType>("resourceType");
        private static readonly AccessTools.FieldRef<BuildingMiniBoss, LocalizedString> MiniBossTitle =
            Field<BuildingMiniBoss, LocalizedString>("stringTitle");
        private static readonly AccessTools.FieldRef<BuildingMiniBoss, LocalizedString> CampOnlyDay =
            Field<BuildingMiniBoss, LocalizedString>("stringOnlyDuringDay");
        private static readonly AccessTools.FieldRef<BuildingMiniBoss, LocalizedString> CampBossDay =
            Field<BuildingMiniBoss, LocalizedString>("stringBossDay");
        private static readonly AccessTools.FieldRef<BuildingMiniBoss, LocalizedString> CampOneBattle =
            Field<BuildingMiniBoss, LocalizedString>("stringOnlyOneAtATime");
        private static readonly AccessTools.FieldRef<UIBuildingWorldOverlay, BuildingWorld> OverlayTarget =
            Field<UIBuildingWorldOverlay, BuildingWorld>("buildingWorld");
        private static readonly AccessTools.FieldRef<UIBuildingOverlayElementTradingPost, ResourceAmount> TradeResource =
            Field<UIBuildingOverlayElementTradingPost, ResourceAmount>("resource");
        private static readonly AccessTools.FieldRef<BuildingExperience, bool> ExperienceUsed =
            Field<BuildingExperience, bool>("m_IsUsed");
        private static readonly AccessTools.FieldRef<BuildingEye, bool> SeerUsed = Field<BuildingEye, bool>("m_IsUsed");
        private static readonly AccessTools.FieldRef<BuildingWorldGuest, Actors> GuestActor =
            Field<BuildingWorldGuest, Actors>("guest");
        private static readonly AccessTools.FieldRef<BuildingTrainingGrounds, bool> TrainingTomorrow =
            Field<BuildingTrainingGrounds, bool>("m_Tomorrow");
        private static readonly AccessTools.FieldRef<BuildingWizardTower, ResourceAmount> WeatherCost =
            Field<BuildingWizardTower, ResourceAmount>("cost");
        private static readonly AccessTools.FieldRef<BuildingWorldTrapper, int> TrapsBought =
            Field<BuildingWorldTrapper, int>("m_TrapAmount");
        private static readonly AccessTools.FieldRef<BuildingUnit, string> UnitId = Field<BuildingUnit, string>("unitIdentifier");
        private static readonly AccessTools.FieldRef<BuildingUnit, ResourceType> UnitCurrency = Field<BuildingUnit, ResourceType>("m_Cost");
        private static readonly AccessTools.FieldRef<BuildingUnit, bool> UnitBought = Field<BuildingUnit, bool>("m_Bought");
        private static readonly AccessTools.FieldRef<BuildingCloudColor, bool> CloudColorUnlocked =
            Field<BuildingCloudColor, bool>("m_IsUpgraded");

        private static AccessTools.FieldRef<T, F> Field<T, F>(string name) => AccessTools.FieldRefAccess<T, F>(name);

        internal static MarkerIcon? ForSite(ClickableSprite target)
        {
            switch (target)
            {
                case BuildingSpotWorld plot when plot.size == BuildingSize.WorldSmall ||
                    plot.size == BuildingSize.WorldBig || plot.size == BuildingSize.WorldSpecial:
                case RepairableBuilding _: case BuildingWorldStartingResource _: case BuildingWorldCamp _:
                    return MarkerIcon.Restore;
                case BuildingTradingPost _: return MarkerIcon.TradingPost;
                case BuildingExperience _: case BuildingEye _: case BuildingMonsterHunter _:
                case BuildingTrainingGrounds _: case BuildingWizardTower _: case BuildingWorldMonsterBell _:
                case BuildingWorldTrapper _: case BuildingWorldGuest _: case BuildingRandomItem _:
                case BuildingSuperPortal _: case BuildingUnit _: case BuildingCloudColor _:
                    return MarkerIcon.Site;
                default: return null;
            }
        }

        internal static UIBuildingWorldOverlay TradingOverlay(BuildingTradingPost post)
        {
            foreach (var overlay in post.GetComponentsInChildren<UIBuildingWorldOverlay>(true))
                if (OverlayTarget(overlay) == post)
                    return overlay;
            return null;
        }

        internal static Sprite RestoreIcon(BuildingSpotWorld plot) =>
            plot.size == BuildingSize.WorldSmall ? RaceManager.Instance.GetResourceData(PlotResource(plot))?.icon : null;

        internal static MarkerState Read(ClickableSprite target, ClickableSprite interaction)
        {
            switch (target)
            {
                case BuildingSpotWorld plot:
                    if (plot.size == BuildingSize.WorldSmall && RaceManager.Instance.GetResourceData(PlotResource(plot)) == null)
                        return MarkerState.Hidden;
                    break;
                case BuildingWorldStartingResource site:
                    if (site.GetUpgrade() != 0 || RestoreCosts(site) == null || RestoreCosts(site).Length == 0)
                        return MarkerState.Hidden;
                    if (ResourceManager.Instance.GetStars() < RestoreStars(site))
                        return MarkerState.Restricted;
                    break;
                case BuildingWorldCamp camp when camp.RepairedOnDay() > 0:
                case BuildingExperience experience when ExperienceUsed(experience):
                case BuildingEye seer when SeerUsed(seer):
                case BuildingCloudColor clouds when !CloudColorUnlocked(clouds) &&
                    !RaceDataManager.Instance.HasRaceEventFlag(RaceEvent.Witch7):
                    return MarkerState.Hidden;
                case BuildingTradingPost _:
                    return ReadTrades((UIBuildingWorldOverlay)interaction);
            }

            if (Restriction(target) != null)
                return MarkerState.Restricted;
            var cost = Cost(target);
            return cost.type == ResourceType.None || ResourceManager.Instance.CanBeBuilt(cost)
                ? MarkerState.Available : MarkerState.Cost;
        }

        private static MarkerState ReadTrades(UIBuildingWorldOverlay overlay)
        {
            if (overlay.buttons == null)
                return MarkerState.Hidden;
            bool hasTrade = false;
            foreach (var button in overlay.buttons)
            {
                if (!(button is UIBuildingOverlayElementTradingPost trade))
                    continue;
                hasTrade = true;
                if (ResourceManager.Instance.CanBeBuilt(new ResourceAmount(TradeResource(trade).type, 5)))
                    return MarkerState.Available;
            }
            return hasTrade ? MarkerState.Restricted : MarkerState.Hidden;
        }

        internal static (string Title, string Reason) Describe(ClickableSprite target, MarkerState state)
        {
            string title = target switch
            {
                BuildingSpotWorld plot when plot.size == BuildingSize.WorldSmall =>
                    $"Restore {RaceManager.Instance.GetResourceData(PlotResource(plot))?.GetName() ?? "resource site"}",
                BuildingSpotWorld _ => "Restore building site",
                RepairableBuilding repair => $"Restore {RepairedBuilding(repair)?.GetTitle() ?? "damaged building"}",
                BuildingWorldStartingResource site => Localize(RestoreTitle(site), "Starting resource site"),
                BuildingWorldCamp camp => Localize(MiniBossTitle(camp), "Occupied camp"),
                BuildingTradingPost post => post.GetTitle(),
                BuildingWorldGuest guest => Localize(DialogueManager.Instance.GetActor(GuestActor(guest))?.stringTitle, "Guest caller"),
                BuildingExperience _ => "Experience shrine",
                BuildingEye _ => "Seer",
                BuildingMonsterHunter _ => "Monster Hunter",
                BuildingTrainingGrounds _ => "Training Grounds",
                BuildingWizardTower _ => "Wizard Tower",
                BuildingWorldMonsterBell _ => "Monster Bell",
                BuildingWorldTrapper _ => "Trapper",
                BuildingSuperPortal _ => "Recruitment portal",
                BuildingUnit unit => AddressablesManager.Instance.GetUnit(UnitId(unit))?.GetName() ?? "Unit recruitment",
                BuildingCloudColor _ => "Cloud color",
                _ => "World item"
            };
            if (state == MarkerState.Restricted)
                return (title, Restriction(target, describe: true));
            var cost = Cost(target);
            return (title, cost.type != ResourceType.None ? EncounterTargets.FormatCost(cost, state == MarkerState.Cost) :
                target is BuildingSpotWorld ? "Choose a building" : null);
        }

        private static string Restriction(ClickableSprite target, bool describe = false)
        {
            switch (target)
            {
                case BuildingTradingPost _: return "Requires 5 of a resource the Trading Post accepts";
                case BuildingWorldStartingResource site:
                    if (ResourceManager.Instance.GetStars() >= RestoreStars(site))
                        return null;
                    return describe ? EncounterTargets.FormatCost(new ResourceAmount(ResourceType.Stars, RestoreStars(site)), true)
                        : "More stars required";
                case BuildingWorldCamp camp:
                    if (DaytimeManager.Instance.IsLoading() || CombatManager.Instance.IsCombatOver() ||
                        MonsterSpawner.Instance.GetCurrentDifficulty() == 0)
                        return describe ? Localize(CampOnlyDay(camp), "Available during the day") : "Available during the day";
                    if (DaytimeManager.Instance.IsBossDay())
                        return describe ? Localize(CampBossDay(camp), "Unavailable on boss days") : "Unavailable on boss days";
                    return CombatManager.Instance.IsMiniBoss()
                        ? (describe ? Localize(CampOneBattle(camp), "Another miniboss is already active") : "Another miniboss is already active")
                        : null;
                case BuildingWorldGuest guest:
                    return WorldManager.Instance.GetNextGuest() == (int)GuestActor(guest) ? "Guest already scheduled" : null;
                case BuildingMonsterHunter _:
                    return DaytimeManager.Instance.IsMorningOrLoading() ? "Available after the morning" : null;
                case BuildingTrainingGrounds training:
                    if (UnitManager.Instance.GetLowestLevelUnit() == null || UnitManager.Instance.GetCurrentHungryUnitsAmount() < 2)
                        return "Requires two units and a unit that can train";
                    return TrainingTomorrow(training) ? "Available tomorrow" : null;
                case BuildingWizardTower _:
                    return WeatherManager.Instance.GetRealWeatherTomorrow() == WeatherTypes.None ? "Tomorrow’s weather is already clear" : null;
                case BuildingSuperPortal _:
                    return UnitManager.Instance.GetCurrentHungryUnitsAmount() == 16 ? "No room to recruit another unit" : null;
                case BuildingUnit unit:
                    var actor = UnitManager.Instance.GetUnit(UnitId(unit));
                    return UnitManager.Instance.GetUpgradeCost(UnitId(unit), UnitCurrency(unit)) < 0 ||
                        (UnitBought(unit) && (actor == null || actor.GetGridPosition() < 1)) ? "Unit unavailable for recruitment or upgrade" : null;
                default: return null;
            }
        }

        private static ResourceAmount Cost(ClickableSprite target) => target switch
        {
            BuildingSpotWorld plot when plot.size == BuildingSize.WorldSmall =>
                new ResourceAmount(ResourceType.Shards, 1 + WorldManager.Instance.GetTotalAmountOfRebuilds()),
            RepairableBuilding repair => repair.cost,
            BuildingWorldStartingResource site => RestoreCosts(site)[0],
            BuildingWorldCamp camp => new ResourceAmount(MiniBossCost(camp), 1),
            BuildingTrainingGrounds _ => new ResourceAmount(ResourceType.Beer, 1),
            BuildingWizardTower wizard => WeatherCost(wizard),
            BuildingWorldMonsterBell _ => new ResourceAmount(ResourceType.Coins, 6 + DaytimeManager.Instance.CurrentDay()),
            BuildingWorldTrapper trapper when TrapsBought(trapper) > 0 => new ResourceAmount(ResourceType.Wood, 3 + TrapsBought(trapper)),
            BuildingUnit unit => new ResourceAmount(UnitCurrency(unit), UnitManager.Instance.GetUpgradeCost(UnitId(unit), UnitCurrency(unit))),
            BuildingCloudColor clouds when !CloudColorUnlocked(clouds) => new ResourceAmount(ResourceType.Shards, 100),
            _ => default
        };

        private static string Localize(LocalizedString text, string fallback) =>
            text == null || text.IsEmpty ? fallback : text.GetLocalizedString();
    }
}
