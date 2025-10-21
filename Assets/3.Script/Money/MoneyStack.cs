using System.Collections;
using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class MoneyStack : MonoBehaviour
{
    [Header("Root / Prefab")]
    [SerializeField] Transform stackRoot;
    [SerializeField] GameObject moneyPrefab;

    [Header("Layer grid (3x3)")]
    [SerializeField] int cols = 3;     
    [SerializeField] int rows = 3;     

    [Header("Spacing")]
    [SerializeField] Vector3 baseLocalOffset = Vector3.zero; 
    [SerializeField] float cellSpacingX = 0.25f;
    [SerializeField] float cellSpacingZ = 0.25f;
    [SerializeField] float layerHeight = 0.06f;              

    [Header("Pop-in FX (optional)")]
    [SerializeField] float popTime = 0.12f;
    [SerializeField] Ease  popEase = Ease.OutBack;

    public int Count { get; private set; } = 0;

    void Reset()
    {
        if (!stackRoot) stackRoot = transform;
    }

    /// 결제 1회 금액(원 단위)만큼 '한 번에' 쌓기. (예: 15원이면 15개 즉시 생성)
    public IEnumerator AddUnitsBatch(int units)
    {
        SoundManager.Instance.PlayCostMoney();
        ArrowPointer.Instance.GotoIndex(3);
        if (units <= 0 || !stackRoot || !moneyPrefab) yield break;

        // 미리 계산된 위치에 전부 생성(동시에)
        for (int i = 0; i < units; i++)
        {
            int globalIndex = Count + i;

            // 레이어/셀 계산
            int perLayer = cols * rows;         // 9
            int layer    = globalIndex / perLayer;
            int inLayer  = globalIndex % perLayer;

            int r = inLayer / cols;             // 0..rows-1
            int c = inLayer % cols;             // 0..cols-1

            // 중앙 기준 배치
            float originX = -0.5f * (cols - 1) * cellSpacingX;
            float originZ = -0.5f * (rows - 1) * cellSpacingZ;

            Vector3 local = baseLocalOffset + new Vector3(
                originX + c * cellSpacingX,
                layer * layerHeight,
                originZ + r * cellSpacingZ
            );

            // 생성 & 부착
            var go = Instantiate(moneyPrefab, stackRoot.TransformPoint(local), Quaternion.identity, stackRoot);
            var t  = go.transform;
            t.localPosition = local;
            t.localRotation = Quaternion.identity;

            var finalScale = t.localScale;          // 프리팹/부모 스케일 반영된 최종값
            if (popTime > 0f)
            {
                t.localScale = Vector3.zero;
                t.DOScale(finalScale, popTime).SetEase(popEase);
            }
        }

        Count += units;
        yield return null; // 한 프레임만 양보(트윈 시작 보장)
    }

    // 필요 시 전체 리셋
    public void ClearAll()
    {
        for (int i = stackRoot.childCount - 1; i >= 0; i--)
            Destroy(stackRoot.GetChild(i).gameObject);
        Count = 0;
    }
    public IEnumerator AbsorbTo(Transform target, int units,
        float arcHeight = 0.55f,
        float duration  = 0.25f,
        float eachDelay = 0.01f,
        System.Action<int> onEachCollected = null)
    {
        if (!stackRoot || !target || units <= 0) yield break;

        for (int i = 0; i < units; i++)
        {
            if (Count <= 0 || stackRoot.childCount == 0) break;

            // 맨 위(마지막) 지폐
            int last = stackRoot.childCount - 1;
            var t = stackRoot.GetChild(last);

            Vector3 start = t.position;
            Vector3 end   = target.position;
            Vector3 mid   = (start + end) * 0.5f; mid.y += arcHeight;

            DOTween.Kill(t);
            yield return DOTween.To(() => 0f, u =>
            {
                Vector3 a = Vector3.Lerp(start, mid, u);
                Vector3 b = Vector3.Lerp(mid, end, u);
                t.position = Vector3.Lerp(a, b, u);
            }, 1f, duration).SetEase(Ease.OutQuad).WaitForCompletion();

            Destroy(t.gameObject);
            Count = Mathf.Max(0, Count - 1);

            onEachCollected?.Invoke(1);
            if (eachDelay > 0f) yield return new WaitForSeconds(eachDelay);
        }
    }
}
