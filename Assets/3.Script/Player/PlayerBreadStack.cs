using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class PlayerBreadStack : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Transform stackRoot;
    [SerializeField] AnimationController anim;

    [Header("Stack Settings")]
    [SerializeField] int    maxCarry = 7;
    [SerializeField] float  verticalSpacing = 0.12f;
    [SerializeField] Vector3 baseLocalOffset = new Vector3(0.25f, 0f, 0f);
    [SerializeField] float  takeDuration = 0.25f;

    [Header("Curve (Bezier)")]
    [SerializeField] float  arcHeight = 0.6f;     // 중간 피크 높이
    [SerializeField] Ease   curveEase = Ease.OutQuad;

    readonly List<Transform> stack = new List<Transform>();
    public bool IsFull => stack.Count >= maxCarry;
    public int  Count  => stack.Count;

    public bool TryCollectBread(BreadInstance bread)
    {
        SoundManager.Instance.PlayGetObject();
        ArrowPointer.Instance.GotoIndex(1);
        if (IsFull || bread == null || stackRoot == null) return false;

        // 1) 물리/충돌 즉시 비활성 (플레이어가 밀리거나 돌아가는 현상 방지)
        var rb = bread.GetComponent<Rigidbody>();
        if (rb) { rb.isKinematic = true; rb.useGravity = false; rb.detectCollisions = false; }
        var col = bread.GetComponent<Collider>();
        if (col) col.enabled = false;

        // 2) 목표 위치(월드) 계산
        int idx = stack.Count;
        Vector3 targetLocal = baseLocalOffset + Vector3.up * (verticalSpacing * idx);
        Vector3 startWorld  = bread.transform.position;
        Vector3 endWorld    = stackRoot.TransformPoint(targetLocal);

        // 3) 베지어 중간 피크
        Vector3 mid = (startWorld + endWorld) * 0.5f;
        mid.y += arcHeight;

        // 4) 베지어로 이동 (0→1)
        DOTween.Kill(bread.transform); // 혹시 기존 트윈 있으면 정리
        DOTween.To(() => 0f, t =>
        {
            Vector3 a = Vector3.Lerp(startWorld, mid, t);
            Vector3 b = Vector3.Lerp(mid, endWorld, t);
            bread.transform.position = Vector3.Lerp(a, b, t);
        }, 1f, takeDuration)
        .SetEase(curveEase)
        .OnComplete(() =>
        {
            // 5) 스택 편입
            bread.transform.SetParent(stackRoot);
            bread.transform.localPosition = targetLocal;
            bread.transform.localRotation = Quaternion.identity;
        });

        stack.Add(bread.transform);

        // 오븐 스택 감소 & 애니 토글
        bread.sourceOven?.OnCroissantTaken();
        anim?.SetHasStack(true);
        return true;
    }
    public void RefreshAnimNow()
    {
        if (anim != null) anim.SetHasStack(stack.Count > 0);
    }
    public Transform PopOne()
    {
       
        if (stack.Count == 0) return null;
        int last = stack.Count - 1;
        var t = stack[last];
        stack.RemoveAt(last);
        if (stack.Count == 0) anim?.SetHasStack(false);
        return t;
    }
}
