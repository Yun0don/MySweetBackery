using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class JoystickSurface : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] DynamicOnScreenStick stick;
    int activePointerId = -1;
    float downTime;
    const float UpDebounce = 0.05f;

    public void OnPointerDown(PointerEventData e)
    {
        activePointerId = e.pointerId;
        downTime = Time.unscaledTime;
        stick.BeginAt(e);
    }

    public void OnDrag(PointerEventData e)
    {
        if (e.pointerId == activePointerId) stick.OnDrag(e);
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (e.pointerId != activePointerId) return;
        if (Time.unscaledTime - downTime < UpDebounce) return; 
      
        stick.OnPointerUp(e);
        activePointerId = -1;
    }
}
