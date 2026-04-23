using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Klyra.Loadout
{
    /// <summary>
    /// Fires <see cref="onRepeat"/> once on press, then again at an accelerating
    /// cadence while the pointer is held. Use instead of Button.onClick for
    /// stepper-style controls (+/-) so users can hold to ramp the value fast.
    /// </summary>
    public class HoldRepeatButton : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [Tooltip("Delay after the first press before repeats start (seconds).")]
        public float initialDelay = 0.35f;
        [Tooltip("Starting interval between repeats (seconds).")]
        public float startInterval = 0.10f;
        [Tooltip("Fastest interval the ramp can reach (seconds).")]
        public float minInterval = 0.02f;
        [Tooltip("Multiplier applied to the interval after each repeat.")]
        public float accelerate = 0.85f;

        public Action onRepeat;

        private bool holding;
        private float nextFireTime;
        private float currentInterval;

        public void OnPointerDown(PointerEventData eventData)
        {
            holding = true;
            currentInterval = startInterval;
            onRepeat?.Invoke();
            nextFireTime = Time.unscaledTime + initialDelay;
        }

        public void OnPointerUp(PointerEventData eventData) { holding = false; }
        public void OnPointerExit(PointerEventData eventData) { holding = false; }

        private void Update()
        {
            if (!holding) return;
            if (Time.unscaledTime < nextFireTime) return;
            onRepeat?.Invoke();
            currentInterval = Mathf.Max(minInterval, currentInterval * accelerate);
            nextFireTime = Time.unscaledTime + currentInterval;
        }
    }
}
