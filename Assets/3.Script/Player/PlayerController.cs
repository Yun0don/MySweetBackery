using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float rotateLerp = 12f;

    [Header("Camera (Fixed)")]
    [SerializeField] Transform cam;
   
    [Header("Anim")]
    [SerializeField] AnimationController animCtrl;
  
    Rigidbody rb;
    Vector2 move;
    
    bool dragHintHidden = false;

    public void OnMove(InputValue v) => move = v.Get<Vector2>();

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (!cam) cam = Camera.main ? Camera.main.transform : null;
        if (!animCtrl) animCtrl = GetComponentInChildren<AnimationController>();
    }

    void FixedUpdate()
    {
    
        Vector3 dir = new Vector3(move.x, 0f, move.y);
        if (!dragHintHidden && dir.sqrMagnitude > 0.01f)
        {
            if (UIManager.Instance) UIManager.Instance.HideDragText();
            dragHintHidden = true;
        }

        if (dir.sqrMagnitude > 0.0001f)
        {
            Vector3 next = rb.position + dir.normalized * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(next);

            var target = Quaternion.LookRotation(dir, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, target, rotateLerp * Time.fixedDeltaTime));
        }
    }

    void Update()
    {
        if (animCtrl) animCtrl.SetMove(move);
    }
}