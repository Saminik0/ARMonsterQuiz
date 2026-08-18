using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ARMonster.UI
{
    public class UIClickDebugger : MonoBehaviour
    {
        private void Update()
        {
            bool clicked = false;
            Vector2 pos = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                clicked = true;
                pos = Mouse.current.position.ReadValue();
            }
            else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                clicked = true;
                pos = Touchscreen.current.primaryTouch.position.ReadValue();
            }
#else
            if (Input.GetMouseButtonDown(0))
            {
                clicked = true;
                pos = Input.mousePosition;
            }
#endif

            if (clicked)
            {
                if (EventSystem.current == null)
                {
                    Debug.LogWarning("EventSystem.current is null!");
                    return;
                }

                PointerEventData pointerData = new PointerEventData(EventSystem.current)
                {
                    position = pos
                };

                List<RaycastResult> results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointerData, results);

                if (results.Count == 0)
                {
                    Debug.Log("<color=red>Клик улетел в пустоту (вне UI)!</color>");
                }
                else
                {
                    Debug.Log($"<color=yellow>КЛИК ПОПАЛ В: {results[0].gameObject.name}</color>");
                }
            }
        }
    }
}
