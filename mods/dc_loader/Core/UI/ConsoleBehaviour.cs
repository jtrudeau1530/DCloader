using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using DCLoader.Core;

namespace DCLoader.Core.UI
{
    // Dev console — backquote (~) to toggle. Bottom-anchored scrollable output + input field.
    public class ConsoleBehaviour : MonoBehaviour
    {
        // IL2CPP needs this or it throws MissingMethodException
        public ConsoleBehaviour(IntPtr ptr) : base(ptr) { }

        private Canvas _canvas;
        private ScrollRect _scrollRect;
        private TextMeshProUGUI _outputText;
        private TMP_InputField _inputField;
        private bool _isOpen = false;
        private bool _pendingFocus = false;
        private bool _scrollToBottom = false;

        // TMP font — gotta cache this because dynamically-created TMP components start with
        // font==null and silently break (no placeholder, no focus) until we assign one
        private TMP_FontAsset _tmpFont;

        private readonly List<(string text, bool isError)> _lines = new();
        private readonly List<string> _history = new();
        private int _historyIndex = -1;
        private string _inputBeforeHistory = "";

        private const int MaxLines = 200;
        private const int MaxHistory = 20;

        private void Start()
        {
            ConsoleManager.UI = this;
            ConsoleManager.RegisterBuiltins();
            LoadTMPFont();
            BuildCanvas();
        }

        private void LoadTMPFont()
        {
            _tmpFont = Resources.Load<TMP_FontAsset>("fonts & materials/LiberationSans SDF");

            if (_tmpFont == null)
            {
                // fallback if the resource path doesn't work
                _tmpFont = TMP_Settings.defaultFontAsset;
            }

            if (_tmpFont == null)
            {
                DcLogger.Warn("Console",
                    "[Console] Could not load TMP font asset. Input field may not render correctly.");
            }
            else
            {
                DcLogger.Info("Console", $"[Console] TMP font loaded: {_tmpFont.name}");
            }
        }

        private void Update()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current.backquoteKey.wasPressedThisFrame)
            {
                ToggleConsole();
                return;
            }

            // delayed focus — clears the ` char that leaks into the input field
            if (_pendingFocus)
            {
                _pendingFocus = false;
                _inputField.text = "";
                Canvas.ForceUpdateCanvases();
                _inputField.ActivateInputField();
                return;
            }

            if (_isOpen && _inputField != null && _inputField.isFocused)
            {
                // using Keyboard directly instead of onSubmit — onSubmit is flaky under IL2CPP
                if (Keyboard.current.enterKey.wasPressedThisFrame ||
                    Keyboard.current.numpadEnterKey.wasPressedThisFrame)
                {
                    SubmitInput();
                }
                else if (Keyboard.current.upArrowKey.wasPressedThisFrame)
                    NavigateHistory(-1);
                else if (Keyboard.current.downArrowKey.wasPressedThisFrame)
                    NavigateHistory(+1);
            }

