using HarmonyLib;
using SuperFantasyKingdom;
using SuperFantasyKingdom.Clickable;
using SuperFantasyKingdom.UI;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

namespace ScoutsMap
{
    internal enum MarkerState { Hidden, Available, Setup, DayOne, Night, Glory, Level, Stars, Cost, Restricted }

    internal sealed class MarkerTarget
    {
        private static readonly AccessTools.FieldRef<UIArrowRoad, string> RoadId =
            AccessTools.FieldRefAccess<UIArrowRoad, string>("m_RoadIdentifier");
        private static readonly AccessTools.FieldRef<UIArrowRoad, LocalizedString> TooLate =
            AccessTools.FieldRefAccess<UIArrowRoad, LocalizedString>("stringTooLate");
        private static readonly AccessTools.FieldRef<UIArrowRoad, LocalizedString> NeedGlory =
            AccessTools.FieldRefAccess<UIArrowRoad, LocalizedString>("stringNotEnoughGlory");
        private static readonly AccessTools.FieldRef<UIArrowRoad, LocalizedString> NeedLevel =
            AccessTools.FieldRefAccess<UIArrowRoad, LocalizedString>("stringNotEnoughKingdomLevel");
        private static readonly AccessTools.FieldRef<UIArrowRoad, LocalizedString> NeedStars =
            AccessTools.FieldRefAccess<UIArrowRoad, LocalizedString>("stringNotEnoughStars");
        private static readonly AccessTools.FieldRef<BuildingShop, int> ShopSetup =
            AccessTools.FieldRefAccess<BuildingShop, int>("daysToSetup");
        private static readonly AccessTools.FieldRef<BuildingObjectShop, int> ObjectShopSetup =
            AccessTools.FieldRefAccess<BuildingObjectShop, int>("daysToSetup");
        private static readonly AccessTools.FieldRef<BuildingGuildOld, int> GuildSetup =
            AccessTools.FieldRefAccess<BuildingGuildOld, int>("daysToSetup");
        private static readonly AccessTools.FieldRef<BuildingShop, SpriteRenderer> ShopCountdown =
            AccessTools.FieldRefAccess<BuildingShop, SpriteRenderer>("daysUntilWork");
        private static readonly AccessTools.FieldRef<BuildingObjectShop, SpriteRenderer> ObjectShopCountdown =
            AccessTools.FieldRefAccess<BuildingObjectShop, SpriteRenderer>("daysUntilWork");
        private static readonly AccessTools.FieldRef<BuildingGuildOld, SpriteRenderer> GuildCountdown =
            AccessTools.FieldRefAccess<BuildingGuildOld, SpriteRenderer>("daysUntilWork");
        private static readonly AccessTools.FieldRef<BuildingShop, LocalizedString> ShopTitle =
            AccessTools.FieldRefAccess<BuildingShop, LocalizedString>("stringTitle");
        private static readonly AccessTools.FieldRef<BuildingObjectShop, LocalizedString> ObjectShopTitle =
            AccessTools.FieldRefAccess<BuildingObjectShop, LocalizedString>("stringTitle");
        private static readonly AccessTools.FieldRef<BuildingGuildOld, LocalizedString> GuildTitle =
            AccessTools.FieldRefAccess<BuildingGuildOld, LocalizedString>("stringTitle");
        private static readonly AccessTools.FieldRef<BuildingGuildOld, LocalizedString> GuildFull =
            AccessTools.FieldRefAccess<BuildingGuildOld, LocalizedString>("stringError");
        private static readonly AccessTools.FieldRef<ClickableSprite, BoxCollider2D> TargetCollider =
            AccessTools.FieldRefAccess<ClickableSprite, BoxCollider2D>("boxCollider2D");
        private static readonly AccessTools.FieldRef<ClickableSprite, SpriteRenderer> TargetSprite =
            AccessTools.FieldRefAccess<ClickableSprite, SpriteRenderer>("spriteRenderer");

        internal readonly ClickableSprite Clickable;
        internal readonly MarkerIcon Kind;
        internal readonly int InstanceId;
        private readonly ClickableSprite interaction;
        private readonly UIArrowRoad roadArrow;
        private UIMarker nativeMarker;
        private Image nativeImage;
        private bool originalImageEnabled;
        private readonly Road road;

        internal MarkerBadge Badge;
        internal MarkerIcon Icon => Kind switch
        {
            MarkerIcon.Road when road.city => MarkerIcon.City,
            MarkerIcon.Road when road.pier != null => MarkerIcon.Water,
            _ => Kind
        };
        internal Vector3 Position => Clickable.transform.position;
        internal Vector3 VisualTop
        {
            get
            {
                Vector3 top = Position;
                var sprite = SpriteRenderer;
                if (sprite != null && sprite.enabled && sprite.sprite != null)
                    top = new Vector3(sprite.bounds.center.x, sprite.bounds.max.y, top.z);
                var collider = TargetCollider(interaction);
                if (collider != null && collider.isActiveAndEnabled)
                    top.y = Mathf.Max(top.y, collider.bounds.max.y);
                return top;
            }
        }

