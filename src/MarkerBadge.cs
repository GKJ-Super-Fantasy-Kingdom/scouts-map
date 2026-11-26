using SuperFantasyKingdom;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ScoutsMap
{
    internal sealed class MarkerBadge : MonoBehaviour, IPointerDownHandler, IPointerClickHandler,
        IPointerEnterHandler, IPointerExitHandler, ICanvasRaycastFilter
    {
        private static readonly Color32 Ready = new Color32(255, 225, 151, 255);
        private static readonly Color32 Blocked = new Color32(157, 167, 179, 255);
        private Image symbol;
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
            plate.sprite = art.Plate;
            go.transform.SetParent(parent, false);
            var badge = go.AddComponent<MarkerBadge>();
            badge.Rect = plate.rectTransform;
            badge.Rect.sizeDelta = Vector2.one * MarkerLayout.BadgeSize;
            badge.symbol = Child(plate.transform, "Encounter", art.Symbol(target.Kind), 16f);
            badge.arrow = Child(plate.transform, "Bearing", art.Arrow, 9f);
            badge.padlock = Child(plate.transform, "Unavailable", art.Lock, 10f);
            badge.padlock.rectTransform.anchoredPosition = new Vector2(9f, -9f);
            badge.Target = target;
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
                symbol.color = arrow.color = state == MarkerState.Available ? Ready : Blocked;
                padlock.gameObject.SetActive(state != MarkerState.Available);
            }
            Rect.anchoredPosition = position;
            bearing.Normalize();
            arrow.rectTransform.anchoredPosition = bearing * 11f;
            arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(bearing.y, bearing.x) * Mathf.Rad2Deg);
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
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
