using System;
using System.Collections.Generic;
using UnityEngine;

namespace VNWinter.DialogueSystem
{
    /// <summary>
    /// Singleton that stores game state variables accessible from dialogue.
    /// Used by VariableNode (set values) and ConditionalNode (check values).
    ///
    /// Setup:
    ///   1. Add this component to a GameObject in your starting scene
    ///   2. Define variables in the Inspector (name + default value)
    ///   3. Variables persist across scenes (DontDestroyOnLoad)
    ///
    /// Dialogue nodes reference variables by string name - ensure names match exactly.
    /// </summary>
    public class GlobalVariables : MonoBehaviour
    {
        /// <summary>Singleton instance. Null until Awake runs or after OnDestroy.</summary>
        public static GlobalVariables Instance { get; private set; }

        [Tooltip("Define integer variables (counters, scores, relationship points, etc.)")]
        [SerializeField] private List<IntVariable> intVariables = new List<IntVariable>();

        [Tooltip("Define boolean variables (flags, has-seen checks, unlocks, etc.)")]
        [SerializeField] private List<BoolVariable> boolVariables = new List<BoolVariable>();

        [Tooltip("Define string variables (names, text values, etc.)")]
        [SerializeField] private List<StringVariable> stringVariables = new List<StringVariable>();

        [Tooltip("Define float variables (timers, positions, etc.)")]
        [SerializeField] private List<FloatVariable> floatVariables = new List<FloatVariable>();

        // Runtime dictionaries for fast lookup
        private Dictionary<string, int> intValues = new Dictionary<string, int>();
        private Dictionary<string, bool> boolValues = new Dictionary<string, bool>();
        private Dictionary<string, string> stringValues = new Dictionary<string, string>();
        private Dictionary<string, float> floatValues = new Dictionary<string, float>();

        /// <summary>Fired when any int variable changes. Parameters: (variableName, newValue)</summary>
        public event Action<string, int> OnIntChanged;

        /// <summary>Fired when any bool variable changes. Parameters: (variableName, newValue)</summary>
        public event Action<string, bool> OnBoolChanged;

        /// <summary>Fired when any string variable changes. Parameters: (variableName, newValue)</summary>
        public event Action<string, string> OnStringChanged;

        /// <summary>Fired when any float variable changes. Parameters: (variableName, newValue)</summary>
        public event Action<string, float> OnFloatChanged;

        private void Awake()
        {
            // Singleton pattern with duplicate protection
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeVariables();
        }

