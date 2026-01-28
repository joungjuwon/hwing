#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine;
using UnityEngine.EventSystems;

namespace StylizedSkybox
{
    public static class InputProxy
    {
        public static Vector3 MousePosition => Input.mousePosition;

        public static bool GetKeyDown(KeyCode key)
        {
            return Input.GetKeyDown(key);
        }

        public static bool GetKey(KeyCode key)
        {
            return Input.GetKey(key);
        }

        public static float GetAxis(string axisName)
        {
            return Input.GetAxis(axisName);
        }

        public static void SetupEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
#if ENABLE_LEGACY_INPUT_MANAGER
            eventSystem.AddComponent<StandaloneInputModule>();
#else
            eventSystem.AddComponent<InputSystemUIInputModule>();
#endif
#else
            eventSystem.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
