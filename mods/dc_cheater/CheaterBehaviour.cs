using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using DCLoader;
using DCLoader.Config;

namespace DCCheater
{
    // Cheat panel — F9 to toggle. Money/rep/XP fields with apply buttons.
    public class CheaterBehaviour : MonoBehaviour
    {
        // IL2CPP needs this
        public CheaterBehaviour(IntPtr ptr) : base(ptr) { }

        private ConfigEntry<string> _toggleKey;
        private GameObject _panelGO;
        private TMP_InputField _moneyInput;
        private TMP_InputField _repInput;
        private TMP_InputField _xpInput;
        private bool _panelVisible = false;

        public void Initialize(ConfigEntry<string> toggleKey)
        {
            _toggleKey = toggleKey;
        }

        private void Start()
        {
            BuildPanel();
            _panelGO?.SetActive(false);
        }

        private void Update()
        {
            if (_toggleKey == null || Keyboard.current == null) return;

            // map config string to actual key — not pretty but it works
            bool pressed = false;
            try
            {
                string keyName = (_toggleKey.Value ?? "F9").Trim().ToLowerInvariant();
                pressed = keyName switch
                {
                    "f1"  => Keyboard.current.f1Key.wasPressedThisFrame,
                    "f2"  => Keyboard.current.f2Key.wasPressedThisFrame,
                    "f3"  => Keyboard.current.f3Key.wasPressedThisFrame,
                    "f4"  => Keyboard.current.f4Key.wasPressedThisFrame,
                    "f5"  => Keyboard.current.f5Key.wasPressedThisFrame,
                    "f6"  => Keyboard.current.f6Key.wasPressedThisFrame,
                    "f7"  => Keyboard.current.f7Key.wasPressedThisFrame,
                    "f8"  => Keyboard.current.f8Key.wasPressedThisFrame,
                    "f9"  => Keyboard.current.f9Key.wasPressedThisFrame,
                    "f10" => Keyboard.current.f10Key.wasPressedThisFrame,
                    "f11" => Keyboard.current.f11Key.wasPressedThisFrame,
                    "f12" => Keyboard.current.f12Key.wasPressedThisFrame,
                    _     => false
                };
            }
            catch { }

            if (pressed) TogglePanel();
        }

        // ─── Panel Toggle ────────────────────────────────────────────────────────

        public void TogglePanel()
        {
            if (_panelGO == null) return;
            _panelVisible = !_panelVisible;
            _panelGO.SetActive(_panelVisible);

            if (_panelVisible)
            {
                try { InputManager.ConfinedCursorforUI(); }
                catch
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }
            else
            {
                try { InputManager.LockedCursorForPlayerMovement(); }
                catch
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }

        // ─── UGUI Construction ───────────────────────────────────────────────────

        private void BuildPanel()
        {
            var canvasGO = new GameObject("DCCheater_Canvas");
            canvasGO.transform.SetParent(transform, false);

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            canvasGO.AddComponent<GraphicRaycaster>();

            // Main panel — 300x220 px, centered
            _panelGO = new GameObject("Panel");
            _panelGO.transform.SetParent(canvasGO.transform, false);

            var panelRT = _panelGO.AddComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.pivot = new Vector2(0.5f, 0.5f);
            panelRT.sizeDelta = new Vector2(320f, 240f);
            panelRT.anchoredPosition = Vector2.zero;

            var bg = _panelGO.AddComponent<Image>();
            bg.sprite = MakePixelSprite();
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

            AddLabel(_panelGO.transform, "DC Cheater",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, -35f), new Vector2(0f, 0f),
                fontSize: 18, bold: true);

            _moneyInput = AddRow(_panelGO.transform, "Money:", 0, () =>
            {
                if (float.TryParse(_moneyInput.text, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float v))
                {
                    var p = GameAPI.Player;
                    if (p != null) p.money = v;
                }
            });

            _repInput = AddRow(_panelGO.transform, "Rep:", 1, () =>
            {
                if (float.TryParse(_repInput.text, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float v))
                {
                    var p = GameAPI.Player;
                    if (p != null) p.reputation = v;
                }
            });

            _xpInput = AddRow(_panelGO.transform, "XP:", 2, () =>
            {
                if (float.TryParse(_xpInput.text, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float v))
                {
                    var p = GameAPI.Player;
                    if (p != null) p.xp = v;
                }
            });

            AddCloseButton(_panelGO.transform);
        }

        // ─── Panel Component Builders ─────────────────────────────────────────────

