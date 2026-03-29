using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace VNWinter.DialogueGraph.Editor
{
    /// <summary>
    /// Helper that adds a dragable resize corner to GraphView elements.
    /// </summary>
    public class GraphElementResizer : VisualElement
    {
        private readonly float minWidth;
        private readonly float minHeight;
        private Vector2 startMousePos;
        private Vector2 startSize;
        private bool isResizing;

        public GraphElementResizer(float minWidth = 150f, float minHeight = 120f)
        {
            this.minWidth = minWidth;
            this.minHeight = minHeight;

            style.position = Position.Absolute;
            style.right = 0;
            style.bottom = 0;
            style.width = 16;
            style.height = 16;
            style.cursor = new StyleCursor();

            // Visual indicator lines
            for (int i = 0; i < 3; i++)
            {
                var line = new VisualElement();
                line.style.position = Position.Absolute;
                line.style.right = 3 + i * 4;
                line.style.bottom = 3;
                line.style.width = 2;
                line.style.height = 8 - i * 2;
                line.style.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
                line.pickingMode = PickingMode.Ignore;
                Add(line);
            }

            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.button != 0 || parent == null) return;
            isResizing = true;
            startMousePos = evt.mousePosition;
            startSize = new Vector2(parent.resolvedStyle.width, parent.resolvedStyle.height);
            evt.StopPropagation();
        }

        private void OnMouseMove(MouseMoveEvent evt)
        {
            if (!isResizing || parent == null) return;
            Vector2 delta = evt.mousePosition - startMousePos;
            float newWidth = Mathf.Max(minWidth, startSize.x + delta.x);
            float newHeight = Mathf.Max(minHeight, startSize.y + delta.y);

            parent.style.width = newWidth;
            parent.style.height = newHeight;

            if (parent is GraphElement graphElement)
            {
                var rect = graphElement.GetPosition();
                graphElement.SetPosition(new Rect(rect.x, rect.y, newWidth, newHeight));
            }

            evt.StopPropagation();
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            if (evt.button != 0) return;
            isResizing = false;
            evt.StopPropagation();
        }
    }
}
