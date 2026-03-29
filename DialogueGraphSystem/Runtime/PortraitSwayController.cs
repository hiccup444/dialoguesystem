using UnityEngine;
using VNWinter.DialogueGraph;

namespace VNWinter.DialogueSystem
{
    /// <summary>
    /// Handles simple sway motion for a portrait RectTransform.
    /// Configured per-dialogue line via DialogueNodeData.
    /// </summary>
    public class PortraitSwayController : MonoBehaviour
    {
        [SerializeField] private RectTransform target;
        [SerializeField] private float defaultIntensity = 10f;

        private bool sway1Enabled;
        private bool sway2Enabled;
        private bool rotationEnabled;
        private bool scaleEnabled;

        private SwayPattern sway1Pattern = SwayPattern.LeftRight;
        private SwayPattern sway2Pattern = SwayPattern.UpDown;
        private float sway1Speed = 1f;
        private float sway2Speed = 1f;
        private float sway1Intensity = 10f;
        private float sway2Intensity = 10f;

        private RotationAxis rotationAxis = RotationAxis.Z;
        private float rotationMinAngle = -5f;
        private float rotationMaxAngle = 5f;
        private float rotationSpeed = 1f;

        private float scaleMin = 1f;
        private float scaleMax = 1f;
        private float scaleSpeed = 1f;

        private Vector2 baseAnchoredPosition;
        private Quaternion baseRotation;
        private Vector3 baseScale;

        private float sway1Time;
        private float sway2Time;
        private float rotationTime;
        private float scaleTime;

        /// <summary>Current target RectTransform being animated.</summary>
        public RectTransform Target => target;

        /// <summary>Assigns a new target for swaying.</summary>
        public void SetTarget(RectTransform newTarget)
        {
            // Only restore transforms when changing to a different target
            if (target != null && target != newTarget)
            {
                ResetTargetTransforms();
            }

            target = newTarget;
            CacheBaseTransforms();
        }

        /// <summary>
        /// Apply animation settings for the current dialogue line.
        /// </summary>
        public void ApplySettings(
            RectTransform newTarget,
            bool enableSway1,
            SwayPattern swayPattern1,
            float swaySpeed1,
            float swayIntensity1,
            bool enableSway2,
            SwayPattern swayPattern2,
            float swaySpeed2,
            float swayIntensity2,
            bool enableRotation,
            RotationAxis rotationAxisValue,
            float rotationMin,
            float rotationMax,
            float rotationSpeedValue,
            bool enableScale,
            float scaleMinValue,
            float scaleMaxValue,
            float scaleSpeedValue)
        {
            bool targetChanged = target != newTarget;
            SetTarget(newTarget);

            if (target == null)
            {
                StopSway(true);
                return;
            }

            sway1Enabled = enableSway1;
            sway2Enabled = enableSway2;
            rotationEnabled = enableRotation;
            scaleEnabled = enableScale;

            sway1Pattern = swayPattern1;
            sway2Pattern = swayPattern2;
            sway1Speed = Mathf.Max(0f, swaySpeed1);
            sway2Speed = Mathf.Max(0f, swaySpeed2);
            sway1Intensity = swayIntensity1 > 0f ? swayIntensity1 : defaultIntensity;
            sway2Intensity = swayIntensity2 > 0f ? swayIntensity2 : defaultIntensity;

            rotationAxis = rotationAxisValue;
            rotationMinAngle = rotationMin;
            rotationMaxAngle = rotationMax;
            rotationSpeed = Mathf.Max(0f, rotationSpeedValue);

            scaleMin = scaleMinValue;
            scaleMax = scaleMaxValue;
            scaleSpeed = Mathf.Max(0f, scaleSpeedValue);

            bool anyActive = sway1Enabled || sway2Enabled || rotationEnabled || scaleEnabled;
            if (!anyActive)
            {
                StopSway(true);
                return;
            }

            CacheBaseTransforms();

            // Only reset phase when switching to a different target; otherwise continue smoothly
            if (targetChanged)
            {
                sway1Time = sway2Time = rotationTime = scaleTime = 0f;
            }
        }

