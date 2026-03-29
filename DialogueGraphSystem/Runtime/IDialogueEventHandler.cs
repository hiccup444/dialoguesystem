using VNWinter.DialogueGraph;

namespace VNWinter.DialogueSystem
{
    /// <summary>
    /// Interface for components that handle dialogue events triggered by Event nodes.
    /// Implement this interface to respond to events like playing animations,
    /// changing backgrounds, playing music, etc.
    /// </summary>
    public interface IDialogueEventHandler
    {
        /// <summary>
        /// Called when an Event node is processed in the dialogue graph.
        /// </summary>
        /// <param name="eventData">The event data containing type, assets, and parameters.</param>
        void HandleDialogueEvent(EventNodeData eventData);

        /// <summary>
        /// Check if this handler can process the given event type.
        /// </summary>
        /// <param name="eventType">The type of event to check.</param>
        /// <returns>True if this handler can process the event type.</returns>
        bool CanHandle(DialogueEventType eventType);
    }
}
