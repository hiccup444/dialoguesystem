using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace VNWinter.DialogueGraph.Editor
{
    public class DialogueGraphEditorWindow : EditorWindow
    {
        private DialogueGraphView graphView;
        private DialogueGraphAsset currentAsset;
        private Label assetNameLabel;
        private VisualElement validationPanel;
        private ListView validationListView;
        private List<ValidationResult> validationResults = new List<ValidationResult>();

        // Search
        private VisualElement searchPanel;
        private TextField searchField;
        private EnumField searchTypeField;
        private ListView searchResultsListView;
        private List<BaseDialogueNode> searchResults = new List<BaseDialogueNode>();

        // Unsaved changes tracking
        private bool isDirty;
        private bool isClosingWindow;

        [MenuItem("VNWinter/Dialogue Graph Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<DialogueGraphEditorWindow>();
            window.titleContent = new GUIContent("Dialogue Graph");
            window.minSize = new Vector2(400, 300);
        }

        public static void OpenWindow(DialogueGraphAsset asset)
        {
            var window = GetWindow<DialogueGraphEditorWindow>();
            window.titleContent = new GUIContent("Dialogue Graph");
            window.minSize = new Vector2(400, 300);
            window.LoadAsset(asset);
        }

        private void OnEnable()
        {
            ConstructGraphView();
            GenerateToolbar();
            CreateValidationPanel();
            CreateSearchPanel();

            // Register keyboard shortcut for Ctrl+S and Ctrl+F
            rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyDown);
            rootVisualElement.focusable = true;

            // Subscribe to undo/redo
            Undo.undoRedoPerformed += OnUndoRedo;

            // Set the save changes message for Unity's built-in prompt
            saveChangesMessage = "The dialogue graph has unsaved changes. Do you want to save before closing?";
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;

            if (graphView != null)
            {
                rootVisualElement.Remove(graphView);
            }
        }

        private void OnDestroy()
        {
            // Check handled in SaveChangesIfNeeded - but OnDestroy is too late for prompts
            // The actual prompt happens in ShowButton/Close handling
        }

        /// <summary>
        /// Called when attempting to close the window. Shows save prompt if there are unsaved changes.
        /// </summary>
        public bool SaveChangesIfNeeded()
        {
            if (!isDirty || currentAsset == null)
                return true;

            int result = EditorUtility.DisplayDialogComplex(
                "Unsaved Changes",
                $"The dialogue graph '{currentAsset.name}' has unsaved changes. Do you want to save before closing?",
                "Save",      // Returns 0
                "Cancel",    // Returns 1
                "Don't Save" // Returns 2
            );

            switch (result)
            {
                case 0: // Save
                    SaveCurrentAsset();
                    return true;
                case 1: // Cancel
                    return false;
                case 2: // Don't Save
                    return true;
                default:
                    return true;
            }
        }

        // Override to use Unity's built-in save prompt system
        public override void SaveChanges()
        {
            SaveCurrentAsset();
            base.SaveChanges();
        }

        private void OnUndoRedo()
        {
            if (currentAsset != null)
            {
                DialogueGraphSaveUtility.LoadGraph(graphView, currentAsset);
            }
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.ctrlKey && evt.keyCode == KeyCode.S)
            {
                SaveCurrentAsset();
                evt.StopPropagation();
            }
            else if (evt.ctrlKey && evt.keyCode == KeyCode.C)
            {
                graphView?.CopySelection();
                evt.StopPropagation();
            }
            else if (evt.ctrlKey && evt.keyCode == KeyCode.V)
            {
                graphView?.Paste();
                evt.StopPropagation();
            }
            else if (evt.ctrlKey && evt.keyCode == KeyCode.D)
            {
                graphView?.DuplicateSelection();
                evt.StopPropagation();
            }
            else if (evt.ctrlKey && evt.keyCode == KeyCode.F)
            {
                ToggleSearchPanel();
                evt.StopPropagation();
            }
        }

        private void ToggleSearchPanel()
        {
            if (searchPanel.style.display == DisplayStyle.None)
            {
                searchPanel.style.display = DisplayStyle.Flex;
                searchField.Focus();
            }
            else
            {
                searchPanel.style.display = DisplayStyle.None;
            }
        }

        private void ConstructGraphView()
        {
            graphView = new DialogueGraphView();
            graphView.StretchToParentSize();
            rootVisualElement.Add(graphView);

            // Subscribe to graph changes to track unsaved changes
            graphView.graphViewChanged += OnGraphViewChangedForDirtyTracking;
        }

        private GraphViewChange OnGraphViewChangedForDirtyTracking(GraphViewChange change)
        {
            // Mark as dirty when nodes or edges are added, removed, or moved
            if (change.elementsToRemove != null || change.edgesToCreate != null || change.movedElements != null)
            {
                MarkDirty();
            }
            return change;
        }

        private void MarkDirty()
        {
            if (!isDirty)
            {
                isDirty = true;
                // Use Unity's built-in unsaved changes system for automatic close prompt
                hasUnsavedChanges = true;
                UpdateWindowTitle();
            }
        }

        private void ClearDirty()
        {
            if (isDirty)
            {
                isDirty = false;
                hasUnsavedChanges = false;
                UpdateWindowTitle();
            }
        }

        private void UpdateWindowTitle()
        {
            string baseName = currentAsset != null ? currentAsset.name : "Dialogue Graph";
            titleContent = new GUIContent(isDirty ? $"*{baseName}" : baseName);
        }

        private void GenerateToolbar()
        {
            var toolbar = new Toolbar();

            assetNameLabel = new Label("No Asset Loaded");
            assetNameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            assetNameLabel.style.marginLeft = 5;
            assetNameLabel.style.marginRight = 10;
            assetNameLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            toolbar.Add(assetNameLabel);

            var loadButton = new ToolbarButton(() => LoadAssetDialog()) { text = "Load" };
            toolbar.Add(loadButton);

            var saveButton = new ToolbarButton(() => SaveCurrentAsset()) { text = "Save" };
            toolbar.Add(saveButton);

            var newButton = new ToolbarButton(() => CreateNewAsset()) { text = "New" };
            toolbar.Add(newButton);

            var spacer = new ToolbarSpacer();
            spacer.style.flexGrow = 1;
            toolbar.Add(spacer);

            var validateButton = new ToolbarButton(() => ValidateGraph()) { text = "Validate" };
            toolbar.Add(validateButton);

            var minimapToggle = new ToolbarToggle { text = "Minimap" };
            minimapToggle.value = true;
            minimapToggle.RegisterValueChangedCallback(evt => graphView.ToggleMiniMap(evt.newValue));
            toolbar.Add(minimapToggle);

            rootVisualElement.Insert(0, toolbar);
        }

        private void CreateValidationPanel()
        {
            validationPanel = new VisualElement();
            validationPanel.style.position = Position.Absolute;
            validationPanel.style.right = 10;
            validationPanel.style.top = 40;
            validationPanel.style.width = 350;
            validationPanel.style.maxHeight = 300;
            validationPanel.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.95f);
            validationPanel.style.borderTopLeftRadius = 4;
            validationPanel.style.borderTopRightRadius = 4;
            validationPanel.style.borderBottomLeftRadius = 4;
            validationPanel.style.borderBottomRightRadius = 4;
            validationPanel.style.display = DisplayStyle.None;

            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.paddingLeft = 8;
            header.style.paddingRight = 8;
            header.style.paddingTop = 4;
            header.style.paddingBottom = 4;
            header.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);

            var titleLabel = new Label("Validation Results");
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(titleLabel);

            var closeButton = new Button(() => validationPanel.style.display = DisplayStyle.None) { text = "X" };
            closeButton.style.width = 20;
            closeButton.style.height = 20;
            header.Add(closeButton);

            validationPanel.Add(header);

            validationListView = new ListView();
            validationListView.makeItem = () =>
            {
                var item = new VisualElement();
                item.style.flexDirection = FlexDirection.Row;
                item.style.paddingLeft = 8;
                item.style.paddingRight = 8;
                item.style.paddingTop = 4;
                item.style.paddingBottom = 4;

                var icon = new Label();
                icon.style.width = 20;
                icon.style.unityTextAlign = TextAnchor.MiddleCenter;
                icon.name = "icon";
                item.Add(icon);

                var message = new Label();
                message.style.flexGrow = 1;
                message.style.whiteSpace = WhiteSpace.Normal;
                message.name = "message";
                item.Add(message);

                return item;
            };
            validationListView.bindItem = (element, index) =>
            {
                var result = validationResults[index];
                var icon = element.Q<Label>("icon");
                var message = element.Q<Label>("message");

                icon.text = result.Severity switch
                {
                    ValidationSeverity.Error => "X",
                    ValidationSeverity.Warning => "!",
                    ValidationSeverity.Info => "i",
                    _ => "?"
                };
                icon.style.color = result.Severity switch
                {
                    ValidationSeverity.Error => Color.red,
                    ValidationSeverity.Warning => Color.yellow,
                    ValidationSeverity.Info => Color.cyan,
                    _ => Color.white
                };

                message.text = result.Message;
            };
            validationListView.itemsSource = validationResults;
            validationListView.fixedItemHeight = 30;
            validationListView.style.flexGrow = 1;
            validationListView.style.maxHeight = 250;
            validationListView.selectionChanged += OnValidationItemSelected;

            validationPanel.Add(validationListView);

            rootVisualElement.Add(validationPanel);
        }

        private void CreateSearchPanel()
        {
            searchPanel = new VisualElement();
            searchPanel.style.position = Position.Absolute;
            searchPanel.style.left = 10;
            searchPanel.style.top = 40;
            searchPanel.style.width = 300;
            searchPanel.style.maxHeight = 350;
            searchPanel.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.95f);
            searchPanel.style.borderTopLeftRadius = 4;
            searchPanel.style.borderTopRightRadius = 4;
            searchPanel.style.borderBottomLeftRadius = 4;
            searchPanel.style.borderBottomRightRadius = 4;
            searchPanel.style.display = DisplayStyle.None;

            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.paddingLeft = 8;
            header.style.paddingRight = 8;
            header.style.paddingTop = 4;
            header.style.paddingBottom = 4;
            header.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);

            var titleLabel = new Label("Search (Ctrl+F)");
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(titleLabel);

            var closeButton = new Button(() => searchPanel.style.display = DisplayStyle.None) { text = "X" };
            closeButton.style.width = 20;
            closeButton.style.height = 20;
            header.Add(closeButton);

            searchPanel.Add(header);

            var searchContainer = new VisualElement();
            searchContainer.style.paddingLeft = 8;
            searchContainer.style.paddingRight = 8;
            searchContainer.style.paddingTop = 4;
            searchContainer.style.paddingBottom = 4;

            searchField = new TextField("Search");
            searchField.RegisterValueChangedCallback(evt => PerformSearch());
            searchContainer.Add(searchField);

            searchTypeField = new EnumField("Search In", SearchType.All);
            searchTypeField.RegisterValueChangedCallback(evt => PerformSearch());
            searchTypeField.style.marginTop = 4;
            searchContainer.Add(searchTypeField);

            searchPanel.Add(searchContainer);

            searchResultsListView = new ListView();
            searchResultsListView.makeItem = () =>
            {
                var item = new Label();
                item.style.paddingLeft = 8;
                item.style.paddingRight = 8;
                item.style.paddingTop = 4;
                item.style.paddingBottom = 4;
                return item;
            };
            searchResultsListView.bindItem = (element, index) =>
            {
                var label = element as Label;
                var node = searchResults[index];
                label.text = GetSearchResultText(node);
            };
            searchResultsListView.itemsSource = searchResults;
            searchResultsListView.fixedItemHeight = 24;
            searchResultsListView.style.flexGrow = 1;
            searchResultsListView.style.maxHeight = 200;
            searchResultsListView.selectionChanged += OnSearchResultSelected;

            searchPanel.Add(searchResultsListView);

            rootVisualElement.Add(searchPanel);
        }

        private string GetSearchResultText(BaseDialogueNode node)
        {
            return node switch
            {
                DialogueNode dn => $"Dialogue: {dn.SpeakerName ?? "unnamed"}",
                ChoiceNode => "Choice",
                VariableNode vn => $"Variable: {vn.VariableName}",
                ConditionalNode cn => $"Conditional: {cn.VariableName}",
                EventNode => "Event",
                _ => node.title
            };
        }

        private void PerformSearch()
        {
            var query = searchField.value;
            var searchType = (SearchType)searchTypeField.value;

            searchResults.Clear();

            if (!string.IsNullOrWhiteSpace(query))
            {
                searchResults.AddRange(graphView.SearchNodes(query, searchType));
            }

            searchResultsListView.Rebuild();
        }

        private void OnSearchResultSelected(IEnumerable<object> selectedItems)
        {
            foreach (var item in selectedItems)
            {
                if (item is BaseDialogueNode node)
                {
                    graphView.FocusOnNode(node.Guid);
                    // Clear selection so clicking the same item again will trigger the event
                    searchResultsListView.ClearSelection();
                    break;
                }
            }
        }

        private void OnValidationItemSelected(IEnumerable<object> selectedItems)
        {
            foreach (var item in selectedItems)
            {
                if (item is ValidationResult result && !string.IsNullOrEmpty(result.NodeGuid))
                {
                    graphView.FocusOnNode(result.NodeGuid);
                    // Clear selection so clicking the same item again will trigger the event
                    validationListView.ClearSelection();
                    break;
                }
            }
        }

        private void ValidateGraph()
        {
            if (currentAsset == null)
            {
                Debug.LogWarning("No asset loaded to validate");
                return;
            }

            // Save first to ensure we're validating the latest changes
            DialogueGraphSaveUtility.SaveGraph(graphView, currentAsset);

            validationResults.Clear();
            validationResults.AddRange(DialogueGraphValidator.Validate(currentAsset));

            validationListView.Rebuild();
            validationPanel.style.display = DisplayStyle.Flex;

            if (validationResults.Count == 0)
            {
                validationResults.Add(new ValidationResult(ValidationSeverity.Info, "No issues found!"));
                validationListView.Rebuild();
            }

            Debug.Log($"Validation complete: {validationResults.Count} issue(s) found");
        }

        private void LoadAssetDialog()
        {
            // Prompt to save if there are unsaved changes
            if (isDirty && currentAsset != null)
            {
                if (!SaveChangesIfNeeded())
                    return; // User cancelled
            }

            var path = EditorUtility.OpenFilePanel("Load Dialogue Graph", "Assets", "asset");
            if (!string.IsNullOrEmpty(path))
            {
                if (path.StartsWith(Application.dataPath))
                {
                    path = "Assets" + path.Substring(Application.dataPath.Length);
                    var asset = AssetDatabase.LoadAssetAtPath<DialogueGraphAsset>(path);
                    if (asset != null)
                    {
                        LoadAsset(asset);
                    }
                    else
                    {
                        Debug.LogError("Selected file is not a DialogueGraphAsset");
                    }
                }
            }
        }

        public void LoadAsset(DialogueGraphAsset asset)
        {
            currentAsset = asset;
            assetNameLabel.text = asset.name;
            isDirty = false;
            hasUnsavedChanges = false;
            UpdateWindowTitle();
            DialogueGraphSaveUtility.LoadGraph(graphView, asset);
        }

        private void SaveCurrentAsset()
        {
            if (currentAsset == null)
            {
                CreateNewAsset();
                return;
            }

            DialogueGraphSaveUtility.SaveGraph(graphView, currentAsset);
            ClearDirty();
            Debug.Log($"Graph saved: {currentAsset.name}");
        }

        private void CreateNewAsset()
        {
            // Prompt to save if there are unsaved changes
            if (isDirty && currentAsset != null)
            {
                if (!SaveChangesIfNeeded())
                    return; // User cancelled
            }

            var path = EditorUtility.SaveFilePanelInProject(
                "Create New Dialogue Graph",
                "NewDialogueGraph",
                "asset",
                "Enter a name for the new dialogue graph");

            if (!string.IsNullOrEmpty(path))
            {
                var asset = ScriptableObject.CreateInstance<DialogueGraphAsset>();
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
                LoadAsset(asset);
            }
        }

        private void OnGUI()
        {
            // Backup keyboard handling for Ctrl+S
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.control && Event.current.keyCode == KeyCode.S)
                {
                    SaveCurrentAsset();
                    Event.current.Use();
                }
            }
        }
    }
}