        /// <summary>Stops swaying and optionally restores the anchored position.</summary>
        public void StopSway(bool resetPosition)
        {
            sway1Enabled = false;
            sway2Enabled = false;
            rotationEnabled = false;
            scaleEnabled = false;

            if (resetPosition)
            {
                ResetTargetTransforms();
            }
        }

        private void Update()
        {
            if (target == null)
                return;

            if (!(sway1Enabled || sway2Enabled || rotationEnabled || scaleEnabled))
                return;

            float deltaTime = Time.unscaledDeltaTime;
            Vector2 position = baseAnchoredPosition;

            if (sway1Enabled && sway1Intensity > 0f)
            {
                position += CalculateOffset(sway1Pattern, sway1Time, sway1Intensity);
                sway1Time += deltaTime * sway1Speed;
            }

            if (sway2Enabled && sway2Intensity > 0f)
            {
                position += CalculateOffset(sway2Pattern, sway2Time, sway2Intensity);
                sway2Time += deltaTime * sway2Speed;
            }

            target.anchoredPosition = position;

            if (rotationEnabled)
            {
                float angle = CalculateOscillation(rotationMinAngle, rotationMaxAngle, rotationTime);
                ApplyRotation(angle);
                rotationTime += deltaTime * rotationSpeed;
            }

            if (scaleEnabled)
            {
                float scaleFactor = CalculateOscillation(scaleMin, scaleMax, scaleTime);
                ApplyScale(scaleFactor);
                scaleTime += deltaTime * scaleSpeed;
            }
        }

        private void CacheBaseTransforms()
        {
            if (target != null)
            {
                baseAnchoredPosition = target.anchoredPosition;
                baseRotation = target.localRotation;
                baseScale = target.localScale;
            }
        }

        private void ResetTargetTransforms()
        {
            if (target != null)
            {
                target.anchoredPosition = baseAnchoredPosition;
                target.localRotation = baseRotation;
                target.localScale = baseScale;
            }
        }

        private Vector2 CalculateOffset(SwayPattern pattern, float time, float amplitude)
        {
            float t = time;

            switch (pattern)
            {
                case SwayPattern.LeftRight:
                    return new Vector2(Mathf.Sin(t) * amplitude, 0f);
                case SwayPattern.UpDown:
                    return new Vector2(0f, Mathf.Sin(t) * amplitude);
                case SwayPattern.Circular:
                    return new Vector2(Mathf.Sin(t) * amplitude, Mathf.Cos(t) * amplitude);
                case SwayPattern.Figure8:
                    return new Vector2(Mathf.Sin(t) * amplitude, Mathf.Sin(t * 2f) * amplitude * 0.5f);
                default:
                    return Vector2.zero;
            }
        }

        private float CalculateOscillation(float min, float max, float time)
        {
            float amplitude = (max - min) * 0.5f;
            float midpoint = (max + min) * 0.5f;
            return midpoint + Mathf.Sin(time) * amplitude;
        }

        private void ApplyRotation(float angle)
        {
            Vector3 baseEuler = baseRotation.eulerAngles;
            switch (rotationAxis)
            {
                case RotationAxis.X:
                    baseEuler.x = baseRotation.eulerAngles.x + angle;
                    break;
                case RotationAxis.Y:
                    baseEuler.y = baseRotation.eulerAngles.y + angle;
                    break;
                case RotationAxis.Z:
                    baseEuler.z = baseRotation.eulerAngles.z + angle;
                    break;
            }
            target.localRotation = Quaternion.Euler(baseEuler);
        }

        private void ApplyScale(float scaleFactor)
        {
            target.localScale = baseScale * scaleFactor;
        }
    }
}