        internal Sprite EntitySprite
        {
            get
            {
                if (Clickable is BuildingSpotWorld plot)
                    return WorldSites.RestoreIcon(plot) ?? SpriteRenderer?.sprite;
                if ((Clickable is BuildingWorld && roadArrow == null) || Clickable is BuildingBoard ||
                    Clickable is RepairableBuilding || Clickable is BuildingCloudColor)
                    return SpriteRenderer?.sprite;
                return EncounterTargets.GetSymbol(Clickable);
            }
        }

        internal bool NeedsNativeMarker { get; }
        internal bool Expired => Clickable == null;
        internal bool IsQuestTurnIn => EncounterTargets.IsQuestTurnIn(Clickable);
        internal MarkerGroup Group => MarkerGroups.Get(Kind, Kind == MarkerIcon.Quest && IsQuestTurnIn);
        private SpriteRenderer SpriteRenderer => TargetSprite(Clickable) ??
            (interaction != null ? TargetSprite(interaction) : null);

        private MarkerTarget(ClickableSprite clickable, MarkerIcon kind, bool needsNativeMarker = false,
            UIArrowRoad arrow = null, Road road = null)
        {
            Clickable = clickable;
            interaction = clickable is BuildingTradingPost post ? WorldSites.TradingOverlay(post) : clickable;
            InstanceId = clickable.GetInstanceID();
            Kind = kind;
            NeedsNativeMarker = needsNativeMarker;
            roadArrow = arrow;
            this.road = road;
        }

        internal static MarkerTarget ForEvent(GameObject target)
        {
            if (!target.TryGetComponent(out ClickableSprite clickable))
                return null;

            MarkerTarget site = ForSite(clickable);
            if (site != null)
                return site;
            var icon = EncounterTargets.ForPickup(clickable);
            return icon.HasValue ? new MarkerTarget(clickable, icon.Value, needsNativeMarker: true) : null;
        }

        internal static MarkerTarget ForSite(ClickableSprite target)
        {
            if (target is BuildingShop || target is BuildingObjectShop || target is BuildingGuildOld)
                return new MarkerTarget(target, MarkerIcon.Shop);
            var icon = EncounterTargets.ForSite(target);
            return icon.HasValue ? new MarkerTarget(target, icon.Value) : null;
        }

        internal void AttachNativeMarker(UIMarker marker, Image image)
        {
            if (nativeMarker == marker)
                return;
            ReplaceNativeImage(false);
            nativeMarker = marker;
            nativeImage = image;
            originalImageEnabled = image.enabled;
        }

        internal static MarkerTarget ForRoad(UIArrowRoad arrow, RoadBuilder builder)
        {
            string id = RoadId(arrow);
            Road road = string.IsNullOrEmpty(id) ? null : builder.GetRoad(id);
            return road == null ? null : new MarkerTarget(arrow, MarkerIcon.Road, arrow: arrow, road: road);
        }

        internal void ReplaceNativeImage(bool replace)
        {
            if (nativeImage != null && nativeImage.enabled != (originalImageEnabled && !replace))
                nativeImage.enabled = originalImageEnabled && !replace;
        }

        internal MarkerState Read()
        {
            if (Expired)
                return MarkerState.Hidden;
            if (Kind == MarkerIcon.Road)
                return ReadRoad();
            if (!Clickable.isActiveAndEnabled || interaction == null || !interaction.isActiveAndEnabled)
                return MarkerState.Hidden;
            // Native encounter arrows are already a discovery signal. Static sites must be uncovered.
            if (Kind == MarkerIcon.Provoke ? !IsUncovered()
                : (nativeMarker == null || Kind == MarkerIcon.Shop) && !IsExposed())
                return MarkerState.Hidden;
            if (Kind == MarkerIcon.Shop && DaytimeManager.Instance.CurrentDay() <= SetupDay)
            {
                var countdown = Clickable switch
                {
                    BuildingShop shop => ShopCountdown(shop),
                    BuildingObjectShop shop => ObjectShopCountdown(shop),
                    BuildingGuildOld shop => GuildCountdown(shop),
                    _ => null
                };
                return countdown != null && countdown.enabled && countdown.gameObject.activeInHierarchy && countdown.sprite != null
                    ? MarkerState.Setup : MarkerState.Hidden;
            }

            var collider = TargetCollider(interaction);
            if (!interaction.IsInteractable() || collider == null || !collider.isActiveAndEnabled)
                return MarkerState.Hidden;

            switch (Kind)
            {
                case MarkerIcon.Treasure:
                case MarkerIcon.Hermit:
                case MarkerIcon.Reward:
                    return MarkerState.Available;
                case MarkerIcon.Shop:
                    return Clickable is BuildingGuildOld && !(Clickable is BuildingBlackMarket) &&
                        UnitManager.Instance.GetCurrentHungryUnitsAmount() == 16 ? MarkerState.Restricted : MarkerState.Available;
                case MarkerIcon.Restore:
                case MarkerIcon.TradingPost:
                case MarkerIcon.Site:
                    return WorldSites.Read(Clickable, interaction);
                default:
                    return EncounterTargets.Read(Clickable);
            }
        }

