using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.OnScreen;

[RequireComponent(typeof(RectTransform))]
public class DynamicOnScreenStick : OnScreenStick, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] Canvas canvas;             
    [SerializeField] RectTransform panelRoot;   
    [SerializeField] RectTransform ring;        

    RectTransform knobRT;

    void Awake()
    {
        knobRT = (RectTransform)transform;
    }

    public void BeginAt(PointerEventData e)
    {
        ShowJoystic();
        if (panelRoot && ring)
            ring.anchoredPosition = ScreenToAnchored(e.position);
        if (knobRT)
            knobRT.anchoredPosition = Vector2.zero;
        base.OnPointerDown(e);
    }
    public new void OnPointerUp(PointerEventData e)
    {
        base.OnPointerUp(e);
        HideJoystic();
    }

    void ShowJoystic()
    {
        if (ring && !ring.gameObject.activeSelf) ring.gameObject.SetActive(true);
        if (!gameObject.activeSelf) gameObject.SetActive(true); 
    }
    void HideJoystic()
    {
        if (ring && ring.gameObject.activeSelf) ring.gameObject.SetActive(false);
        if (gameObject.activeSelf) gameObject.SetActive(false); 
    }
    Vector2 ScreenToAnchored(Vector2 screenPos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            panelRoot,
            screenPos,
            canvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : (canvas ? canvas.worldCamera : null),
            out var local);
        return local;
    }
}
