using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using Il2CppInterop.Runtime.Injection;
using DCLoader.Core;
using DCLoader.Config;

namespace DCLoader.Core.UI
{
    // Mod menu panel, toggled with F10. Shows all mods with enable/disable toggles + settings.
    public class ModMenuBehaviour : MonoBehaviour
    {
        // IL2CPP needs this or it explodes
        public ModMenuBehaviour(IntPtr ptr) : base(ptr) { }

        private Canvas _canvas;
        private GameObject _panelGO;
        private Transform _contentTransform;
        private bool _isOpen = false;

        private void Start()
        {
            BuildCanvas();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f10Key.wasPressedThisFrame)
                ToggleMenu();
        }

        // ─── Canvas Construction ────────────────────────────────────────────────

        private void BuildCanvas()
        {
            var canvasGO = new GameObject("DCLoader_ModMenu");
            canvasGO.transform.SetParent(transform, false);

            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 9999;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            // uses game's EventSystem -- don't create a second one
            canvasGO.AddComponent<GraphicRaycaster>();

            canvasGO.SetActive(false);

            BuildBlocker(canvasGO.transform);
            BuildPanel(canvasGO.transform);
        }

        private void BuildBlocker(Transform canvasTransform)
        {
            // full-screen invisible button -- click outside panel to close
            var blockerGO = new GameObject("Blocker");
            blockerGO.transform.SetParent(canvasTransform, false);

            var rt = blockerGO.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = blockerGO.AddComponent<Image>();
            img.sprite = MakePixelSprite();
            img.color = new Color(0f, 0f, 0f, 0f); // invisible, just catches clicks

            var btn = blockerGO.AddComponent<Button>();
            btn.onClick.AddListener((UnityEngine.Events.UnityAction)(() => ToggleMenu()));

            blockerGO.transform.SetSiblingIndex(0); // panel renders on top
        }

        private void BuildPanel(Transform canvasTransform)
        {
            _panelGO = new GameObject("Panel");
            _panelGO.transform.SetParent(canvasTransform, false);

            var panelRT = _panelGO.AddComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0.2f, 0.15f);
            panelRT.anchorMax = new Vector2(0.8f, 0.85f);
            panelRT.offsetMin = Vector2.zero;
            panelRT.offsetMax = Vector2.zero;

            var bg = _panelGO.AddComponent<Image>();
            bg.sprite = MakePixelSprite();
            bg.color = new Color(0.05f, 0.05f, 0.1f, 0.92f); // dark blue-black, nearly opaque

