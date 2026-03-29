using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using VNWinter.DialogueGraph;

namespace VNWinter.DialogueGraph.Editor
{
    /// <summary>
    /// A resizable sticky note element for the dialogue graph editor.
    /// Used for annotations, reminders, and documentation within the graph.
    /// Similar to Shader Graph's sticky note feature.
    /// </summary>
    public class StickyNote : GraphElement
    {
        private const float MIN_WIDTH = 150f;
        private const float MIN_HEIGHT = 100f;
        private const float DEFAULT_WIDTH = 200f;
        private const float DEFAULT_HEIGHT = 160f;

        private static readonly Color DEFAULT_COLOR = new Color(1f, 0.95f, 0.7f, 1f);

        private string guid;
        private TextField titleField;
        private TextField contentField;
        private VisualElement resizeHandle;
        private EnumField fontSizeField;
        private ColorField colorField;
        private VisualElement header;
        private Color currentColor;

        public string Guid => guid;
        public string Title => titleField.value;
        public string Content => contentField.value;
        public StickyNoteFontSize CurrentFontSize => (StickyNoteFontSize)fontSizeField.value;
        public Color CurrentColor => currentColor;

        public StickyNote()
        {
            guid = System.Guid.NewGuid().ToString();
            currentColor = DEFAULT_COLOR;
            BuildUI();
            RegisterCallbacks();
        }

