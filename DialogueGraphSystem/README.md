# Dialogue Graph System

A visual node-based dialogue system for Unity, built with the GraphView API. Drop into any Unity project and write branching dialogue, variable logic, audio triggers, and screen effects without writing code.

---

## Requirements

- Unity 2021.3+
- TextMeshPro
- Unity Input System (for DialogueTrigger)

---

## Drop-in Setup (New Project)

1. **Copy the folder** `Assets/DialogueGraphSystem/` into your project's `Assets/`.
2. In your scene, create a **Canvas** (Screen Space – Overlay) and build the dialogue UI (see [UI Setup](#ui-setup)).
3. Create a **DialogueManager** prefab (an empty GameObject) and add:
   - `DialogueManager` component — drag in your `DialogueUI` object.
   - `DialogueTrigger` component — drag in the `DialogueManager` and a `DialogueGraphAsset`.
   - _(Optional)_ `ScreenFader` component for fade nodes.
4. Create a **DialogueGraphAsset** via `Right-click → Create → VNWinter → Dialogue Graph` and open it in **VNWinter → Dialogue Graph Editor**.
5. Build your dialogue graph, press **Ctrl+S** to save.
6. Assign the asset to `DialogueTrigger.dialogueGraph` and hit Play.

---

## UI Setup

The `DialogueUI` component expects the following objects assigned in the Inspector:

| Field | Description |
|---|---|
| `dialoguePanel` | Root panel toggled when dialogue is active |
| `dialogueRootCanvasGroup` | CanvasGroup on the root for fade in/out |
| `speakerNameText` | TMP_Text for the character name |
| `speakerNamePanel` | Panel containing the name (hidden when no speaker) |
| `dialogueText` | TMP_Text for dialogue body |
| `portraitImage` | Back/main portrait Image |
| `portraitImageFront1/2/3` | Front portrait Image slots |
| `portraitPanel` | Panel containing portraits |
| `continueIndicator` | Pulsing "click to continue" icon |
| `nextIconCanvasGroup` | CanvasGroup for the continue icon fade |
| `choiceButtons` | List of `ChoiceButton` components |
| `portraitSwayController` | `PortraitSwayController` on the portrait panel |
| `dialoguePanelImage` | Image component for swapping dialogue box sprite |

Add `TypewriterEffect` to the **same GameObject** as `DialogueUI` — it is found via `GetComponent`.

---

## Node Reference

### Core Flow

| Node | Description |
|---|---|
| **Start** | Entry point. Every graph needs exactly one. |
| **Dialogue** | Displays text with speaker name and optional portrait. |
| **Choice** | Presents up to N buttons; each connects to a different path. |
| **End** | Terminates dialogue and fires `OnDialogueEnded`. |
| **Graph Transition** | Jumps to a different `DialogueGraphAsset` seamlessly. |
| **Random** | Randomly picks one of up to 4 output paths. |
| **Prevent Auto Trigger** | If dialogue was auto-started (e.g. room entry), ends here. Manual triggers pass through. |

### Logic

| Node | Description |
|---|---|
| **Variable** | Sets/adds/subtracts an int, or sets/toggles a bool in `GlobalVariables`. |
| **Conditional** | Branches **True/False** based on a `GlobalVariables` int or bool check. |

### Effects

| Node | Description |
|---|---|
| **Fade** | Fades the screen to/from a colour. Requires `ScreenFader` in the scene. |
| **Event** | Fires `OnDialogueEvent` with a type and optional key, target, audio clip, etc. Use **Custom** type + `eventKey` for anything project-specific. |
| **Scene Image** | Finds a `GameObject` by name and replaces its `Image` sprite and opacity. |
| **Room Transition** | Fires `OnRoomTransitionExecuted` with a scene name string. |

### Audio

| Node | Description |
|---|---|
| **Audio** | Fires `OnAudioNodeExecuted` (play clip / fade volume on Music, SFX, or Voice channel). |
| **Stop Sound** | Fires `OnStopSoundNodeExecuted` (stop a channel with optional fade-out). |
| **Save Audio Playback** | Fires `OnSaveAudioPlaybackExecuted` (save current music position). |

---

## Hooking Up Audio & Scene Transitions

The package fires events instead of calling any specific audio or scene management system, so it stays dependency-free.

Subscribe in your own MonoBehaviour:

```csharp
void Start()
{
    var dm = FindObjectOfType<DialogueManager>();

    // Audio
    dm.OnAudioNodeExecuted += node => myAudioSystem.Play(node.audioClip, node.loop);
    dm.OnStopSoundNodeExecuted += node => myAudioSystem.Stop(node.channelType);

    // Room / scene transitions
    dm.OnRoomTransitionExecuted += node => SceneManager.LoadScene(node.targetSceneName);

    // Poll while transitioning (optional – enables "wait for completion" on the node)
    dm.IsRoomTransitioning = () => myTransitionSystem.IsTransitioning;

    // Custom events
    dm.OnDialogueEvent += e =>
    {
        if (e.eventType == DialogueEventType.Custom && e.eventKey == "OpenShop")
            shopUI.Open();
    };
}
```

---

## Emotion Swap (Portrait Swapping)

Dialogue nodes support swapping to a different portrait mid-line. Because CharacterData is project-specific, the system calls a delegate you provide:

```csharp
// speakerName  – value from the Dialogue node's Speaker Name field
// emotionName  – "neutral", "positive", or "negative"
dm.GetEmotionSprite = (speakerName, emotionName) =>
{
    return myCharacterDB.GetPortrait(speakerName, emotionName);
};
```

Return `null` to skip the swap.

---

## Variable System (`GlobalVariables`)

A singleton ScriptableObject-style store for int, bool, string, and float variables.

```csharp
// Reading
int pts = GlobalVariables.Instance.GetInt("player_score");
bool flag = GlobalVariables.Instance.GetBool("met_merchant");
string name = GlobalVariables.Instance.GetString("player_name");

// Writing
GlobalVariables.Instance.SetInt("player_score", pts + 10);
GlobalVariables.Instance.SetBool("met_merchant", true);

// Subscribe to changes
GlobalVariables.Instance.OnIntChanged += (key, value) => Debug.Log($"{key} = {value}");
```

Variable nodes in the graph write to this store; Conditional nodes read from it.

**Text substitution** — use `{variableName}` anywhere in dialogue text:

```
"Your score is {player_score} points."
```

---

## DialogueTrigger

`DialogueTrigger` handles input (Space, Enter, left mouse click) and forwards it to `DialogueManager`.

| Field | Description |
|---|---|
| `dialogueGraph` | Graph to play when `StartDialogue()` is called. |
| `dialogueManager` | The `DialogueManager` in the scene. |
| `startOnAwake` | Auto-starts dialogue when the scene loads. |
| `ignoreClickPanel` | Optional `RectTransform` — clicks over this panel are ignored (useful for overlaid UI like a phone). |

To start dialogue from code:

```csharp
dialogueTrigger.SetDialogueGraph(myGraph);
dialogueTrigger.StartDialogue();
```

Or call `DialogueManager.StartDialogue(graph)` directly.

---

## Editor Shortcuts

| Shortcut | Action |
|---|---|
| `Ctrl+S` | Save graph |
| `Ctrl+F` | Open search |
| Right-click canvas | Add node |
| Ctrl+C / Ctrl+V | Copy / paste selected nodes |

---

## Using in This Project (VNWinter)

This project includes `Assets/Scripts/GameExtensions/DialogueGameBridge.cs`. Add it to the same GameObject as `DialogueManager` — it wires up:

- `AudioManager` → handles `OnAudioNodeExecuted`, `OnStopSoundNodeExecuted`, `OnSaveAudioPlaybackExecuted`
- `RoomManager` → handles `OnRoomTransitionExecuted` and provides `IsRoomTransitioning`. Assign all `RoomData` assets to `DialogueGameBridge.allRooms` — the node matches `targetSceneName` against `RoomData.roomId`.
- `CharacterData.portraits` → provides `GetEmotionSprite` from the current room's character placements
- `FrostingController` → handles the `Custom / "StartFrosting"` event

**Note:** Room Transition nodes in existing dialogue graphs stored a `RoomData` reference. After this refactor they use a `string targetSceneName`. Open any graph that had Room Transition nodes in the editor and re-enter the scene name in the text field, then save.

---

## Package Folder Structure

```
Assets/DialogueGraphSystem/
├── Runtime/
│   ├── NodeData.cs               — All node data types (serializable)
│   ├── DialogueGraphAsset.cs     — ScriptableObject container
│   ├── GlobalVariables.cs        — In/out variable store singleton
│   ├── DialogueManager.cs        — Graph traversal and state machine
│   ├── DialogueUI.cs             — Canvas / portrait / choice rendering
│   ├── DialogueTrigger.cs        — Input → advance dialogue
│   ├── TypewriterEffect.cs       — Character-by-character text reveal
│   ├── ChoiceButton.cs           — Button wiring for choices
│   ├── ScreenFader.cs            — Full-screen colour fade
│   ├── PortraitSwayController.cs — Looping portrait animations
│   └── ...
└── Editor/
    ├── DialogueGraphEditorWindow.cs  — Main graph editor window
    ├── DialogueGraphView.cs          — GraphView canvas
    ├── DialogueGraphSaveUtility.cs   — Serialise nodes ↔ asset
    ├── DialogueGraphValidator.cs     — Error / warning checker
    ├── Resources/
    │   └── DialogueGraphStyles.uss   — Visual styling
    └── Nodes/
        └── *.cs                      — Visual node editors (one per type)
```
