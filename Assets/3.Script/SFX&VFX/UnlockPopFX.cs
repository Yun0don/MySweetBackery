using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

[DisallowMultipleComponent]
public class UnlockPopFX : MonoBehaviour
{
    [Header("Targets")]
    public Transform root;                 // 기본값: this.transform
    public bool useDirectChildren = true;  // true: 바로 아래 자식만, false: 모든 하위 포함
    public List<Transform> extraTargets;   // 추가 타겟(옵션)

    [Header("Exclude")]
    public List<Transform> excludeTargets; 
    public bool excludeDescendants = true; 
    public bool respectIgnoreComponent = true; // ✅ UnlockPopIgnore 있으면 제외

    [Header("Animation")]
    public bool playOnEnable = true;
    public float fromScale = 0f;
    public float toScaleMultiplier = 1f;
    public float duration = 0.25f;
    public float stagger = 0.05f;
    public Ease  ease = Ease.OutBack;

    readonly List<Transform> targets = new();
    readonly List<Vector3>   finalScales = new();

    void Reset() { root = transform; }
    void OnEnable() { if (playOnEnable) Play(); }

    public void Play()
    {
        CollectTargets();

        // 시작 스케일 세팅
        for (int i = 0; i < targets.Count; i++)
        {
            var t = targets[i];
            if (!t) continue;
            t.DOKill();
            t.localScale = Vector3.one * fromScale;
        }

        // 순차 팝
        var seq = DOTween.Sequence();
        for (int i = 0; i < targets.Count; i++)
        {
            var t = targets[i];
            if (!t) continue;
            Vector3 endScale = finalScales[i] * toScaleMultiplier;
            seq.Insert(i * stagger, t.DOScale(endScale, duration).SetEase(ease));
        }
        seq.Play();
    }

    void CollectTargets()
    {
        targets.Clear();
        finalScales.Clear();

        Transform r = root ? root : transform;

        IEnumerable<Transform> Enumerate()
        {
            if (useDirectChildren)
            {
                for (int i = 0; i < r.childCount; i++)
                    yield return r.GetChild(i);
            }
            else
            {
                foreach (var t in r.GetComponentsInChildren<Transform>(false))
                    if (t != r) yield return t;
            }

            if (extraTargets != null)
                foreach (var t in extraTargets) if (t) yield return t;
        }

        foreach (var t in Enumerate())
        {
            if (!t.gameObject.activeSelf) continue;
            if (IsExcluded(t)) continue;                 // ✅ 제외 로직

            if (!targets.Contains(t))                   // 중복 방지
            {
                targets.Add(t);
                finalScales.Add(t.localScale);          // 원래 스케일 캡처
            }
        }
    }

    bool IsExcluded(Transform t)
    {
        // 1) 마커 컴포넌트로 제외
        if (respectIgnoreComponent && t.GetComponent<UnlockPopIgnore>()) return true;

        // 2) 인스펙터 제외 목록
        if (excludeTargets != null)
        {
            foreach (var ex in excludeTargets)
            {
                if (!ex) continue;
                if (t == ex) return true;
                if (excludeDescendants && t.IsChildOf(ex)) return true;
            }
        }
        return false;
    }
}
public class UnlockPopIgnore : MonoBehaviour {}
