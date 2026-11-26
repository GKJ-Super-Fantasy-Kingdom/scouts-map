using SuperFantasyKingdom;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ScoutsMap
{
    internal sealed class MarkerBadge : MonoBehaviour, IPointerDownHandler, IPointerClickHandler,
        IPointerEnterHandler, IPointerExitHandler, ICanvasRaycastFilter
    {
        private static readonly Color32 DisabledRim = new Color32(106, 117, 135, 255);
        private static readonly Color32 DisabledFocus = new Color32(184, 195, 210, 255);
        private static readonly Color32 DisabledPlate = new Color32(105, 119, 144, 255);
        private static readonly Color32 DisabledSymbol = new Color32(190, 201, 213, 255);
        private static readonly Color32 Focused = new Color32(255, 245, 220, 255);
        private Image plate;
        private Image rim;
        private Image symbol;
        private MarkerArt art;
        private Sprite entitySprite;
        private Color32 accent;
        private bool hasHover;
        private Image arrow;
        private Image padlock;
        private Plugin owner;

        internal RectTransform Rect { get; private set; }
        internal MarkerTarget Target { get; private set; }
        internal MarkerState State { get; private set; }

        internal static MarkerBadge Create(Plugin owner, Transform parent, MarkerTarget target, MarkerArt art)
        {
            var go = new GameObject("Scout’s Map " + target.Kind, typeof(RectTransform), typeof(Image));
            go.SetActive(false);
            var plate = go.GetComponent<Image>();
            var frame = art.Frame(target.Icon);
            plate.sprite = frame.Plate;
            go.transform.SetParent(parent, false);
            var badge = go.AddComponent<MarkerBadge>();
            badge.plate = plate;
            badge.Rect = plate.rectTransform;
            badge.Rect.sizeDelta = Vector2.one * MarkerLayout.BadgeSize;
            badge.rim = Child(plate.transform, "State outline", frame.Rim, MarkerLayout.BadgeSize);
            badge.entitySprite = target.EntitySprite;
            badge.symbol = Child(plate.transform, "Encounter", badge.entitySprite ?? art.Symbol(target.Icon, true), 19f);
            badge.accent = frame.Accent;
            badge.arrow = Child(plate.transform, "Bearing", art.Arrow, 8f);
            badge.padlock = Child(plate.transform, "Unavailable", art.Lock, 9f);
            badge.padlock.rectTransform.anchoredPosition = new Vector2(9f, -9f);
            badge.Target = target;
            badge.art = art;
            badge.owner = owner;
            return badge;
        }

        private static Image Child(Transform parent, string name, Sprite sprite, float size)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            child.transform.SetParent(parent, false);
            child.rectTransform.sizeDelta = Vector2.one * size;
            child.sprite = sprite;
            child.preserveAspect = true;
            child.raycastTarget = false;
            return child;
        }

        internal void Show(MarkerState state, Vector2 position, Vector2 bearing)
        {
            if (State != state)
            {
                State = state;
                bool available = state == MarkerState.Available;
                plate.color = available ? Color.white : (Color)DisabledPlate;
                symbol.color = available ? Color.white : (Color)DisabledSymbol;
                symbol.sprite = entitySprite != null ? entitySprite : art.Symbol(Target.Icon, available);
                RefreshOutline();
                padlock.gameObject.SetActive(!available);
            }
            Rect.anchoredPosition = position;
            bearing.Normalize();
            symbol.rectTransform.anchoredPosition = -bearing * 2f;
            arrow.rectTransform.anchoredPosition = bearing * 11.5f;
            arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(bearing.y, bearing.x) * Mathf.Rad2Deg);
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
        }

        internal void SetHover(bool value)
        {
            if (hasHover == value)
                return;
            hasHover = value;
            RefreshOutline();
        }

        private void RefreshOutline()
        {
            if (State == MarkerState.Available)
            {
                rim.color = hasHover ? Focused : accent;
                arrow.color = rim.color;
                return;
            }

            rim.color = hasHover ? DisabledFocus : DisabledRim;
            arrow.color = DisabledRim;
        }

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera) =>
            GamepadManager.Instance == null || !GamepadManager.Instance.IsGamepadInUse();

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                eventData.Use();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left ||
                !IsRaycastLocationValid(eventData.position, eventData.pressEventCamera))
                return;
            eventData.Use();
            owner.Focus(Target);
        }

        public void OnPointerEnter(PointerEventData eventData) => owner.Hover(this);
        public void OnPointerExit(PointerEventData eventData) => owner.EndHover(this);
        private void OnDisable() => owner.EndHover(this);
    }
}