        private void BuildUI()
        {
            // Enable selection and movement
            capabilities = Capabilities.Movable | Capabilities.Selectable | Capabilities.Deletable | Capabilities.Resizable;

            // Main container styling
            style.position = Position.Absolute;
            style.width = DEFAULT_WIDTH;
            style.height = DEFAULT_HEIGHT;
            style.minWidth = MIN_WIDTH;
            style.minHeight = MIN_HEIGHT;
            style.backgroundColor = new Color(1f, 0.95f, 0.7f, 1f); // Light yellow
            style.borderTopWidth = 1;
            style.borderBottomWidth = 1;
            style.borderLeftWidth = 1;
            style.borderRightWidth = 1;
            style.borderTopColor = new Color(0.8f, 0.7f, 0.4f, 1f);
            style.borderBottomColor = new Color(0.8f, 0.7f, 0.4f, 1f);
            style.borderLeftColor = new Color(0.8f, 0.7f, 0.4f, 1f);
            style.borderRightColor = new Color(0.8f, 0.7f, 0.4f, 1f);
            style.borderTopLeftRadius = 4;
            style.borderTopRightRadius = 4;
            style.borderBottomLeftRadius = 4;
            style.borderBottomRightRadius = 4;
            style.flexDirection = FlexDirection.Column;

            // Header bar with title and font size selector
            header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.backgroundColor = new Color(0.9f, 0.8f, 0.5f, 1f);
            header.style.paddingLeft = 4;
            header.style.paddingRight = 4;
            header.style.paddingTop = 4;
            header.style.paddingBottom = 4;
            header.style.borderBottomWidth = 1;
            header.style.borderBottomColor = new Color(0.8f, 0.7f, 0.4f, 1f);

            titleField = new TextField();
            titleField.value = "Note";
            titleField.style.flexGrow = 1;
            titleField.style.unityFontStyleAndWeight = FontStyle.Bold;
            // Remove the label
            titleField.labelElement.style.display = DisplayStyle.None;
            // Style the input
            var titleInput = titleField.Q<VisualElement>("unity-text-input");
            if (titleInput != null)
            {
                titleInput.style.backgroundColor = Color.clear;
                titleInput.style.borderTopWidth = 0;
                titleInput.style.borderBottomWidth = 0;
                titleInput.style.borderLeftWidth = 0;
                titleInput.style.borderRightWidth = 0;
                titleInput.style.color = Color.black;
            }
            // Update header visibility when title changes
            titleField.RegisterValueChangedCallback(evt => UpdateHeaderVisibility());
            header.Add(titleField);

            // Font size dropdown
            fontSizeField = new EnumField(StickyNoteFontSize.Normal);
            fontSizeField.style.width = 70;
            fontSizeField.style.marginLeft = 4;
            fontSizeField.RegisterValueChangedCallback(evt => UpdateFontSize());
            // Hide label
            fontSizeField.labelElement.style.display = DisplayStyle.None;
            header.Add(fontSizeField);

            // Color picker
            colorField = new ColorField();
            colorField.value = currentColor;
            colorField.style.width = 50;
            colorField.style.marginLeft = 4;
            colorField.showAlpha = false;
            colorField.RegisterValueChangedCallback(evt => ApplyColor(evt.newValue));
            // Hide label
            colorField.labelElement.style.display = DisplayStyle.None;
            header.Add(colorField);

            Add(header);

            // Content area
            var contentContainer = new VisualElement();
            contentContainer.style.flexGrow = 1;
            contentContainer.style.paddingLeft = 8;
            contentContainer.style.paddingRight = 8;
            contentContainer.style.paddingTop = 8;
            contentContainer.style.paddingBottom = 8;

            contentField = new TextField();
            contentField.multiline = true;
            contentField.value = "Enter notes here...";
            contentField.style.flexGrow = 1;
            contentField.style.whiteSpace = WhiteSpace.Normal;
            // Remove label
            contentField.labelElement.style.display = DisplayStyle.None;
            // Style the input to fill the space
            var contentInput = contentField.Q<VisualElement>("unity-text-input");
            if (contentInput != null)
            {
                contentInput.style.backgroundColor = Color.clear;
                contentInput.style.borderTopWidth = 0;
                contentInput.style.borderBottomWidth = 0;
                contentInput.style.borderLeftWidth = 0;
                contentInput.style.borderRightWidth = 0;
                contentInput.style.flexGrow = 1;
                contentInput.style.unityTextAlign = TextAnchor.UpperLeft;
                contentInput.style.color = Color.black;
            }
            contentField.style.height = new StyleLength(StyleKeyword.Auto);
            contentField.style.flexGrow = 1;

            contentContainer.Add(contentField);
            Add(contentContainer);

            // Resize handle visual indicator (bottom-right corner)
            resizeHandle = new VisualElement();
            resizeHandle.style.position = Position.Absolute;
            resizeHandle.style.right = 0;
            resizeHandle.style.bottom = 0;
            resizeHandle.style.width = 16;
            resizeHandle.style.height = 16;
            resizeHandle.style.backgroundColor = new Color(0.8f, 0.7f, 0.4f, 0.5f);
            resizeHandle.pickingMode = PickingMode.Ignore; // Let the built-in resizer handle it

            // Draw resize grip lines
            for (int i = 0; i < 3; i++)
            {
                var gripLine = new VisualElement();
                gripLine.style.position = Position.Absolute;
                gripLine.style.right = 3 + i * 4;
                gripLine.style.bottom = 3;
                gripLine.style.width = 1;
                gripLine.style.height = 10 - i * 3;
                gripLine.style.backgroundColor = new Color(0.6f, 0.5f, 0.3f, 1f);
                gripLine.pickingMode = PickingMode.Ignore;
                resizeHandle.Add(gripLine);
            }

            Add(resizeHandle);

            // Add the shared graph element resizer
            Add(new GraphElementResizer(MIN_WIDTH, MIN_HEIGHT));

            UpdateFontSize();
        }

        private void RegisterCallbacks()
        {
            // Prevent drag when clicking inside text fields and controls
            titleField.RegisterCallback<MouseDownEvent>(evt => evt.StopPropagation());
            contentField.RegisterCallback<MouseDownEvent>(evt => evt.StopPropagation());
            colorField.RegisterCallback<MouseDownEvent>(evt => evt.StopPropagation());

            // Update style dimensions when geometry changes (after resize)
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            // Keep style in sync with layout for saving
            if (evt.newRect.width > 0 && evt.newRect.height > 0)
            {
                style.width = evt.newRect.width;
                style.height = evt.newRect.height;
            }
        }