        private void OnDestroy()
        {
            // Clear singleton reference to prevent stale references in editor play mode
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>Copies default values from Inspector lists to runtime dictionaries.</summary>
        private void InitializeVariables()
        {
            foreach (var v in intVariables)
            {
                intValues[v.name] = v.defaultValue;
            }
            foreach (var v in boolVariables)
            {
                boolValues[v.name] = v.defaultValue;
            }
            foreach (var v in stringVariables)
            {
                stringValues[v.name] = v.defaultValue;
            }
            foreach (var v in floatVariables)
            {
                floatValues[v.name] = v.defaultValue;
            }
        }

        #region Int Operations

        /// <summary>Gets an int variable's current value. Returns 0 if not found.</summary>
        public int GetInt(string name)
        {
            return intValues.TryGetValue(name, out var value) ? value : 0;
        }

        /// <summary>Sets an int variable to a specific value. Creates variable if it doesn't exist.</summary>
        public void SetInt(string name, int value)
        {
            int oldValue = GetInt(name);
            intValues[name] = value;

            // Log point changes with stack trace for debugging
            if (name.Contains("points") || name.Contains("Points"))
            {
                string stackTrace = UnityEngine.StackTraceUtility.ExtractStackTrace();
                // Get just the first few relevant lines of the stack trace
                string[] lines = stackTrace.Split('\n');
                string callerInfo = lines.Length > 1 ? lines[1].Trim() : "unknown";
                Debug.Log($"[POINTS] SetInt '{name}': {oldValue} -> {value} | Called from: {callerInfo}");
            }

            OnIntChanged?.Invoke(name, value);
        }

        /// <summary>Adds amount to current value (current + amount).</summary>
        public void AddInt(string name, int amount)
        {
            int current = GetInt(name);

            // Log point additions with stack trace for debugging
            if (name.Contains("points") || name.Contains("Points"))
            {
                string stackTrace = UnityEngine.StackTraceUtility.ExtractStackTrace();
                string[] lines = stackTrace.Split('\n');
                string callerInfo = lines.Length > 1 ? lines[1].Trim() : "unknown";
                Debug.Log($"[POINTS] AddInt '{name}': {current} + {amount} = {current + amount} | Called from: {callerInfo}");
            }

            SetInt(name, current + amount);
        }

        /// <summary>Subtracts amount from current value (current - amount).</summary>
        public void SubtractInt(string name, int amount)
        {
            int current = GetInt(name);

            // Log point subtractions with stack trace for debugging
            if (name.Contains("points") || name.Contains("Points"))
            {
                string stackTrace = UnityEngine.StackTraceUtility.ExtractStackTrace();
                string[] lines = stackTrace.Split('\n');
                string callerInfo = lines.Length > 1 ? lines[1].Trim() : "unknown";
                Debug.Log($"[POINTS] SubtractInt '{name}': {current} - {amount} = {current - amount} | Called from: {callerInfo}");
            }

            SetInt(name, current - amount);
        }

        #endregion

        #region Bool Operations

        /// <summary>Gets a bool variable's current value. Returns false if not found.</summary>
        public bool GetBool(string name)
        {
            return boolValues.TryGetValue(name, out var value) && value;
        }

        /// <summary>Sets a bool variable to a specific value. Creates variable if it doesn't exist.</summary>
        public void SetBool(string name, bool value)
        {
            boolValues[name] = value;
            OnBoolChanged?.Invoke(name, value);
        }

        /// <summary>Flips a bool variable (true becomes false, false becomes true).</summary>
        public void ToggleBool(string name)
        {
            SetBool(name, !GetBool(name));
        }

        #endregion

        #region String Operations

        /// <summary>Gets a string variable's current value. Returns empty string if not found.</summary>
        public string GetString(string name)
        {
            return stringValues.TryGetValue(name, out var value) ? value : string.Empty;
        }

        /// <summary>Sets a string variable to a specific value. Creates variable if it doesn't exist.</summary>
        public void SetString(string name, string value)
        {
            stringValues[name] = value ?? string.Empty;
            OnStringChanged?.Invoke(name, value);
        }

        #endregion

        #region Float Operations

        /// <summary>Gets a float variable's current value. Returns 0 if not found.</summary>
        public float GetFloat(string name)
        {
            return floatValues.TryGetValue(name, out var value) ? value : 0f;
        }

        /// <summary>Sets a float variable to a specific value. Creates variable if it doesn't exist.</summary>
        public void SetFloat(string name, float value)
        {
            floatValues[name] = value;
            OnFloatChanged?.Invoke(name, value);
        }

        #endregion

        /// <summary>Resets all variables to their Inspector-defined default values.</summary>
        public void ResetAll()
        {
            InitializeVariables();
        }

        /// <summary>Serializable definition for an integer variable shown in Inspector.</summary>
        [Serializable]
        public class IntVariable
        {
            [Tooltip("Variable name - must match exactly in dialogue nodes")]
            public string name;

            [Tooltip("Starting value when game begins or after ResetAll()")]
            public int defaultValue;
        }

        /// <summary>Serializable definition for a boolean variable shown in Inspector.</summary>
        [Serializable]
        public class BoolVariable
        {
            [Tooltip("Variable name - must match exactly in dialogue nodes")]
            public string name;

            [Tooltip("Starting value when game begins or after ResetAll()")]
            public bool defaultValue;
        }

        /// <summary>Serializable definition for a string variable shown in Inspector.</summary>
        [Serializable]
        public class StringVariable
        {
            [Tooltip("Variable name - must match exactly in dialogue nodes")]
            public string name;

            [Tooltip("Starting value when game begins or after ResetAll()")]
            public string defaultValue;
        }

        /// <summary>Serializable definition for a float variable shown in Inspector.</summary>
        [Serializable]
        public class FloatVariable
        {
            [Tooltip("Variable name - must match exactly in dialogue nodes")]
            public string name;

            [Tooltip("Starting value when game begins or after ResetAll()")]
            public float defaultValue;
        }
    }
}
