using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VNWinter.DialogueSystem
{
    [RequireComponent(typeof(Button))]
    public class ChoiceButton : MonoBehaviour
    {
        [SerializeField] private TMP_Text choiceText;

        private Button button;
        private int choiceIndex;
        private Action<int> onClick;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        public void Initialize(int index, string text, Action<int> callback)
        {
            choiceIndex = index;
            choiceText.text = text;
            onClick = callback;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);
        }

        private void HandleClick()
        {
            onClick?.Invoke(choiceIndex);
        }

        public void SetInteractable(bool interactable)
        {
            button.interactable = interactable;
        }
    }
}