            // Deferred scroll-to-bottom after layout pass
            if (_scrollToBottom && _scrollRect != null)
            {
                _scrollToBottom = false;
                Canvas.ForceUpdateCanvases();
                _scrollRect.verticalNormalizedPosition = 0f;
            }
        }

        // ─── Toggle ─────────────────────────────────────────────────────────────

        private void ToggleConsole()
        {
            _isOpen = !_isOpen;
            _canvas.gameObject.SetActive(_isOpen);

            if (_isOpen)
            {
                // disable both Player AND UI action maps — the pause key lives in UI, not Player
                try
                {
                    InputManager.inputActions.Player.Disable();
                }
                catch (Exception ex)
                {
                    DcLogger.Warn("Console",
                        $"[Console] Could not disable Player action map: {ex.Message}");
                }
                try
                {
                    InputManager.inputActions.UI.Disable();
                }
                catch (Exception ex)
                {
                    DcLogger.Warn("Console",
                        $"[Console] Could not disable UI action map: {ex.Message}");
                }

                _pendingFocus = true;
                _historyIndex = -1;
            }
            else
            {
                _inputField?.DeactivateInputField();

                try
                {
                    InputManager.inputActions.Player.Enable();
                }
                catch (Exception ex)
                {
                    DcLogger.Warn("Console",
                        $"[Console] Could not re-enable Player action map: {ex.Message}");
                }
                try
                {
                    InputManager.inputActions.UI.Enable();
                }
                catch (Exception ex)
                {
                    DcLogger.Warn("Console",
                        $"[Console] Could not re-enable UI action map: {ex.Message}");
                }
            }
        }

        // ─── Submit ─────────────────────────────────────────────────────────────

        private void SubmitInput()
        {
            if (_inputField == null) return;
            var text = _inputField.text;
            if (!string.IsNullOrWhiteSpace(text))
            {
                ConsoleManager.Execute(text.Trim());
                AddToHistory(text.Trim());
                _inputField.text = "";
                _inputField.ActivateInputField(); // keep focus for next command
            }
        }

        // ─── Output API ─────────────────────────────────────────────────────────

        internal void AppendLine(string text, bool isError)
        {
            _lines.Add((text, isError));
            if (_lines.Count > MaxLines)
                _lines.RemoveAt(0);
            RebuildOutput();
            // force layout update or the new text gets clipped on the same frame
            Canvas.ForceUpdateCanvases();
            _scrollToBottom = true;
        }

        internal void ClearOutput()
        {
            _lines.Clear();
            if (_outputText != null)
                _outputText.text = "";
        }

        // ─── Output Rendering ───────────────────────────────────────────────────

        private void RebuildOutput()
        {
            if (_outputText == null) return;
            var sb = new StringBuilder();
            foreach (var (text, isError) in _lines)
            {
                string color = isError ? "#FF4444" : "#CCCCCC";
                sb.AppendLine($"<color={color}>{text}</color>");
            }
            _outputText.text = sb.ToString();
        }

        // ─── History ────────────────────────────────────────────────────────────

        private void AddToHistory(string command)
        {
            if (_history.Count == 0 || _history[^1] != command)
            {
                _history.Add(command);
                if (_history.Count > MaxHistory)
                    _history.RemoveAt(0);
            }
            _historyIndex = -1;
        }

        private void NavigateHistory(int direction)
        {
            if (_history.Count == 0) return;

            if (_historyIndex == -1 && direction < 0)
            {
                _inputBeforeHistory = _inputField.text;
                _historyIndex = _history.Count - 1;
                _inputField.text = _history[_historyIndex];
            }
            else if (_historyIndex >= 0)
            {
                int newIndex = _historyIndex + direction;
                if (direction > 0 && newIndex >= _history.Count)
                {
                    _historyIndex = -1;
                    _inputField.text = _inputBeforeHistory;
                }
                else
                {
                    _historyIndex = Mathf.Clamp(newIndex, 0, _history.Count - 1);
                    _inputField.text = _history[_historyIndex];
                }
            }

            // MoveToEndOfLine() is sketchy under IL2CPP, just set caret directly
            if (_inputField != null)
                _inputField.caretPosition = _inputField.text.Length;
        }

        // ─── Helpers ────────────────────────────────────────────────────────────

        private void ApplyFont(TextMeshProUGUI tmp)
        {
            if (_tmpFont != null)
                tmp.font = _tmpFont;
        }

        // IL2CPP can't access the built-in UISprite so we make our own 1x1 white pixel
        private static Sprite MakePixelSprite()
        {
            return Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
        }

        // ─── Canvas Construction ────────────────────────────────────────────────

        private void BuildCanvas()
        {
            var canvasGO = new GameObject("DCLoader_Console");
            canvasGO.transform.SetParent(transform, false);

            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 9998; // below mod menu

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            canvasGO.AddComponent<GraphicRaycaster>(); // uses game's existing EventSystem
            canvasGO.SetActive(false);
            BuildPanel(canvasGO.transform);
        }

        private void BuildPanel(Transform canvasTransform)
        {
            var panelGO = new GameObject("Panel");
            panelGO.transform.SetParent(canvasTransform, false);

            var panelRT = panelGO.AddComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0f, 0f);
            panelRT.anchorMax = new Vector2(1f, 0.40f);
            panelRT.offsetMin = Vector2.zero;
            panelRT.offsetMax = Vector2.zero;

            var bg = panelGO.AddComponent<Image>();
            bg.sprite = MakePixelSprite();
            bg.color = new Color(0.05f, 0.05f, 0.05f, 0.88f);

            BuildOutputArea(panelGO.transform);
            BuildInputRow(panelGO.transform);
        }

        private void BuildOutputArea(Transform panelTransform)
        {
            var scrollGO = new GameObject("OutputScroll");
            scrollGO.transform.SetParent(panelTransform, false);

            var scrollRT = scrollGO.AddComponent<RectTransform>();
            scrollRT.anchorMin = Vector2.zero;
            scrollRT.anchorMax = Vector2.one;
            scrollRT.offsetMin = new Vector2(0f, 40f);
            scrollRT.offsetMax = Vector2.zero;

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

            viewportGO.AddComponent<RectMask2D>();

            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(viewportGO.transform, false);

            var contentRT = contentGO.AddComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0f, 1f);
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.pivot = new Vector2(0.5f, 1f);
            contentRT.offsetMin = Vector2.zero;
            contentRT.offsetMax = Vector2.zero;

            var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 0f;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.padding.left = 4;
            vlg.padding.right = 4;
            vlg.padding.top = 4;
            vlg.padding.bottom = 4;

            var csf = contentGO.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var outputGO = new GameObject("OutputText");
            outputGO.transform.SetParent(contentGO.transform, false);

            var outputRT = outputGO.AddComponent<RectTransform>();
            outputRT.anchorMin = Vector2.zero;
            outputRT.anchorMax = Vector2.one;
            outputRT.offsetMin = Vector2.zero;
            outputRT.offsetMax = Vector2.zero;

            var outputCSF = outputGO.AddComponent<ContentSizeFitter>();
            outputCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _outputText = outputGO.AddComponent<TextMeshProUGUI>();
            _outputText.fontSize = 14;
            _outputText.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            _outputText.alignment = TextAlignmentOptions.TopLeft;
            _outputText.enableWordWrapping = true;
            _outputText.richText = true;
            ApplyFont(_outputText);

            scrollRect.content = contentRT;
            scrollRect.viewport = viewportRT;
            _scrollRect = scrollRect;
        }

        private void BuildInputRow(Transform panelTransform)
        {
            var inputRowGO = new GameObject("InputRow");
            inputRowGO.transform.SetParent(panelTransform, false);

            var inputRowRT = inputRowGO.AddComponent<RectTransform>();
            inputRowRT.anchorMin = new Vector2(0f, 0f);
            inputRowRT.anchorMax = new Vector2(1f, 0f);
            inputRowRT.pivot = new Vector2(0.5f, 0f);
            inputRowRT.offsetMin = Vector2.zero;
            inputRowRT.offsetMax = new Vector2(0f, 40f);

            var rowBg = inputRowGO.AddComponent<Image>();
            rowBg.sprite = MakePixelSprite();
            rowBg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

            var hlg = inputRowGO.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 4f;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.padding.left = 8;
            hlg.padding.right = 8;
            hlg.padding.top = 4;
            hlg.padding.bottom = 4;

            var promptGO = new GameObject("Prompt");
            promptGO.transform.SetParent(inputRowGO.transform, false);

            var promptLE = promptGO.AddComponent<LayoutElement>();
            promptLE.preferredWidth = 25f;

            var promptRT = promptGO.AddComponent<RectTransform>();
            promptRT.anchorMin = Vector2.zero;
            promptRT.anchorMax = Vector2.one;
            promptRT.offsetMin = Vector2.zero;
            promptRT.offsetMax = Vector2.zero;

            var promptText = promptGO.AddComponent<TextMeshProUGUI>();
            promptText.text = "> ";
            promptText.fontSize = 14;
            promptText.color = new Color(0.3f, 1f, 0.3f, 1f);
            promptText.alignment = TextAlignmentOptions.MidlineLeft;
            ApplyFont(promptText);

            // Input field container
            var inputGO = new GameObject("InputField");
            inputGO.transform.SetParent(inputRowGO.transform, false);

            var inputLE = inputGO.AddComponent<LayoutElement>();
            inputLE.flexibleWidth = 1f;

            // TMP_InputField needs a Graphic on its GO to work as targetGraphic
            var inputBg = inputGO.AddComponent<Image>();
            inputBg.sprite = MakePixelSprite();
            inputBg.color = new Color(0.08f, 0.08f, 0.08f, 0f);

            var inputField = inputGO.AddComponent<TMP_InputField>();

            // TMP_InputField doesn't auto-create its child hierarchy under IL2CPP — gotta build it manually
            var textAreaGO = new GameObject("Text Area");
            textAreaGO.transform.SetParent(inputGO.transform, false);

            var textAreaRT = textAreaGO.AddComponent<RectTransform>();
            textAreaRT.anchorMin = Vector2.zero;
            textAreaRT.anchorMax = Vector2.one;
            textAreaRT.offsetMin = new Vector2(10f, 6f);
            textAreaRT.offsetMax = new Vector2(-10f, -6f);
            textAreaGO.AddComponent<RectMask2D>();

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(textAreaGO.transform, false);

            var textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;

            var textComp = textGO.AddComponent<TextMeshProUGUI>();
            textComp.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            textComp.fontSize = 14;
            ApplyFont(textComp);

            var placeholderGO = new GameObject("Placeholder");
            placeholderGO.transform.SetParent(textAreaGO.transform, false);

            var phRT = placeholderGO.AddComponent<RectTransform>();
            phRT.anchorMin = Vector2.zero;
            phRT.anchorMax = Vector2.one;
            phRT.offsetMin = Vector2.zero;
            phRT.offsetMax = Vector2.zero;

            var phComp = placeholderGO.AddComponent<TextMeshProUGUI>();
            phComp.text = "type command...";
            phComp.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
            phComp.fontSize = 14;
            ApplyFont(phComp);

            // textViewport must be set or caret won't render (early-returns in GenerateCaret)
            inputField.textViewport = textAreaRT;
            inputField.textComponent = textComp;
            inputField.placeholder = phComp;
            inputField.lineType = TMP_InputField.LineType.SingleLine;

            _inputField = inputField;
        }
    }
}
