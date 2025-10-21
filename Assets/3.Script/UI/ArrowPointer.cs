using UnityEngine;
using DG.Tweening;

[DisallowMultipleComponent]
public class ArrowPointer : MonoBehaviour
{
    public static ArrowPointer Instance { get; private set; }
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    [Header("Anchors (0~4)")]
    [SerializeField] Transform[] anchors = new Transform[5];
    [SerializeField, Range(0, 4)] int currentIndex = 0;

    [Header("Attach & Offset")]
    public bool parentToAnchor = true;
    public Vector3 anchorLocalOffset = new Vector3(0f, 0.9f, 0f);
    public float moveToAnchorDuration = 0.15f;

    [Header("Bounce")]
    public float amplitude = 0.35f;
    public float bounceDuration = 0.4f;
    public Ease  bounceEase = Ease.InOutSine;

    [SerializeField] bool[] anchorLocked = new bool[5];
    [SerializeField] bool   sequentialGate = true;
    [SerializeField] bool[] visited        = new bool[5];
    int nextStep = 0;

    Tween bounceTween;

    void OnValidate()
    {
        int n = anchors?.Length ?? 0;
        if (anchorLocked == null || anchorLocked.Length != n) anchorLocked = new bool[n];
        if (visited     == null || visited.Length     != n) visited     = new bool[n];
    }

    void OnEnable()
    {
        if (!IsUsable(currentIndex)) currentIndex = FirstUsableIndex();

        if (currentIndex >= 0)
        {
            var t = anchors[currentIndex];
            ApplyAnchor(t, smooth:false);
            ArrowNavi.Instance?.SetTarget(t);
        }

        for (int i = 0; i < visited.Length; i++) visited[i] = false;
        if (currentIndex >= 0) visited[currentIndex] = true;
        nextStep = FindNextStep();

        StartBounce();
    }

    void OnDisable() => bounceTween?.Kill();

    public bool GotoIndex(int index, bool smooth = true)
    {
        if (!IsUsable(index)) return false;
        if (sequentialGate && index != nextStep) return false;

        int prev = currentIndex;
        currentIndex = index;

        var t = anchors[currentIndex];          // ★ target 변수 선언
        ApplyAnchor(t, smooth);
        StartBounce();

        if (prev >= 0) anchorLocked[prev] = true;
        anchorLocked[currentIndex] = true;

        visited[currentIndex] = true;
        nextStep = FindNextStep();

        // ★ 네비에게도 현재 타겟 전달
        ArrowNavi.Instance?.SetTarget(t);
        
        return true;
    }

    bool IsUsable(int idx)
    {
        return (idx >= 0 && idx < anchors.Length &&
                anchors[idx] != null &&
                !anchorLocked[idx]);
    }

    int FirstUsableIndex()
    {
        for (int i = 0; i < anchors.Length; i++)
            if (IsUsable(i)) return i;
        return -1;
    }

    int FindNextStep()
    {
        for (int i = 0; i < anchors.Length; i++)
            if (anchors[i] != null && !visited[i] && !anchorLocked[i]) return i;
        return -1;
    }

    void ApplyAnchor(Transform a, bool smooth)
    {
        if (!a) return;

        bounceTween?.Kill();

        if (parentToAnchor)
        {
            if (smooth && moveToAnchorDuration > 0f)
            {
                transform.SetParent(a, true); // 월드 유지
                transform.DOLocalMove(anchorLocalOffset, moveToAnchorDuration);
            }
            else
            {
                transform.SetParent(a, false);
                transform.localPosition = anchorLocalOffset;
            }
        }
        else
        {
            transform.SetParent(null, true);
            Vector3 dest = a.position + anchorLocalOffset;
            if (smooth && moveToAnchorDuration > 0f)
                transform.DOMove(dest, moveToAnchorDuration);
            else
                transform.position = dest;
        }
    }

    public void DisableArrow()
    {
        bounceTween?.Kill();
        transform.DOKill();
        gameObject.SetActive(false);
    }

    void StartBounce()
    {
        bounceTween?.Kill();
        float baseY = transform.localPosition.y;
        bounceTween = transform
            .DOLocalMoveY(baseY + amplitude, bounceDuration)
            .SetEase(bounceEase)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(gameObject);
    }
}