        private bool IsExposed()
        {
            var sprite = SpriteRenderer;
            return IsUncovered() && sprite != null && sprite.enabled && sprite.gameObject.activeInHierarchy && sprite.sprite != null;
        }

        private bool IsUncovered()
        {
            var fog = RaceManager.Instance.GetFogTilemap();
            return fog != null && !fog.HasTile(fog.WorldToCell(Position));
        }

        private MarkerState ReadRoad()
        {
            // RoadBuilder only creates an arrow after the adjoining road is discovered.
            // Outside expansion mode, respect the arrow's own visibility gate.
            if (road == null || !road.gameObject.activeSelf || road.parts.Count == 0 ||
                !roadArrow.gameObject.activeSelf)
                return MarkerState.Hidden;
            if (DaytimeManager.Instance.IsNight())
                return MarkerState.Night;
            if (DaytimeManager.Instance.CurrentDay() <= 1 && road.requiredGlory > 0)
                return MarkerState.DayOne;
            if (WorldManager.Instance.GetGlory() < road.requiredGlory)
                return MarkerState.Glory;
            if (RaceDataManager.Instance.GetKingdomLevel() < road.requiredKingdomLevel)
                return MarkerState.Level;
            if (RaceDataManager.Instance.GetStars() < road.requiredStars)
                return MarkerState.Stars;
            return ResourceManager.Instance.CanBeBuilt(road.GetCost())
                ? MarkerState.Available : MarkerState.Cost;
        }

        internal (string Title, string Reason) GetTooltip(MarkerState state)
        {
            string title;
            switch (Kind)
            {
                case MarkerIcon.Treasure: title = "Treasure chest"; break;
                case MarkerIcon.Hermit: title = "Hermit"; break;
                case MarkerIcon.Reward: title = "World reward"; break;
                case MarkerIcon.Road: title = road.city ? "City expansion" : road.pier != null ? "Water access" : "Road expansion"; break;
                case MarkerIcon.Shop:
                    title = Clickable switch
                    {
                        BuildingShop shop => Localize(ShopTitle(shop), "Shop"),
                        BuildingObjectShop shop => Localize(ObjectShopTitle(shop), "Shop"),
                        BuildingBlackMarket shop => Localize(GuildTitle(shop), "Black Market"),
                        BuildingGuildOld shop => Localize(GuildTitle(shop), "Guild"),
                        _ => "Shop"
                    };
                    break;
                case MarkerIcon.Restore:
                case MarkerIcon.TradingPost:
                case MarkerIcon.Site:
                    return WorldSites.Describe(Clickable, state);
                default: return EncounterTargets.Describe(Clickable, state);
            }

            string reason;
            switch (state)
            {
                case MarkerState.Setup: reason = $"Opens on day {SetupDay + 1}"; break;
                case MarkerState.DayOne: reason = "Available from day 2"; break;
                case MarkerState.Night: reason = Localize(TooLate(roadArrow), "Unavailable at night"); break;
                case MarkerState.Glory: reason = Localize(NeedGlory(roadArrow), $"Requires {road.requiredGlory} glory"); break;
                case MarkerState.Level: reason = Localize(NeedLevel(roadArrow), $"Requires kingdom level {road.requiredKingdomLevel}"); break;
                case MarkerState.Stars: reason = Localize(NeedStars(roadArrow), $"Requires {road.requiredStars} stars"); break;
                case MarkerState.Cost: reason = EncounterTargets.FormatCost(road.GetCost(), blocked: true); break;
                case MarkerState.Restricted when Clickable is BuildingGuildOld guild:
                    reason = Localize(GuildFull(guild), "No room to recruit another unit"); break;
                default: return (title, null);
            }
            return (title, reason);
        }

        private int SetupDay => Clickable switch
        {
            BuildingShop shop => ShopSetup(shop),
            BuildingObjectShop shop => ObjectShopSetup(shop),
            BuildingGuildOld shop => GuildSetup(shop),
            _ => -1
        };

        private static string Localize(LocalizedString text, string fallback) =>
            text == null || text.IsEmpty ? fallback : text.GetLocalizedString();
    }
}
