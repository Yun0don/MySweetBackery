using UnityEngine;

public class AnimationController : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] float damp = 0.1f;
    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int HasStackHash = Animator.StringToHash("HasStack");

    void Reset(){ if (!animator) animator = GetComponentInChildren<Animator>(); }

    public void SetMove(Vector2 move) =>
        animator.SetFloat(SpeedHash, Mathf.Clamp01(move.magnitude), damp, Time.deltaTime);

    public void SetHasStack(bool on)
    {
        animator.SetBool(HasStackHash, on);
    }
}
