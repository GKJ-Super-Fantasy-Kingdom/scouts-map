using System;
using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using SuperFantasyKingdom;
using SuperFantasyKingdom.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ScoutsMap
{
    [BepInPlugin(Id, "Scout's Map", "0.1.0")]
    public sealed class Plugin : BaseUnityPlugin
    {
        private const string Id = "gkj.scoutsmap";

        private static readonly AccessTools.FieldRef<UIManager, CanvasGroup> MarkerContainer =
            AccessTools.FieldRefAccess<UIManager, CanvasGroup>("markerContainer");
        private static readonly AccessTools.FieldRef<TooltipManager, TextMeshProUGUI> TooltipText =
            AccessTools.FieldRefAccess<TooltipManager, TextMeshProUGUI>("text");

        private static Plugin instance;
        private Harmony harmony;
        private MarkerArt art;
        private RectTransform root;
        private CanvasGroup host;
        private Camera uiCamera;
        private float rescanAt;
        private readonly List<MarkerTarget> markers = new List<MarkerTarget>();
        private readonly HashSet<int> discovered = new HashSet<int>();
        private readonly List<MarkerPoint> occupied = new List<MarkerPoint>();
        private readonly Vector3[] corners = new Vector3[4];
        private readonly List<RaycastResult> hits = new List<RaycastResult>();

        private MarkerBadge hovered;
        private MarkerState tooltipState;
        private RectTransform tooltip;
        private TextMeshProUGUI tooltipLabel;

        private void Awake()
        {
            harmony = new Harmony(Id);
            try
            {
                art = new MarkerArt();
                harmony.PatchAll(typeof(Plugin).Assembly);
                instance = this;
                Logger.LogInfo("Scout's Map ready.");
            }
            catch (Exception error)
            {
                harmony.UnpatchSelf();
                art?.Dispose();
                art = null;
                enabled = false;
                Logger.LogError($"Scout's Map could not start: {error}");
            }
        }

        private void OnDestroy()
        {
            instance = null;
            harmony?.UnpatchSelf();
            foreach (var marker in markers)
                marker.ReplaceNativeImage(false);
            if (root != null)
                Destroy(root.gameObject);
            art?.Dispose();
        }

        private static bool CanShow()
        {
            var game = GameManager.Instance;
            var ui = UIManager.Instance;
            var camera = GameCamera.Instance;
            return game != null && game.IsGameRunning() && !game.IsGameOver() &&
                ui != null && !ui.IsHiddenByUser() && ui.IsWorldInteractable() &&
                (!game.IsForcedPause() || ui.IsInRoadMode()) &&
                !ui.DisplayAnyControlsEnabled() &&
                camera != null && !camera.IsCutscene() && !camera.IsMoving() && !DialogueManager.Instance.IsInDialogue();
        }

        private void LateUpdate()
        {
            if (!CanShow() || !BindFrame())
            {
                if (root != null && root.gameObject.activeSelf)
                    root.gameObject.SetActive(false);
                return;
            }
            if (Time.unscaledTime >= rescanAt)
            {
                RefreshTargets();
                rescanAt = Time.unscaledTime + 0.25f;
            }

            if (!root.gameObject.activeSelf)
                root.gameObject.SetActive(true);
            var camera = GameCamera.Instance.GetCamera();
            if (camera == null)
                return;

            var frame = GetFrame();
            Vector2 origin = RectTransformUtility.WorldToScreenPoint(uiCamera, root.position);
            Vector2 unit = RectTransformUtility.WorldToScreenPoint(uiCamera, root.TransformPoint(Vector3.right));
            float scale = (unit - origin).magnitude;
            if (scale <= 0f)
                return;

            occupied.Clear();
            for (int index = 0; index < markers.Count;)
            {
                var marker = markers[index];
                if (marker.Expired)
                {
                    if (marker.Badge != null)
                        Destroy(marker.Badge.gameObject);
                    if (marker.Kind >= MarkerKind.Road)
                        discovered.Remove(marker.InstanceId);
                    markers.RemoveAt(index);
                    continue;
                }
                DrawMarker(marker, frame, camera, scale);
                index++;
            }
            DrawTooltip();
        }

        private bool BindFrame()
        {
            CanvasGroup nextHost = MarkerContainer(UIManager.Instance);
            if (nextHost == null || !nextHost.gameObject.activeInHierarchy)
                return false;
            if (root != null && host == nextHost)
                return true;
            var canvas = nextHost.GetComponentInParent<Canvas>();
            if (canvas == null || canvas.GetComponent<GraphicRaycaster>() == null)
                return false;
            if (root != null)
                Destroy(root.gameObject);

            host = nextHost;
            uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            root = new GameObject("Scout's Map", typeof(RectTransform)).GetComponent<RectTransform>();
            // Leave the native CanvasGroup's alpha and raycast settings alone.
            root.SetParent(host.transform.parent, false);
            root.SetSiblingIndex(host.transform.GetSiblingIndex() + 1);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;
            rescanAt = 0f;
            return true;
        }

        private MarkerBounds GetFrame()
        {
            ((RectTransform)host.transform).GetWorldCorners(corners);
            Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[0]);
            Vector2 topRight = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[2]);
            Rect screen = Screen.safeArea;
            return new MarkerBounds(Mathf.Max(screen.xMin, bottomLeft.x), Mathf.Max(screen.yMin, bottomLeft.y),
                Mathf.Min(screen.xMax, topRight.x), Mathf.Min(screen.yMax, topRight.y));
        }

        private void RefreshTargets()
        {
            if (RoadBuilder.Instance != null)
            {
                foreach (Transform child in UIManager.Instance.GetRoadButtons())
                {
                    if (child.TryGetComponent(out UIArrowRoad arrow) &&
                        !discovered.Contains(arrow.GetInstanceID()))
                        Track(MarkerTarget.ForRoad(arrow, RoadBuilder.Instance));
                }
            }
            if (WorldManager.Instance != null)
            {
                foreach (var building in WorldManager.Instance.GetBuildingsWorld())
                {
                    if (building != null && !discovered.Contains(building.GetInstanceID()))
                        Track(MarkerTarget.ForShop(building));
                }
            }
        }

        private void Track(MarkerTarget target)
        {
            if (target == null)
                return;
            discovered.Add(target.InstanceId);
            markers.Add(target);
        }

        private void DrawMarker(MarkerTarget marker, MarkerBounds frame, Camera camera, float scale)
        {
            marker.ReplaceNativeImage(true);
            MarkerState state = marker.Kind < MarkerKind.Road && host.alpha <= 0.01f
                ? MarkerState.Hidden : marker.Read();
            if (state != MarkerState.Hidden)
            {
                Vector3 target = camera.WorldToScreenPoint(marker.Position);
                if (target.z > 0f && MarkerLayout.TryPlace(frame, target.x, target.y, Screen.width / 2f,
                    Screen.height / 2f, scale, occupied, out var position))
                {
                    var screen = new Vector2(position.X, position.Y);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, uiCamera, out var local);
                    if (marker.Badge == null)
                        marker.Badge = MarkerBadge.Create(this, root, marker, art);
                    marker.Badge.Show(state, local, (Vector2)target - screen);
                    return;
                }
            }
            if (marker.Badge != null && marker.Badge.gameObject.activeSelf)
                marker.Badge.gameObject.SetActive(false);
        }

        internal void Focus(MarkerTarget marker)
        {
            if (CanShow() && marker.Read() == MarkerState.Available)
                GameCamera.Instance.SetPosition(marker.Position);
        }

        internal void Hover(MarkerBadge badge)
        {
            hovered = badge;
            tooltipState = MarkerState.Hidden;
        }

        internal void EndHover(MarkerBadge badge)
        {
            if (hovered != badge)
                return;
            hovered = null;
            if (tooltip != null)
                tooltip.gameObject.SetActive(false);
        }

        private void DrawTooltip()
        {
            if (hovered == null)
                return;
            if (GamepadManager.Instance != null && GamepadManager.Instance.IsGamepadInUse())
            {
                EndHover(hovered);
                return;
            }
            if (tooltip == null)
            {
                if (TooltipManager.Instance == null)
                    return;
                var native = TooltipText(TooltipManager.Instance);
                if (native == null || native.font == null)
                    return;
                var background = new GameObject("Scout's Map tooltip", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                background.transform.SetParent(root, false);
                background.color = new Color32(20, 25, 32, 245);
                background.raycastTarget = false;
                tooltip = background.rectTransform;
                tooltipLabel = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
                tooltipLabel.transform.SetParent(tooltip, false);
                tooltipLabel.font = native.font;
                tooltipLabel.spriteAsset = native.spriteAsset;
                tooltipLabel.fontSize = 14f;
                tooltipLabel.color = new Color32(255, 243, 213, 255);
                tooltipLabel.alignment = TextAlignmentOptions.Center;
                tooltipLabel.raycastTarget = false;
                tooltipLabel.rectTransform.anchorMin = Vector2.zero;
                tooltipLabel.rectTransform.anchorMax = Vector2.one;
                tooltipLabel.rectTransform.offsetMin = new Vector2(8f, 4f);
                tooltipLabel.rectTransform.offsetMax = new Vector2(-8f, -4f);
                tooltipState = MarkerState.Hidden;
            }
            if (hovered.State != tooltipState)
            {
                tooltipState = hovered.State;
                tooltipLabel.text = hovered.Target.GetTooltip(tooltipState);
                float maxWidth = Mathf.Min(250f, root.rect.width - 24f);
                float width = Mathf.Clamp(tooltipLabel.GetPreferredValues().x, 60f, maxWidth);
                float height = tooltipLabel.GetPreferredValues(tooltipLabel.text, width, 0f).y;
                tooltip.sizeDelta = new Vector2(width + 16f, height + 8f);
            }
            Vector2 position = hovered.Rect.anchoredPosition;
            float halfWidth = tooltip.sizeDelta.x / 2f;
            float halfHeight = tooltip.sizeDelta.y / 2f;
            float distance = MarkerLayout.BadgeSize / 2f + 6f + halfHeight;
            position.x = Mathf.Clamp(position.x, root.rect.xMin + halfWidth, root.rect.xMax - halfWidth);
            position.y += position.y > 0f ? -distance : distance;
            tooltip.anchoredPosition = position;
            tooltip.SetAsLastSibling();
            if (!tooltip.gameObject.activeSelf)
                tooltip.gameObject.SetActive(true);
        }

        private bool BadgeAt(Vector2 screen)
        {
            var eventSystem = EventSystem.current;
            if (root == null || !root.gameObject.activeInHierarchy || eventSystem == null || !CanShow())
                return false;
            var data = new PointerEventData(eventSystem) { position = screen };
            hits.Clear();
            eventSystem.RaycastAll(data, hits);
            return hits.Count > 0 && hits[0].gameObject.TryGetComponent<MarkerBadge>(out _);
        }

        [HarmonyPatch(typeof(UIMarker), nameof(UIMarker.Init), typeof(GameObject), typeof(Sprite))]
        private static class NativeEventMarker
        {
            private static void Postfix(UIMarker __instance, GameObject o, Sprite icon, Image ___image)
            {
                if (instance == null || o == null || icon != null || ___image == null)
                    return;
                var marker = MarkerTarget.ForEvent(__instance, ___image, o);
                if (marker != null)
                    instance.markers.Insert(0, marker);
            }
        }

        [HarmonyPatch(typeof(GameInputManager), nameof(GameInputManager.GameClick), typeof(InputAction.CallbackContext))]
        private static class MouseSpellClick
        {
            private static bool Prefix(InputAction.CallbackContext context) =>
                instance == null || !context.performed || !(context.control?.device is Mouse mouse) ||
                (VirtualInputManager.Instance != null && VirtualInputManager.Instance.IsVirtualInput(context)) || !instance.BadgeAt(mouse.position.ReadValue());
        }

        [HarmonyPatch(typeof(GameCamera), "Update")]
        private static class CameraDrag
        {
            private static bool Prefix(GameCamera __instance, ref Vector3 ___m_DragOrigin)
            {
                if (instance == null || __instance.IsMoving() || !Input.GetMouseButtonDown(0) ||
                    !instance.BadgeAt(Input.mousePosition))
                    return true;
                ___m_DragOrigin = Vector3.zero;
                return false;
            }
        }
    }
}