            BuildTitleBar(_panelGO.transform);
            BuildScrollArea(_panelGO.transform);
        }

        private void BuildTitleBar(Transform panelTransform)
        {
            var titleBarGO = new GameObject("TitleBar");
            titleBarGO.transform.SetParent(panelTransform, false);

            var rt = titleBarGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(0f, -50f);
            rt.offsetMax = Vector2.zero;

            var titleGO = new GameObject("TitleText");
            titleGO.transform.SetParent(titleBarGO.transform, false);

            var titleRT = titleGO.AddComponent<RectTransform>();
            titleRT.anchorMin = Vector2.zero;
            titleRT.anchorMax = Vector2.one;
            titleRT.offsetMin = Vector2.zero;
            titleRT.offsetMax = Vector2.zero;

            var title = titleGO.AddComponent<TextMeshProUGUI>();
            title.text = "DC Loader - Mod Menu";
            title.fontSize = 22;
            title.color = Color.white;
            title.alignment = TextAlignmentOptions.Center;
        }

        private void BuildScrollArea(Transform panelTransform)
        {
            var scrollGO = new GameObject("ScrollView");
            scrollGO.transform.SetParent(panelTransform, false);

            var scrollRT = scrollGO.AddComponent<RectTransform>();
            scrollRT.anchorMin = Vector2.zero;
            scrollRT.anchorMax = Vector2.one;
            scrollRT.offsetMin = Vector2.zero;
            scrollRT.offsetMax = new Vector2(0f, -55f);

            var scrollRect = scrollGO.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            var viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(scrollGO.transform, false);

            var viewportRT = viewportGO.AddComponent<RectTransform>();
            viewportRT.anchorMin = Vector2.zero;
            viewportRT.anchorMax = Vector2.one;
            viewportRT.offsetMin = Vector2.zero;
            viewportRT.offsetMax = Vector2.zero;

            var viewportImg = viewportGO.AddComponent<Image>();
            viewportImg.sprite = MakePixelSprite();
            viewportImg.color = Color.white;
            viewportGO.AddComponent<Mask>().showMaskGraphic = false;

            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(viewportGO.transform, false);

            var contentRT = contentGO.AddComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0f, 1f);
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.pivot = new Vector2(0.5f, 1f);
            contentRT.offsetMin = Vector2.zero;
            contentRT.offsetMax = Vector2.zero;

            var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 2f;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.padding.left = 8;
            vlg.padding.right = 8;
            vlg.padding.top = 8;
            vlg.padding.bottom = 8;

            var csf = contentGO.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRT;
            scrollRect.viewport = viewportRT;
            _contentTransform = contentGO.transform;
        }

        // ─── Menu Toggle ────────────────────────────────────────────────────────

        private void ToggleMenu()
        {
            _isOpen = !_isOpen;
            _canvas.gameObject.SetActive(_isOpen);

            if (_isOpen)
            {
                try
                {
                    InputManager.ConfinedCursorforUI();
                }
                catch (Exception)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }

                try
                {
                    InputManager.inputActions.Player.Disable();
                }
                catch (Exception ex)
                {
                    DcLogger.Warn("ModMenu",
                        $"[ModMenu] Could not disable Player action map: {ex.Message}");
                }

                PopulateModList();
            }
            else
            {
                try
                {
                    InputManager.LockedCursorForPlayerMovement();
                }
                catch (Exception)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }

                try
                {
                    InputManager.inputActions.Player.Enable();
                }
                catch (Exception ex)
                {
                    DcLogger.Warn("ModMenu",
                        $"[ModMenu] Could not re-enable Player action map: {ex.Message}");
                }
            }
        }

        // ─── Helpers ────────────────────────────────────────────────────────────

        // IL2CPP can't access the built-in UISprite, so we make our own 1x1 white pixel.
        // Tint with Image.color for solid rectangles.
        private static Sprite MakePixelSprite()
        {
            return Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
        }

        // ─── Mod List Population ────────────────────────────────────────────────

        private void PopulateModList()
        {
            // nuke old rows
            for (int i = _contentTransform.childCount - 1; i >= 0; i--)
            {
                var child = _contentTransform.GetChild(i);
                if (child != null)
                    UnityEngine.Object.Destroy(child.gameObject);
            }

            var mods = ModRegistry.Instance?.LoadOrder;
            if (mods == null || mods.Count == 0)
            {
                var emptyGO = new GameObject("EmptyLabel");
                emptyGO.transform.SetParent(_contentTransform, false);

                var emptyRT = emptyGO.AddComponent<RectTransform>();
                emptyRT.anchorMin = Vector2.zero;
                emptyRT.anchorMax = Vector2.one;
                emptyRT.offsetMin = Vector2.zero;
                emptyRT.offsetMax = Vector2.zero;

                var emptyLabel = emptyGO.AddComponent<TextMeshProUGUI>();
                emptyLabel.text = "No mods found. Place mod DLLs in Mods/ folder.";
                emptyLabel.fontSize = 16;
                emptyLabel.color = new Color(0.7f, 0.7f, 0.7f, 1f);
                emptyLabel.alignment = TextAlignmentOptions.Center;
                return;
            }

            foreach (var entry in mods)
            {
                AddModRow(entry);
                if (entry.ConfigFile?.Entries?.Count > 0)
                    BuildSettingsPanel(_contentTransform, entry);
            }
        }

        private void AddModRow(ModEntry entry)
        {
            var rowGO = new GameObject($"Row_{entry.Info.ID}");
            rowGO.transform.SetParent(_contentTransform, false);

            var rowLE = rowGO.AddComponent<LayoutElement>();
            rowLE.preferredHeight = 36f;

            var rowBg = rowGO.AddComponent<Image>();
            rowBg.sprite = MakePixelSprite();
            rowBg.color = new Color(0.12f, 0.12f, 0.18f, 0.8f);

            var hlg = rowGO.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10f;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.padding.left = 8;
            hlg.padding.right = 8;
            hlg.padding.top = 2;
            hlg.padding.bottom = 2;

            bool isEnabled = entry.Enabled;
            float textAlpha = isEnabled ? 1f : 0.4f;

            var nameLabel = CreateTMPLabel(rowGO.transform, entry.Info.Name, 15, Color.white, 200f);
            nameLabel.color = new Color(1f, 1f, 1f, textAlpha);

            var versionLabel = CreateTMPLabel(rowGO.transform, $"v{entry.Info.Version}", 13,
                new Color(0.6f, 0.8f, 0.6f, 1f), 80f);
            versionLabel.color = new Color(0.6f, 0.8f, 0.6f, textAlpha);

            var authorLabel = CreateTMPLabel(rowGO.transform, entry.Info.Author, 13,
                new Color(0.7f, 0.7f, 0.7f, 1f), 150f);
            authorLabel.color = new Color(0.7f, 0.7f, 0.7f, textAlpha);

            var spacerGO = new GameObject("Spacer");
            spacerGO.transform.SetParent(rowGO.transform, false);
            var spacerLE = spacerGO.AddComponent<LayoutElement>();
            spacerLE.flexibleWidth = 1f;

            BuildToggle(rowGO.transform, entry, nameLabel, versionLabel, authorLabel);

            if (entry.ConfigFile?.Entries?.Count > 0)
            {
                var settingsBtnGO = new GameObject("SettingsButton");
                settingsBtnGO.transform.SetParent(rowGO.transform, false);

                var btnLE = settingsBtnGO.AddComponent<LayoutElement>();
                btnLE.preferredWidth = 70f;
                btnLE.preferredHeight = 26f;

                var btnBg = settingsBtnGO.AddComponent<Image>();
                btnBg.sprite = MakePixelSprite();
                btnBg.color = new Color(0.2f, 0.4f, 0.6f, 1f);

                var btnLabelGO = new GameObject("Label");
                btnLabelGO.transform.SetParent(settingsBtnGO.transform, false);

                var btnLabelRT = btnLabelGO.AddComponent<RectTransform>();
                btnLabelRT.anchorMin = Vector2.zero;
                btnLabelRT.anchorMax = Vector2.one;
                btnLabelRT.offsetMin = Vector2.zero;
                btnLabelRT.offsetMax = Vector2.zero;

                var btnLabel = btnLabelGO.AddComponent<TextMeshProUGUI>();
                btnLabel.text = "Settings";
                btnLabel.fontSize = 12;
                btnLabel.alignment = TextAlignmentOptions.Center;
                btnLabel.color = Color.white;

                var btn = settingsBtnGO.AddComponent<Button>();
                string settingsPanelName = $"Settings_{entry.Info.ID}";
                btn.onClick.AddListener((UnityEngine.Events.UnityAction)(() =>
                {
                    var panelTransform = _contentTransform.Find(settingsPanelName);
                    if (panelTransform != null)
                        panelTransform.gameObject.SetActive(!panelTransform.gameObject.activeSelf);
                }));
            }
        }

        private void BuildToggle(Transform rowTransform, ModEntry entry,
            TextMeshProUGUI nameLabel, TextMeshProUGUI versionLabel, TextMeshProUGUI authorLabel)
        {
            var toggleGO = new GameObject("Toggle");
            toggleGO.transform.SetParent(rowTransform, false);

            var toggleLE = toggleGO.AddComponent<LayoutElement>();
            toggleLE.preferredWidth = 30f;
            toggleLE.preferredHeight = 30f;

            var bgImg = toggleGO.AddComponent<Image>();
            bgImg.sprite = MakePixelSprite();
            bgImg.color = entry.Enabled
                ? new Color(0.2f, 0.7f, 0.2f, 1f)   // green when on
                : new Color(0.3f, 0.3f, 0.3f, 1f);    // grey when off

            var checkmarkGO = new GameObject("Checkmark");
            checkmarkGO.transform.SetParent(toggleGO.transform, false);

            var checkRT = checkmarkGO.AddComponent<RectTransform>();
            checkRT.anchorMin = new Vector2(0.1f, 0.1f);
            checkRT.anchorMax = new Vector2(0.9f, 0.9f);
            checkRT.offsetMin = Vector2.zero;
            checkRT.offsetMax = Vector2.zero;

            var checkImg = checkmarkGO.AddComponent<Image>();
            checkImg.sprite = MakePixelSprite();
            checkImg.color = Color.white;

            var toggle = toggleGO.AddComponent<Toggle>();
            toggle.targetGraphic = bgImg;
            toggle.graphic = checkImg;
            toggle.isOn = entry.Enabled;

            string modId = entry.Info.ID;
            toggle.onValueChanged.AddListener((UnityEngine.Events.UnityAction<bool>)((value) =>
            {
                if (value)
                    ModRegistry.Instance.EnableMod(modId);
                else
                    ModRegistry.Instance.DisableMod(modId);

                float alpha = value ? 1f : 0.4f;
                nameLabel.color = new Color(1f, 1f, 1f, alpha);
                versionLabel.color = new Color(0.6f, 0.8f, 0.6f, alpha);
                authorLabel.color = new Color(0.7f, 0.7f, 0.7f, alpha);
                bgImg.color = value
                    ? new Color(0.2f, 0.7f, 0.2f, 1f)
                    : new Color(0.3f, 0.3f, 0.3f, 1f);
            }));
        }

        // ─── Settings Panel ─────────────────────────────────────────────────────

        private void BuildSettingsPanel(Transform parent, ModEntry entry)
        {
            var panelGO = new GameObject($"Settings_{entry.Info.ID}");
            panelGO.transform.SetParent(parent, false);

            var panelImg = panelGO.AddComponent<Image>();
            panelImg.sprite = MakePixelSprite();
            panelImg.color = new Color(0.08f, 0.08f, 0.12f, 0.9f);

            var vlg = panelGO.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4f;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.padding.left = 8;
            vlg.padding.right = 8;
            vlg.padding.top = 4;
            vlg.padding.bottom = 4;

            var csf = panelGO.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            panelGO.SetActive(false); // collapsed by default

            foreach (var kvp in entry.ConfigFile.Entries)
            {
                BuildSettingRow(panelGO.transform, kvp.Value, entry.ConfigFile);
            }
        }

        private void BuildSettingRow(Transform panelTransform, IConfigEntryBase e, ConfigFile configFile)
        {
            var rowGO = new GameObject($"Setting_{e.Key}");
            rowGO.transform.SetParent(panelTransform, false);

            var rowLE = rowGO.AddComponent<LayoutElement>();
            rowLE.preferredHeight = 32f;

            var hlg = rowGO.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8f;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.padding.left = 4;
            hlg.padding.right = 4;
            hlg.padding.top = 2;
            hlg.padding.bottom = 2;

            // Key label (with optional description)
            string labelText = !string.IsNullOrEmpty(e.Description)
                ? $"{e.Key}\n{e.Description}"
                : e.Key;

            var labelGO = new GameObject("KeyLabel");
            labelGO.transform.SetParent(rowGO.transform, false);

            var labelLE = labelGO.AddComponent<LayoutElement>();
            labelLE.preferredWidth = 160f;

            var labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = Vector2.zero;
            labelRT.offsetMax = Vector2.zero;

            var labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
            labelTMP.text = labelText;
            labelTMP.fontSize = 11;
            labelTMP.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            labelTMP.alignment = TextAlignmentOptions.MidlineLeft;
            labelTMP.overflowMode = TextOverflowModes.Ellipsis;

            // Control — branched on ValueType
            if (e.ValueType == typeof(bool))
            {
                BuildSettingToggle(rowGO.transform, e, configFile);
            }
            else if (e.ValueType == typeof(float) || e.ValueType == typeof(double))
            {
                BuildSettingSliderFloat(rowGO.transform, e, configFile);
            }
            else if (e.ValueType == typeof(int) || e.ValueType == typeof(long))
            {
                BuildSettingSliderInt(rowGO.transform, e, configFile);
            }
            else if (e.ValueType == typeof(string))
            {
                BuildSettingInputField(rowGO.transform, e, configFile);
            }
            else if (e.EnumValues != null)
            {
                BuildSettingDropdown(rowGO.transform, e, configFile);
            }
            else
            {
                // Fallback: read-only label
                var fallbackGO = new GameObject("FallbackLabel");
                fallbackGO.transform.SetParent(rowGO.transform, false);

                var fallbackRT = fallbackGO.AddComponent<RectTransform>();
                fallbackRT.anchorMin = Vector2.zero;
                fallbackRT.anchorMax = Vector2.one;
                fallbackRT.offsetMin = Vector2.zero;
                fallbackRT.offsetMax = Vector2.zero;

                var fallbackTMP = fallbackGO.AddComponent<TextMeshProUGUI>();
                fallbackTMP.text = e.ValueObject?.ToString() ?? "(null)";
                fallbackTMP.fontSize = 11;
                fallbackTMP.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            }
        }

        private void BuildSettingToggle(Transform rowTransform, IConfigEntryBase e, ConfigFile configFile)
        {
            var toggleGO = new GameObject("Toggle");
            toggleGO.transform.SetParent(rowTransform, false);

            var toggleLE = toggleGO.AddComponent<LayoutElement>();
            toggleLE.preferredWidth = 30f;
            toggleLE.preferredHeight = 26f;

            var bgImg = toggleGO.AddComponent<Image>();
            bgImg.sprite = MakePixelSprite();
            bool currentBool = e.ValueObject is bool b && b;
            bgImg.color = currentBool
                ? new Color(0.2f, 0.7f, 0.2f, 1f)
                : new Color(0.3f, 0.3f, 0.3f, 1f);

            var checkmarkGO = new GameObject("Checkmark");
            checkmarkGO.transform.SetParent(toggleGO.transform, false);

            var checkRT = checkmarkGO.AddComponent<RectTransform>();
            checkRT.anchorMin = new Vector2(0.1f, 0.1f);
            checkRT.anchorMax = new Vector2(0.9f, 0.9f);
            checkRT.offsetMin = Vector2.zero;
            checkRT.offsetMax = Vector2.zero;

            var checkImg = checkmarkGO.AddComponent<Image>();
            checkImg.sprite = MakePixelSprite();
            checkImg.color = Color.white;

            var toggle = toggleGO.AddComponent<Toggle>();
            toggle.targetGraphic = bgImg;
            toggle.graphic = checkImg;
            toggle.isOn = currentBool;

            toggle.onValueChanged.AddListener((UnityEngine.Events.UnityAction<bool>)((value) =>
            {
                e.ValueObject = value;
                bgImg.color = value
                    ? new Color(0.2f, 0.7f, 0.2f, 1f)
                    : new Color(0.3f, 0.3f, 0.3f, 1f);
                configFile.Save();
            }));
        }

        private void BuildSettingSliderFloat(Transform rowTransform, IConfigEntryBase e, ConfigFile configFile)
        {
            // Value display label (built first so the slider listener can reference it)
            var valueLabelGO = new GameObject("ValueLabel");
            valueLabelGO.transform.SetParent(rowTransform, false);

            var valueLabelLE = valueLabelGO.AddComponent<LayoutElement>();
            valueLabelLE.preferredWidth = 40f;

            var valueLabelRT = valueLabelGO.AddComponent<RectTransform>();
            valueLabelRT.anchorMin = Vector2.zero;
            valueLabelRT.anchorMax = Vector2.one;
            valueLabelRT.offsetMin = Vector2.zero;
            valueLabelRT.offsetMax = Vector2.zero;

            var valueLabelTMP = valueLabelGO.AddComponent<TextMeshProUGUI>();
            float currentFloat = Convert.ToSingle(e.ValueObject);
            valueLabelTMP.text = currentFloat.ToString("F2");
            valueLabelTMP.fontSize = 11;
            valueLabelTMP.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            valueLabelTMP.alignment = TextAlignmentOptions.MidlineRight;

            // Slider
            var sliderGO = new GameObject("Slider");
            sliderGO.transform.SetParent(rowTransform, false);

            var sliderLE = sliderGO.AddComponent<LayoutElement>();
            sliderLE.preferredWidth = 120f;
            sliderLE.preferredHeight = 20f;

            var sliderBg = sliderGO.AddComponent<Image>();
            sliderBg.sprite = MakePixelSprite();
            sliderBg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            // Fill area
            var fillAreaGO = new GameObject("Fill Area");
            fillAreaGO.transform.SetParent(sliderGO.transform, false);

            var fillAreaRT = fillAreaGO.AddComponent<RectTransform>();
            fillAreaRT.anchorMin = Vector2.zero;
            fillAreaRT.anchorMax = Vector2.one;
            fillAreaRT.offsetMin = new Vector2(5f, 0f);
            fillAreaRT.offsetMax = new Vector2(-15f, 0f);

            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(fillAreaGO.transform, false);

            var fillRT = fillGO.AddComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = Vector2.zero;
            fillRT.offsetMax = Vector2.zero;

            var fillImg = fillGO.AddComponent<Image>();
            fillImg.sprite = MakePixelSprite();
            fillImg.color = new Color(0.2f, 0.5f, 0.8f, 1f);

            // Handle area
            var handleAreaGO = new GameObject("Handle Slide Area");
            handleAreaGO.transform.SetParent(sliderGO.transform, false);

            var handleAreaRT = handleAreaGO.AddComponent<RectTransform>();
            handleAreaRT.anchorMin = Vector2.zero;
            handleAreaRT.anchorMax = Vector2.one;
            handleAreaRT.offsetMin = new Vector2(10f, 0f);
            handleAreaRT.offsetMax = new Vector2(-10f, 0f);

            var handleGO = new GameObject("Handle");
            handleGO.transform.SetParent(handleAreaGO.transform, false);

            var handleRT = handleGO.AddComponent<RectTransform>();
            handleRT.anchorMin = new Vector2(0f, 0f);
            handleRT.anchorMax = new Vector2(0f, 1f);
            handleRT.sizeDelta = new Vector2(20f, 0f);

            var handleImg = handleGO.AddComponent<Image>();
            handleImg.sprite = MakePixelSprite();
            handleImg.color = Color.white;

            var slider = sliderGO.AddComponent<Slider>();
            slider.fillRect = fillRT;
            slider.handleRect = handleRT;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = currentFloat;

            Type valueType = e.ValueType;
            slider.onValueChanged.AddListener((UnityEngine.Events.UnityAction<float>)((value) =>
            {
                e.ValueObject = (valueType == typeof(double)) ? (object)(double)value : (object)value;
                valueLabelTMP.text = value.ToString("F2");
                configFile.Save();
            }));
        }

        private void BuildSettingSliderInt(Transform rowTransform, IConfigEntryBase e, ConfigFile configFile)
        {
            // Value display label
            var valueLabelGO = new GameObject("ValueLabel");
            valueLabelGO.transform.SetParent(rowTransform, false);

            var valueLabelLE = valueLabelGO.AddComponent<LayoutElement>();
            valueLabelLE.preferredWidth = 40f;

            var valueLabelRT = valueLabelGO.AddComponent<RectTransform>();
            valueLabelRT.anchorMin = Vector2.zero;
            valueLabelRT.anchorMax = Vector2.one;
            valueLabelRT.offsetMin = Vector2.zero;
            valueLabelRT.offsetMax = Vector2.zero;

            var valueLabelTMP = valueLabelGO.AddComponent<TextMeshProUGUI>();
            float currentFloat = Convert.ToSingle(e.ValueObject);
            valueLabelTMP.text = ((int)currentFloat).ToString();
            valueLabelTMP.fontSize = 11;
            valueLabelTMP.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            valueLabelTMP.alignment = TextAlignmentOptions.MidlineRight;

            // Slider
            var sliderGO = new GameObject("Slider");
            sliderGO.transform.SetParent(rowTransform, false);

            var sliderLE = sliderGO.AddComponent<LayoutElement>();
            sliderLE.preferredWidth = 120f;
            sliderLE.preferredHeight = 20f;

            var sliderBg = sliderGO.AddComponent<Image>();
            sliderBg.sprite = MakePixelSprite();
            sliderBg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            // Fill area
            var fillAreaGO = new GameObject("Fill Area");
            fillAreaGO.transform.SetParent(sliderGO.transform, false);

            var fillAreaRT = fillAreaGO.AddComponent<RectTransform>();
            fillAreaRT.anchorMin = Vector2.zero;
            fillAreaRT.anchorMax = Vector2.one;
            fillAreaRT.offsetMin = new Vector2(5f, 0f);
            fillAreaRT.offsetMax = new Vector2(-15f, 0f);

            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(fillAreaGO.transform, false);

            var fillRT = fillGO.AddComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = Vector2.zero;
            fillRT.offsetMax = Vector2.zero;

            var fillImg = fillGO.AddComponent<Image>();
            fillImg.sprite = MakePixelSprite();
            fillImg.color = new Color(0.2f, 0.5f, 0.8f, 1f);

            // Handle area
            var handleAreaGO = new GameObject("Handle Slide Area");
            handleAreaGO.transform.SetParent(sliderGO.transform, false);

            var handleAreaRT = handleAreaGO.AddComponent<RectTransform>();
            handleAreaRT.anchorMin = Vector2.zero;
            handleAreaRT.anchorMax = Vector2.one;
            handleAreaRT.offsetMin = new Vector2(10f, 0f);
            handleAreaRT.offsetMax = new Vector2(-10f, 0f);

            var handleGO = new GameObject("Handle");
            handleGO.transform.SetParent(handleAreaGO.transform, false);

            var handleRT = handleGO.AddComponent<RectTransform>();
            handleRT.anchorMin = new Vector2(0f, 0f);
            handleRT.anchorMax = new Vector2(0f, 1f);
            handleRT.sizeDelta = new Vector2(20f, 0f);

            var handleImg = handleGO.AddComponent<Image>();
            handleImg.sprite = MakePixelSprite();
            handleImg.color = Color.white;

            var slider = sliderGO.AddComponent<Slider>();
            slider.fillRect = fillRT;
            slider.handleRect = handleRT;
            slider.direction = Slider.Direction.LeftToRight;
            slider.wholeNumbers = true;
            slider.minValue = 0;
            slider.maxValue = 100;
            slider.value = currentFloat;

            Type valueType = e.ValueType;
            slider.onValueChanged.AddListener((UnityEngine.Events.UnityAction<float>)((value) =>
            {
                e.ValueObject = (valueType == typeof(long)) ? (object)(long)(int)value : (object)(int)value;
                valueLabelTMP.text = ((int)value).ToString();
                configFile.Save();
            }));
        }

        private void BuildSettingInputField(Transform rowTransform, IConfigEntryBase e, ConfigFile configFile)
        {
            var inputGO = new GameObject("InputField");
            inputGO.transform.SetParent(rowTransform, false);

            var inputLE = inputGO.AddComponent<LayoutElement>();
            inputLE.preferredWidth = 160f;
            inputLE.preferredHeight = 28f;

            var inputBg = inputGO.AddComponent<Image>();
            inputBg.sprite = MakePixelSprite();
            inputBg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            // Text Area child
            var textAreaGO = new GameObject("Text Area");
            textAreaGO.transform.SetParent(inputGO.transform, false);

            var textAreaRT = textAreaGO.AddComponent<RectTransform>();
            textAreaRT.anchorMin = Vector2.zero;
            textAreaRT.anchorMax = Vector2.one;
            textAreaRT.offsetMin = new Vector2(4f, 2f);
            textAreaRT.offsetMax = new Vector2(-4f, -2f);

            textAreaGO.AddComponent<RectMask2D>();

            // Placeholder child
            var placeholderGO = new GameObject("Placeholder");
            placeholderGO.transform.SetParent(textAreaGO.transform, false);

            var placeholderRT = placeholderGO.AddComponent<RectTransform>();
            placeholderRT.anchorMin = Vector2.zero;
            placeholderRT.anchorMax = Vector2.one;
            placeholderRT.offsetMin = Vector2.zero;
            placeholderRT.offsetMax = Vector2.zero;

            var placeholderTMP = placeholderGO.AddComponent<TextMeshProUGUI>();
            placeholderTMP.text = "Enter value...";
            placeholderTMP.fontSize = 11;
            placeholderTMP.color = new Color(0.5f, 0.5f, 0.5f, 0.75f);
            placeholderTMP.alignment = TextAlignmentOptions.MidlineLeft;

            // Text child
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(textAreaGO.transform, false);

            var textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;

            var textTMP = textGO.AddComponent<TextMeshProUGUI>();
            textTMP.text = "";
            textTMP.fontSize = 11;
            textTMP.color = Color.white;
            textTMP.alignment = TextAlignmentOptions.MidlineLeft;

            // TMP_InputField component
            var inputField = inputGO.AddComponent<TMP_InputField>();
            inputField.textViewport = textAreaRT;
            inputField.textComponent = textTMP;
            inputField.placeholder = placeholderTMP;
            inputField.lineType = TMP_InputField.LineType.SingleLine;
            inputField.text = e.ValueObject?.ToString() ?? "";
            inputField.caretPosition = inputField.text.Length;

            inputField.onDeselect.AddListener((UnityEngine.Events.UnityAction<string>)((s) =>
            {
                e.ValueObject = s;
                configFile.Save();
            }));
        }

        private void BuildSettingDropdown(Transform rowTransform, IConfigEntryBase e, ConfigFile configFile)
        {
            var dropdownGO = new GameObject("Dropdown");
            dropdownGO.transform.SetParent(rowTransform, false);

            var dropdownLE = dropdownGO.AddComponent<LayoutElement>();
            dropdownLE.preferredWidth = 160f;
            dropdownLE.preferredHeight = 28f;

            var dropdownBg = dropdownGO.AddComponent<Image>();
            dropdownBg.sprite = MakePixelSprite();
            dropdownBg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            // Label child
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(dropdownGO.transform, false);

            var labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = new Vector2(10f, 2f);
            labelRT.offsetMax = new Vector2(-25f, -2f);

            var labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
            labelTMP.fontSize = 11;
            labelTMP.color = Color.white;
            labelTMP.alignment = TextAlignmentOptions.MidlineLeft;
            labelTMP.overflowMode = TextOverflowModes.Ellipsis;

            // Arrow child
            var arrowGO = new GameObject("Arrow");
            arrowGO.transform.SetParent(dropdownGO.transform, false);

            var arrowRT = arrowGO.AddComponent<RectTransform>();
            arrowRT.anchorMin = new Vector2(1f, 0.5f);
            arrowRT.anchorMax = new Vector2(1f, 0.5f);
            arrowRT.pivot = new Vector2(1f, 0.5f);
            arrowRT.sizeDelta = new Vector2(20f, 20f);
            arrowRT.anchoredPosition = new Vector2(-2f, 0f);

            var arrowTMP = arrowGO.AddComponent<TextMeshProUGUI>();
            arrowTMP.text = "v";
            arrowTMP.fontSize = 10;
            arrowTMP.color = Color.white;
            arrowTMP.alignment = TextAlignmentOptions.Center;

            // Template (required by TMP_Dropdown but not shown — hidden)
            var templateGO = new GameObject("Template");
            templateGO.transform.SetParent(dropdownGO.transform, false);
            templateGO.SetActive(false);

            var templateRT = templateGO.AddComponent<RectTransform>();
            templateRT.anchorMin = new Vector2(0f, 0f);
            templateRT.anchorMax = new Vector2(1f, 0f);
            templateRT.pivot = new Vector2(0.5f, 1f);
            templateRT.sizeDelta = new Vector2(0f, 150f);

            var templateBg = templateGO.AddComponent<Image>();
            templateBg.sprite = MakePixelSprite();
            templateBg.color = new Color(0.1f, 0.1f, 0.1f, 1f);

            var scrollRectGO = new GameObject("Scroll View");
            scrollRectGO.transform.SetParent(templateGO.transform, false);

            var scrollRT = scrollRectGO.AddComponent<RectTransform>();
            scrollRT.anchorMin = Vector2.zero;
            scrollRT.anchorMax = Vector2.one;
            scrollRT.offsetMin = new Vector2(0f, 0f);
            scrollRT.offsetMax = new Vector2(0f, 0f);

            var scrollRect = scrollRectGO.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;

            var viewportGO2 = new GameObject("Viewport");
            viewportGO2.transform.SetParent(scrollRectGO.transform, false);

            var viewportRT2 = viewportGO2.AddComponent<RectTransform>();
            viewportRT2.anchorMin = Vector2.zero;
            viewportRT2.anchorMax = Vector2.one;
            viewportRT2.offsetMin = Vector2.zero;
            viewportRT2.offsetMax = Vector2.zero;

            var viewportImg2 = viewportGO2.AddComponent<Image>();
            viewportImg2.sprite = MakePixelSprite();
            viewportImg2.color = Color.white;
            viewportGO2.AddComponent<Mask>().showMaskGraphic = false;

            var contentGO2 = new GameObject("Content");
            contentGO2.transform.SetParent(viewportGO2.transform, false);

            var contentRT2 = contentGO2.AddComponent<RectTransform>();
            contentRT2.anchorMin = new Vector2(0f, 1f);
            contentRT2.anchorMax = new Vector2(1f, 1f);
            contentRT2.pivot = new Vector2(0.5f, 1f);
            contentRT2.sizeDelta = new Vector2(0f, 28f);

            scrollRect.viewport = viewportRT2;
            scrollRect.content = contentRT2;

            // Item template
            var itemGO = new GameObject("Item");
            itemGO.transform.SetParent(contentGO2.transform, false);

            var itemToggle = itemGO.AddComponent<Toggle>();
            var itemRT = itemGO.AddComponent<RectTransform>();
            itemRT.anchorMin = Vector2.zero;
            itemRT.anchorMax = new Vector2(1f, 0f);
            itemRT.sizeDelta = new Vector2(0f, 28f);

            var itemBg = itemGO.AddComponent<Image>();
            itemBg.sprite = MakePixelSprite();
            itemBg.color = new Color(0.15f, 0.15f, 0.15f, 1f);
            itemToggle.targetGraphic = itemBg;

            var itemLabelGO = new GameObject("Item Label");
            itemLabelGO.transform.SetParent(itemGO.transform, false);

            var itemLabelRT = itemLabelGO.AddComponent<RectTransform>();
            itemLabelRT.anchorMin = Vector2.zero;
            itemLabelRT.anchorMax = Vector2.one;
            itemLabelRT.offsetMin = new Vector2(10f, 2f);
            itemLabelRT.offsetMax = new Vector2(-2f, -2f);

            var itemLabelTMP = itemLabelGO.AddComponent<TextMeshProUGUI>();
            itemLabelTMP.fontSize = 11;
            itemLabelTMP.color = Color.white;
            itemLabelTMP.alignment = TextAlignmentOptions.MidlineLeft;

            // TMP_Dropdown component
            var dropdown = dropdownGO.AddComponent<TMP_Dropdown>();
            dropdown.template = templateRT;
            dropdown.captionText = labelTMP;
            dropdown.itemText = itemLabelTMP;

            // Populate options from EnumValues
            dropdown.ClearOptions();
            var options = new Il2CppSystem.Collections.Generic.List<TMP_Dropdown.OptionData>();
            string[] enumValues = e.EnumValues;
            foreach (string enumValue in enumValues)
                options.Add(new TMP_Dropdown.OptionData(enumValue));
            dropdown.AddOptions(options);

            // Set current selection
            string currentStr = e.ValueObject?.ToString() ?? "";
            int selectedIdx = Array.IndexOf(enumValues, currentStr);
            dropdown.value = selectedIdx >= 0 ? selectedIdx : 0;

            dropdown.onValueChanged.AddListener((UnityEngine.Events.UnityAction<int>)((i) =>
            {
                e.ValueObject = enumValues[i];
                configFile.Save();
            }));
        }

        // ─── Helpers ────────────────────────────────────────────────────────────

        private TextMeshProUGUI CreateTMPLabel(Transform parent, string text, int fontSize,
            Color color, float preferredWidth)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);

            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = preferredWidth;

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.overflowMode = TextOverflowModes.Ellipsis;

            return tmp;
        }
    }
}
