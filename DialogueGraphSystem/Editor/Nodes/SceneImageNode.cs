using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using VNWinter.DialogueGraph;

namespace VNWinter.DialogueGraph.Editor
{
    public class SceneImageNode : BaseDialogueNode
    {
        private TextField targetObjectNameField;
        private ObjectField imageSpriteField;
        private Slider opacityField;

        public string TargetObjectName => targetObjectNameField?.value ?? "";
        public Sprite ImageSprite => imageSpriteField?.value as Sprite;
        public float Opacity => opacityField?.value ?? 1f;

        public SceneImageNode()
        {
            title = "Scene Image";
            AddToClassList("scene-image-node");
        }

        public override void Initialize(Vector2 position)
        {
            base.Initialize(position);

            inputPort = CreateInputPort("Input");
            outputPort = CreateOutputPort("Output");

            CreateFields();

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

            // Target object name field
            targetObjectNameField = new TextField("Target Object Name")
            {
                tooltip = "Name of the GameObject in the scene to find and replace its image"
            };
            container.Add(targetObjectNameField);

            // Image sprite field
            imageSpriteField = new ObjectField("Image")
            {
                objectType = typeof(Sprite),
                allowSceneObjects = false,
                tooltip = "Sprite to set on the target object's Image component"
            };
            imageSpriteField.style.marginTop = 4;
            container.Add(imageSpriteField);

            // Opacity slider
            opacityField = new Slider("Opacity", 0f, 1f)
            {
                value = 1f,
                tooltip = "Opacity/alpha value for the image (0 = transparent, 1 = fully opaque)"
            };
            opacityField.showInputField = true;
            opacityField.style.marginTop = 4;
            container.Add(opacityField);

            extensionContainer.Add(container);

            CreateNotesSection();
        }

        public override void LoadFromData(BaseNodeData data)
        {
            var sceneImageData = data as SceneImageNodeData;
            if (sceneImageData == null) return;

            Guid = data.guid;
            SetPosition(new Rect(data.position, Vector2.zero));

            inputPort = CreateInputPort("Input");
            outputPort = CreateOutputPort("Output");

            CreateFields();

            targetObjectNameField.value = sceneImageData.targetObjectName ?? "";
            imageSpriteField.value = sceneImageData.imageSprite;
            opacityField.value = sceneImageData.opacity;

            LoadNotesFromData(data);
            LoadExpandedStateFromData(data);

            RefreshExpandedState();
            RefreshPorts();
        }

        public override BaseNodeData SaveToData()
        {
            var data = new SceneImageNodeData
            {
                guid = Guid,
                nodeType = "SceneImage",
                position = GetPosition().position,
                targetObjectName = TargetObjectName,
                imageSprite = ImageSprite,
                opacity = Opacity
            };
            SaveNotesToData(data);
            SaveExpandedStateToData(data);
            return data;
        }
    }
}
