using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.UI;
#endif

namespace StylizedSkybox {

    static class InputProxy {

        public static void SetupEventSystem() {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var eventSystem = EventSystem.current;
            if (eventSystem == null) return;
            var standaloneModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (standaloneModule != null) {
                Object.Destroy(standaloneModule);
                if (eventSystem.GetComponent<InputSystemUIInputModule>() == null) {
                    eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                }
            }
#endif
        }

        public static bool GetKey(KeyCode keyCode) {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(keyCode);
#elif ENABLE_INPUT_SYSTEM
            return GetKeyInternal(keyCode, false);
#else
            return false;
#endif
        }

        public static bool GetKeyDown(KeyCode keyCode) {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(keyCode);
#elif ENABLE_INPUT_SYSTEM
            return GetKeyInternal(keyCode, true);
#else
            return false;
#endif
        }

        public static float GetAxis(string axisName) {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetAxis(axisName);
#elif ENABLE_INPUT_SYSTEM
            if (axisName == HorizontalAxisName) {
                return GetDigitalAxis(horizontalNegativeKeys, horizontalPositiveKeys);
            }

            if (axisName == VerticalAxisName) {
                return GetDigitalAxis(verticalNegativeKeys, verticalPositiveKeys);
            }

            if (axisName == MouseXAxisName) {
                return GetMouseAxis(true);
            }

            if (axisName == MouseYAxisName) {
                return GetMouseAxis(false);
            }

            return 0f;
#else
            return 0f;
#endif
        }

        public static Vector3 MousePosition {
            get {
#if ENABLE_LEGACY_INPUT_MANAGER
                return Input.mousePosition;
#elif ENABLE_INPUT_SYSTEM
                var mouse = Mouse.current;
                if (mouse == null) return Vector3.zero;
                var position = mouse.position.ReadValue();
                mousePositionCache.x = position.x;
                mousePositionCache.y = position.y;
                mousePositionCache.z = 0f;
                return mousePositionCache;
#else
                return Vector3.zero;
#endif
            }
        }

#if ENABLE_INPUT_SYSTEM
        const string HorizontalAxisName = "Horizontal";
        const string VerticalAxisName = "Vertical";
        const string MouseXAxisName = "Mouse X";
        const string MouseYAxisName = "Mouse Y";

        static readonly KeyCode[] horizontalNegativeKeys = { KeyCode.A, KeyCode.LeftArrow };
        static readonly KeyCode[] horizontalPositiveKeys = { KeyCode.D, KeyCode.RightArrow };
        static readonly KeyCode[] verticalNegativeKeys = { KeyCode.S, KeyCode.DownArrow };
        static readonly KeyCode[] verticalPositiveKeys = { KeyCode.W, KeyCode.UpArrow };

        static Vector3 mousePositionCache;
        static Vector2 mouseDelta;
        static int mouseDeltaFrame = -1;

        static float GetDigitalAxis(KeyCode[] negativeKeys, KeyCode[] positiveKeys) {
            var keyboard = Keyboard.current;
            if (keyboard == null) return 0f;

            float value = 0f;
            if (IsAnyKeyPressed(keyboard, negativeKeys)) value -= 1f;
            if (IsAnyKeyPressed(keyboard, positiveKeys)) value += 1f;
            return Mathf.Clamp(value, -1f, 1f);
        }

        static bool IsAnyKeyPressed(Keyboard keyboard, KeyCode[] keys) {
            for (int i = 0; i < keys.Length; i++) {
                var control = GetKeyControl(keyboard, keys[i]);
                if (control != null && control.isPressed) return true;
            }
            return false;
        }

        static float GetMouseAxis(bool horizontal) {
            var mouse = Mouse.current;
            if (mouse == null) return 0f;
            if (mouseDeltaFrame != Time.frameCount) {
                mouseDelta = mouse.delta.ReadValue();
                mouseDeltaFrame = Time.frameCount;
            }
            return horizontal ? mouseDelta.x : mouseDelta.y;
        }

        static bool GetKeyInternal(KeyCode keyCode, bool down) {
            var keyboard = Keyboard.current;
            if (keyboard == null) return false;
            var control = GetKeyControl(keyboard, keyCode);
            if (control == null) return false;
            return down ? control.wasPressedThisFrame : control.isPressed;
        }

        static KeyControl GetKeyControl(Keyboard keyboard, KeyCode keyCode) {
            switch (keyCode) {
                case KeyCode.LeftArrow: return keyboard.leftArrowKey;
                case KeyCode.RightArrow: return keyboard.rightArrowKey;
                case KeyCode.UpArrow: return keyboard.upArrowKey;
                case KeyCode.DownArrow: return keyboard.downArrowKey;
                case KeyCode.A: return keyboard.aKey;
                case KeyCode.D: return keyboard.dKey;
                case KeyCode.E: return keyboard.eKey;
                case KeyCode.Q: return keyboard.qKey;
                case KeyCode.S: return keyboard.sKey;
                case KeyCode.T: return keyboard.tKey;
                case KeyCode.U: return keyboard.uKey;
                case KeyCode.W: return keyboard.wKey;
                case KeyCode.LeftShift: return keyboard.leftShiftKey;
                case KeyCode.RightShift: return keyboard.rightShiftKey;
                case KeyCode.LeftControl: return keyboard.leftCtrlKey;
                case KeyCode.RightControl: return keyboard.rightCtrlKey;
                case KeyCode.Space: return keyboard.spaceKey;
                case KeyCode.Alpha1: return keyboard.digit1Key;
                case KeyCode.Alpha2: return keyboard.digit2Key;
                case KeyCode.Alpha3: return keyboard.digit3Key;
                default:
                    return null;
            }
        }
#else
        static Vector3 mousePositionCache;
#endif
    }
}