        private void UpdateFontSize()
        {
            var size = (StickyNoteFontSize)fontSizeField.value;
            int fontSize = size switch
            {
                StickyNoteFontSize.Small => 12,
                StickyNoteFontSize.Normal => 16,
                StickyNoteFontSize.Large => 22,
                StickyNoteFontSize.ExtraLarge => 32,
                _ => 16
            };

            // Apply font size to the TextField and its inner text input element
            contentField.style.fontSize = fontSize;
            var textInput = contentField.Q<VisualElement>("unity-text-input");
            if (textInput != null)
            {
                textInput.style.fontSize = fontSize;
            }
        }

        private void UpdateHeaderVisibility()
        {
            bool hasTitle = !string.IsNullOrEmpty(titleField.value);

            if (hasTitle)
            {
                // Show full header with title field
                titleField.style.display = DisplayStyle.Flex;
                header.style.backgroundColor = GetHeaderColor();
                header.style.borderBottomWidth = 1;
                fontSizeField.style.marginLeft = 4;
            }
            else
            {
                // Hide title field, show only font size dropdown in minimal header
                titleField.style.display = DisplayStyle.None;
                header.style.backgroundColor = Color.clear;
                header.style.borderBottomWidth = 0;
                fontSizeField.style.marginLeft = 0;
            }
        }

        private void ApplyColor(Color color)
        {
            currentColor = color;

            // Apply main background color
            style.backgroundColor = color;

            // Compute a slightly darker shade for borders and header
            var borderColor = new Color(color.r * 0.8f, color.g * 0.8f, color.b * 0.8f, 1f);
            style.borderTopColor = borderColor;
            style.borderBottomColor = borderColor;
            style.borderLeftColor = borderColor;
            style.borderRightColor = borderColor;

            // Update header color if visible
            if (!string.IsNullOrEmpty(titleField.value))
            {
                header.style.backgroundColor = GetHeaderColor();
                header.style.borderBottomColor = borderColor;
            }

            // Update resize handle color
            resizeHandle.style.backgroundColor = new Color(borderColor.r, borderColor.g, borderColor.b, 0.5f);

            // Update grip lines
            var gripColor = new Color(color.r * 0.6f, color.g * 0.6f, color.b * 0.6f, 1f);
            foreach (var child in resizeHandle.Children())
            {
                child.style.backgroundColor = gripColor;
            }
        }

        private Color GetHeaderColor()
        {
            // Return a slightly darker shade for the header
            return new Color(currentColor.r * 0.9f, currentColor.g * 0.9f, currentColor.b * 0.9f, 1f);
        }

        /// <summary>
        /// Initialize the sticky note at a specific position.
        /// </summary>
        public void Initialize(Vector2 position)
        {
            SetPosition(new Rect(position.x, position.y, DEFAULT_WIDTH, DEFAULT_HEIGHT));
        }

        /// <summary>
        /// Load data from a serialized StickyNoteData object.
        /// </summary>
        public void LoadFromData(StickyNoteData data)
        {
            guid = data.guid;
            titleField.value = data.title;
            contentField.value = data.content;
            fontSizeField.value = data.fontSize;

            // Load color (handle legacy notes without color)
            var color = data.backgroundColor.a > 0 ? data.backgroundColor : DEFAULT_COLOR;
            colorField.value = color;
            ApplyColor(color);

            UpdateFontSize();
            UpdateHeaderVisibility();

            style.width = data.size.x;
            style.height = data.size.y;
            SetPosition(new Rect(data.position.x, data.position.y, data.size.x, data.size.y));
        }

        /// <summary>
        /// Save the current state to a StickyNoteData object for serialization.
        /// </summary>
        public StickyNoteData SaveToData()
        {
            var rect = GetPosition();
            return new StickyNoteData
            {
                guid = guid,
                title = titleField.value,
                content = contentField.value,
                fontSize = (StickyNoteFontSize)fontSizeField.value,
                backgroundColor = currentColor,
                position = new Vector2(rect.x, rect.y),
                size = new Vector2(resolvedStyle.width > 0 ? resolvedStyle.width : rect.width,
                                   resolvedStyle.height > 0 ? resolvedStyle.height : rect.height)
            };
        }
    }

}
