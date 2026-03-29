using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using VNWinter.DialogueGraph;

namespace VNWinter.DialogueGraph.Editor
{
    public class FadeNode : BaseDialogueNode
    {
        private EnumField fadeTypeField;
        private ColorField fadeColorField;
        private FloatField fadeDurationField;
        private Toggle waitForCompletionField;

        public FadeNodeData.FadeType FadeType => (FadeNodeData.FadeType)(fadeTypeField?.value ?? FadeNodeData.FadeType.FadeOut);
        public Color FadeColor => fadeColorField?.value ?? Color.black;
        public float FadeDuration => fadeDurationField?.value ?? 1f;
        public bool WaitForCompletion => waitForCompletionField?.value ?? true;

        public FadeNode()
        {
            title = "Fade";
            AddToClassList("fade-node");
        }

        public override void Initialize(Vector2 position)
        {
            base.Initialize(position);

            inputPort = CreateInputPort("Input");
            outputPort = CreateOutputPort("Output");

            CreateFields();
            UpdateFieldVisibility();

            RefreshExpandedState();
            RefreshPorts();
        }

        private void CreateFields()
        {
            var container = new VisualElement();
            container.style.paddingLeft = 4;
            container.style.paddingRight = 4;
            container.style.paddingTop = 4;
            container.style.paddingBottom = 4;
            container.style.minWidth = 200;

            // Fade type dropdown
            fadeTypeField = new EnumField("Fade Type", FadeNodeData.FadeType.FadeOut);
            fadeTypeField.RegisterValueChangedCallback(evt => UpdateFieldVisibility());
            container.Add(fadeTypeField);

            // Color field (conditional visibility)
            fadeColorField = new ColorField("Fade Color");
            fadeColorField.value = Color.black;
            fadeColorField.style.marginTop = 4;
            container.Add(fadeColorField);

            // Duration field
            fadeDurationField = new FloatField("Duration (seconds)");
            fadeDurationField.value = 1f;
            fadeDurationField.style.marginTop = 4;
            container.Add(fadeDurationField);

            // Wait for completion toggle
            waitForCompletionField = new Toggle("Wait For Completion");
            waitForCompletionField.value = true;
            waitForCompletionField.style.marginTop = 8;
            waitForCompletionField.tooltip = "If true, dialogue waits until fade completes before continuing. If false, dialogue advances immediately while fade continues in background.";
            container.Add(waitForCompletionField);

            extensionContainer.Add(container);

            CreateNotesSection();
        }

        private void UpdateFieldVisibility()
        {
            var fadeType = FadeType;

            // Show color field for FadeOut, FadeToColor, and SolidColor
            // Hide for FadeIn (since it fades from whatever color is already shown to transparent)
            bool showColorField = fadeType == FadeNodeData.FadeType.FadeOut ||
                                  fadeType == FadeNodeData.FadeType.FadeToColor ||
                                  fadeType == FadeNodeData.FadeType.SolidColor;
            fadeColorField.style.display = showColorField ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public override void LoadFromData(BaseNodeData data)
        {
            var fadeData = data as FadeNodeData;
            if (fadeData == null) return;

            Guid = data.guid;
            SetPosition(new Rect(data.position, Vector2.zero));

            inputPort = CreateInputPort("Input");
            outputPort = CreateOutputPort("Output");

            CreateFields();

            fadeTypeField.value = fadeData.fadeType;
            fadeColorField.value = fadeData.fadeColor;
            fadeDurationField.value = fadeData.fadeDuration;
            waitForCompletionField.value = fadeData.waitForCompletion;

            UpdateFieldVisibility();

            LoadNotesFromData(data);
            LoadExpandedStateFromData(data);

            RefreshExpandedState();
            RefreshPorts();
        }

        public override BaseNodeData SaveToData()
        {
            var data = new FadeNodeData
            {
                guid = Guid,
                nodeType = "Fade",
                position = GetPosition().position,
                fadeType = FadeType,
                fadeColor = FadeColor,
                fadeDuration = FadeDuration,
                waitForCompletion = WaitForCompletion
            };
            SaveNotesToData(data);
            SaveExpandedStateToData(data);
            return data;
        }
    }
}
