using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class CustomerBreadStack : MonoBehaviour
{
    [Header("Stack Root")]
    [SerializeField] Transform stackRoot;       // 빵 붙일 위치
    [SerializeField] Vector3 baseLocalOffset = Vector3.zero;
    [SerializeField] float verticalSpacing = 0.3f;
    [SerializeField] float arcHeight = 0.45f;
    [SerializeField] float takeDuration = 0.25f;
    [SerializeField] Ease curveEase = Ease.OutQuad;

    public int CurrentCount { get; private set; } = 0;
    public int TargetCount { get; set; } = 0;   
    readonly List<Transform> stack = new List<Transform>();
    public Transform StackRoot => stackRoot; 
    Customer owner; 

    void Awake()
    {
        owner = GetComponent<Customer>();
    }
    public IEnumerator TakeBread(BreadInstance bread)
    {
        if (bread == null || stackRoot == null) yield break;

        var breadTrans = bread.transform;

        var rb = breadTrans.GetComponent<Rigidbody>();
        if (rb) { rb.isKinematic = true; rb.useGravity = false; rb.detectCollisions = false; }
        var col = breadTrans.GetComponent<Collider>();
        if (col) col.enabled = false;

        int idx = stack.Count;
        Vector3 targetLocal = baseLocalOffset + Vector3.up * (verticalSpacing * idx);
        Vector3 startWorld  = breadTrans.position;
        Vector3 endWorld    = stackRoot.TransformPoint(targetLocal);

        Vector3 mid = (startWorld + endWorld) * 0.5f; 
        mid.y += arcHeight;

        DOTween.Kill(breadTrans);
        yield return DOTween.To(() => 0f, t =>
        {
            Vector3 a = Vector3.Lerp(startWorld, mid, t);
            Vector3 b = Vector3.Lerp(mid, endWorld, t);
            breadTrans.position = Vector3.Lerp(a, b, t);
        }, 1f, takeDuration)
        .SetEase(curveEase)
        .WaitForCompletion();

        breadTrans.SetParent(stackRoot);
        breadTrans.localPosition = targetLocal;
        breadTrans.localRotation = Quaternion.identity;

        stack.Add(breadTrans);
        CurrentCount++;
        owner?.SetCarrying(stack.Count > 0);
    }

    public Transform PopOne()
    {
        if (stack.Count == 0) return null;
        int last = stack.Count - 1;
        var t = stack[last];
        stack.RemoveAt(last);

        CurrentCount = Mathf.Max(0, CurrentCount - 1);
        owner?.SetCarrying(stack.Count > 0);
        return t;
    }

    public void ClearStack()
    {
        foreach (var t in stack)
        {
            if (t) Destroy(t.gameObject);
        }
        stack.Clear();
        CurrentCount = 0;
        owner?.SetCarrying(false);
    }
}