        private TMP_InputField AddRow(Transform parent, string labelText, int rowIndex, Action onApply)
        {
            float rowHeight = 45f;
            float startY = -50f; // below title
            float y = startY - rowIndex * (rowHeight + 8f);

            var rowGO = new GameObject($"Row_{labelText}");
            rowGO.transform.SetParent(parent, false);

            var rowRT = rowGO.AddComponent<RectTransform>();
            rowRT.anchorMin = new Vector2(0f, 1f);
            rowRT.anchorMax = new Vector2(1f, 1f);
            rowRT.pivot = new Vector2(0.5f, 1f);
            rowRT.offsetMin = new Vector2(12f, y - rowHeight);
            rowRT.offsetMax = new Vector2(-12f, y);

            // Label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(rowGO.transform, false);
            var labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.anchorMin = new Vector2(0f, 0f);
            labelRT.anchorMax = new Vector2(0.28f, 1f);
            labelRT.offsetMin = Vector2.zero;
            labelRT.offsetMax = Vector2.zero;
            var label = labelGO.AddComponent<TextMeshProUGUI>();
            label.text = labelText;
            label.fontSize = 14;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.MidlineRight;

            // Input field
            var inputGO = new GameObject("Input");
            inputGO.transform.SetParent(rowGO.transform, false);
            var inputRT = inputGO.AddComponent<RectTransform>();
            inputRT.anchorMin = new Vector2(0.3f, 0f);
            inputRT.anchorMax = new Vector2(0.72f, 1f);
            inputRT.offsetMin = new Vector2(4f, 2f);
            inputRT.offsetMax = new Vector2(-4f, -2f);

            // Input field background
            var inputBg = inputGO.AddComponent<Image>();
            inputBg.sprite = MakePixelSprite();
            inputBg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            var inputField = inputGO.AddComponent<TMP_InputField>();
            inputField.contentType = TMP_InputField.ContentType.DecimalNumber;

            var textAreaGO = new GameObject("Text Area");
            textAreaGO.transform.SetParent(inputGO.transform, false);
            var textAreaRT = textAreaGO.AddComponent<RectTransform>();
            textAreaRT.anchorMin = Vector2.zero;
            textAreaRT.anchorMax = Vector2.one;
            textAreaRT.offsetMin = new Vector2(4f, 2f);
            textAreaRT.offsetMax = new Vector2(-4f, -2f);
            var mask = textAreaGO.AddComponent<RectMask2D>();

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(textAreaGO.transform, false);
            var textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
            var textComp = textGO.AddComponent<TextMeshProUGUI>();
            textComp.fontSize = 13;
            textComp.color = Color.white;

            var placeholderGO = new GameObject("Placeholder");
            placeholderGO.transform.SetParent(textAreaGO.transform, false);
            var placeholderRT = placeholderGO.AddComponent<RectTransform>();
            placeholderRT.anchorMin = Vector2.zero;
            placeholderRT.anchorMax = Vector2.one;
            placeholderRT.offsetMin = Vector2.zero;
            placeholderRT.offsetMax = Vector2.zero;
            var placeholderComp = placeholderGO.AddComponent<TextMeshProUGUI>();
            placeholderComp.text = "Enter value...";
            placeholderComp.fontSize = 12;
            placeholderComp.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            placeholderComp.fontStyle = FontStyles.Italic;

            inputField.textComponent = textComp;
            inputField.placeholder = placeholderComp;
            inputField.textViewport = textAreaRT;

            // Apply button
            var btnGO = new GameObject("ApplyBtn");
            btnGO.transform.SetParent(rowGO.transform, false);
            var btnRT = btnGO.AddComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.74f, 0f);
            btnRT.anchorMax = new Vector2(1f, 1f);
            btnRT.offsetMin = new Vector2(4f, 2f);
            btnRT.offsetMax = new Vector2(0f, -2f);

            var btnImg = btnGO.AddComponent<Image>();
            btnImg.sprite = MakePixelSprite();
            btnImg.color = new Color(0.2f, 0.5f, 0.2f, 1f);

            var btn = btnGO.AddComponent<Button>();

            var btnTextGO = new GameObject("Text");
            btnTextGO.transform.SetParent(btnGO.transform, false);
            var btnTextRT = btnTextGO.AddComponent<RectTransform>();
            btnTextRT.anchorMin = Vector2.zero;
            btnTextRT.anchorMax = Vector2.one;
            btnTextRT.offsetMin = Vector2.zero;
            btnTextRT.offsetMax = Vector2.zero;
            var btnText = btnTextGO.AddComponent<TextMeshProUGUI>();
            btnText.text = "Apply";
            btnText.fontSize = 12;
            btnText.color = Color.white;
            btnText.alignment = TextAlignmentOptions.Center;

            btn.onClick.AddListener((UnityEngine.Events.UnityAction)(() => onApply()));

            return inputField;
        }

        private void AddLabel(Transform parent, string text,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 offsetMin, Vector2 offsetMax,
            float fontSize = 14, bool bold = false)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            if (bold) tmp.fontStyle = FontStyles.Bold;
        }

        private void AddCloseButton(Transform parent)
        {
            var btnGO = new GameObject("CloseBtn");
            btnGO.transform.SetParent(parent, false);
            var btnRT = btnGO.AddComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.5f, 0f);
            btnRT.anchorMax = new Vector2(0.5f, 0f);
            btnRT.pivot = new Vector2(0.5f, 0f);
            btnRT.sizeDelta = new Vector2(80f, 28f);
            btnRT.anchoredPosition = new Vector2(0f, 10f);

            var btnImg = btnGO.AddComponent<Image>();
            btnImg.sprite = MakePixelSprite();
            btnImg.color = new Color(0.5f, 0.15f, 0.15f, 1f);

            var btn = btnGO.AddComponent<Button>();
            btn.onClick.AddListener((UnityEngine.Events.UnityAction)(() => TogglePanel()));

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(btnGO.transform, false);
            var textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = "Close";
            tmp.fontSize = 13;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
        }

        // ─── Utilities ──────────────────────────────────────────────────────────

        private static Sprite MakePixelSprite()
        {
            var tex = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            return Sprite.Create(tex,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f));
        }
    }
}
