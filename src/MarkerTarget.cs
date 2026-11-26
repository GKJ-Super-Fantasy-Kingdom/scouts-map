using HarmonyLib;
using SuperFantasyKingdom;
using SuperFantasyKingdom.Clickable;
using SuperFantasyKingdom.UI;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

namespace ScoutsMap
{
    internal enum MarkerKind { Treasure, Hermit, Reward, Road, Shop }
    internal enum MarkerState { Hidden, Available, Setup, DayOne, Night, Glory, Level, Stars, Cost }

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
        private static readonly AccessTools.FieldRef<BuildingShop, SpriteRenderer> ShopCountdown =
            AccessTools.FieldRefAccess<BuildingShop, SpriteRenderer>("daysUntilWork");
        private static readonly AccessTools.FieldRef<BuildingObjectShop, SpriteRenderer> ObjectShopCountdown =
            AccessTools.FieldRefAccess<BuildingObjectShop, SpriteRenderer>("daysUntilWork");
        private static readonly AccessTools.FieldRef<BuildingShop, LocalizedString> ShopTitle =
            AccessTools.FieldRefAccess<BuildingShop, LocalizedString>("stringTitle");
        private static readonly AccessTools.FieldRef<BuildingObjectShop, LocalizedString> ObjectShopTitle =
            AccessTools.FieldRefAccess<BuildingObjectShop, LocalizedString>("stringTitle");
        private static readonly AccessTools.FieldRef<ClickableSprite, BoxCollider2D> TargetCollider =
            AccessTools.FieldRefAccess<ClickableSprite, BoxCollider2D>("boxCollider2D");
        private static readonly AccessTools.FieldRef<ClickableSprite, SpriteRenderer> TargetSprite =
            AccessTools.FieldRefAccess<ClickableSprite, SpriteRenderer>("spriteRenderer");

        internal readonly ClickableSprite Clickable;
        internal readonly MarkerKind Kind;
        internal readonly int InstanceId;
        private readonly UIArrowRoad roadArrow;
        private readonly UIMarker nativeMarker;
        private readonly Image nativeImage;
        private readonly bool originalImageEnabled;
        private readonly Road road;

        internal MarkerBadge Badge;
        internal MarkerIcon Icon => Kind switch
        {
            MarkerKind.Treasure => MarkerIcon.Treasure,
            MarkerKind.Hermit => MarkerIcon.Hermit,
            MarkerKind.Reward => MarkerIcon.Reward,
            MarkerKind.Road when road.city => MarkerIcon.City,
            MarkerKind.Road when road.pier != null => MarkerIcon.Water,
            MarkerKind.Road => MarkerIcon.Road,
            _ => MarkerIcon.Shop
        };
        internal Vector3 Position => Clickable.transform.position;
        internal bool Expired => Clickable == null ||
            (Kind < MarkerKind.Road && nativeMarker == null);

        private MarkerTarget(ClickableSprite clickable, MarkerKind kind, UIMarker marker = null,
            Image image = null, UIArrowRoad arrow = null, Road road = null)
        {
            Clickable = clickable;
            InstanceId = clickable.GetInstanceID();
            Kind = kind;
            nativeMarker = marker;
            nativeImage = image;
            originalImageEnabled = image != null && image.enabled;
            roadArrow = arrow;
            this.road = road;
        }

        internal static MarkerTarget ForEvent(UIMarker marker, Image image, GameObject target)
        {
            if (target.TryGetComponent(out DropChest chest))
                return new MarkerTarget(chest, MarkerKind.Treasure, marker, image);
            if (target.TryGetComponent(out WorldHermit hermit))
                return new MarkerTarget(hermit, MarkerKind.Hermit, marker, image);
            if (target.TryGetComponent(out WorldRewardGiver reward))
                return new MarkerTarget(reward, MarkerKind.Reward, marker, image);
            return null;
        }

        internal static MarkerTarget ForRoad(UIArrowRoad arrow, RoadBuilder builder)
        {
            string id = RoadId(arrow);
            Road road = string.IsNullOrEmpty(id) ? null : builder.GetRoad(id);
            return road == null ? null : new MarkerTarget(arrow, MarkerKind.Road, arrow: arrow, road: road);
        }

        internal static MarkerTarget ForShop(BuildingWorld shop) =>
            shop is BuildingShop || shop is BuildingObjectShop ? new MarkerTarget(shop, MarkerKind.Shop) : null;

        internal void ReplaceNativeImage(bool replace)
        {
            if (nativeImage != null && nativeImage.enabled != (originalImageEnabled && !replace))
                nativeImage.enabled = originalImageEnabled && !replace;
        }

        internal MarkerState Read()
        {
            if (Expired)
                return MarkerState.Hidden;
            if (Kind == MarkerKind.Road)
                return ReadRoad();
            if (!Clickable.isActiveAndEnabled)
                return MarkerState.Hidden;
            if (Kind == MarkerKind.Shop)
            {
                var fog = RaceManager.Instance.GetFogTilemap();
                if (fog == null || fog.HasTile(fog.WorldToCell(Position)))
                    return MarkerState.Hidden;
                var sprite = TargetSprite(Clickable);
                if (sprite == null || !sprite.enabled)
                    return MarkerState.Hidden;
                if (DaytimeManager.Instance.CurrentDay() <= SetupDay)
                {
                    var countdown = Clickable is BuildingShop shop ? ShopCountdown(shop)
                        : ObjectShopCountdown((BuildingObjectShop)Clickable);
                    return countdown != null && countdown.enabled && countdown.gameObject.activeInHierarchy && countdown.sprite != null
                        ? MarkerState.Setup : MarkerState.Hidden;
                }
            }
            var collider = TargetCollider(Clickable);
            return Clickable.IsInteractable() && collider != null && collider.isActiveAndEnabled
                ? MarkerState.Available : MarkerState.Hidden;
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
                case MarkerKind.Treasure: title = "Treasure chest"; break;
                case MarkerKind.Hermit: title = "Hermit"; break;
                case MarkerKind.Reward: title = "World reward"; break;
                case MarkerKind.Road: title = road.city ? "City expansion" : road.pier != null ? "Water access" : "Road expansion"; break;
                default: title = Localize(Clickable is BuildingShop shop ? ShopTitle(shop) :
                    ObjectShopTitle((BuildingObjectShop)Clickable), "Shop"); break;
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
                case MarkerState.Cost:
                    ResourceAmount cost = road.GetCost();
                    ResourceData data = RaceManager.Instance.GetResourceData(cost.type);
                    reason = $"Requires {cost.amount} {(data != null ? data.GetName() : cost.type.ToString())}";
                    break;
                default: return (title, null);
            }
            return (title, reason);
        }

        private int SetupDay => Clickable is BuildingShop shop ? ShopSetup(shop)
            : ObjectShopSetup((BuildingObjectShop)Clickable);

        private static string Localize(LocalizedString text, string fallback) =>
            text == null || text.IsEmpty ? fallback : text.GetLocalizedString();
    }
}
