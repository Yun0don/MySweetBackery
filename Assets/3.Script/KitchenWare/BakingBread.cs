using System.Collections;
using UnityEngine;

public class BakingBread : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Transform spawnPoint;
    [SerializeField] GameObject croissantPrefab;

    [Header("State")]
    [SerializeField, Min(1)] int currentLevel = 1;
    [SerializeField] int currentStack = 0;               // 바구니에 쌓인 빵 수
    Coroutine loop;

    public System.Func<int> GetMoney;
    public System.Action<int> SpendMoney;

    void OnEnable() { StartLoop(); }
    void OnDisable() { if (loop != null) StopCoroutine(loop); }

    public int CurrentLevel => currentLevel;
    public int MaxLevel => BakingConfig.MaxLevel;
    public int MaxStack   => BakingConfig.GetLevelData(currentLevel).maxStack;
    public float BakeDur  => BakingConfig.GetLevelData(currentLevel).bakeDuration;
    public int UpgradePrice(int to) => BakingConfig.GetLevelData(to).price;

    public bool TryUpgrade()
    {
        if (currentLevel >= MaxLevel) return false;
        int next = currentLevel + 1;
        int cost = UpgradePrice(next);
        if (GetMoney != null && GetMoney() < cost) return false;
        SpendMoney?.Invoke(cost);
        currentLevel = next;

        // 로그
        Debug.Log($"{{\"event\":\"upgrade\",\"oven_level\":{currentLevel},\"price\":{cost}}}");

        // 레벨업으로 MaxStack이 늘 수 있으니, 루프 재가동 보장
        EnsureLoopRunning();
        return true;
    }

    void StartLoop()
    {
        if (loop == null) loop = StartCoroutine(BakeLoop());
    }
    void EnsureLoopRunning()
    {
        if (loop == null) loop = StartCoroutine(BakeLoop());
    }

    IEnumerator BakeLoop()
    {
        while (true)
        {
            // 스택이 꽉 차면 대기 (스택이 줄어들 때까지)
            while (currentStack >= MaxStack) yield return null;

            // 굽기 시작
            float t = 0f, dur = BakeDur;
            Debug.Log($"{{\"event\":\"bake_start\",\"level\":{currentLevel},\"duration\":{dur}}}");
            while (t < dur) { t += Time.deltaTime; yield return null; }

            // 완성 → 스택 증가 & 스폰
            currentStack++;
            SpawnCroissant();
            Debug.Log($"{{\"event\":\"bake_complete\",\"level\":{currentLevel},\"stack\":{currentStack}}}");

            // 다음 루프에서 스택/레벨 조건 다시 확인
            yield return null;
        }
    }

    void SpawnCroissant()
    {
        if (!croissantPrefab) return;

        var go = Instantiate(
            croissantPrefab,
            spawnPoint ? spawnPoint.position : transform.position,
            spawnPoint ? spawnPoint.rotation : Quaternion.identity
        );

        var inst = go.GetComponent<BreadInstance>();
        if (inst == null) inst = go.AddComponent<BreadInstance>();
        inst.Init(this);
    }


    // 크루아상 픽업(판매/수거) 시 호출
    public void OnCroissantTaken()
    {
        currentStack = Mathf.Max(0, currentStack - 1);
    //     Debug.Log($"{{\"event\":\"take\",\"stack\":{currentStack}}}");
        EnsureLoopRunning(); // 꽉 차서 멈춰있었다면 즉시 재개
    }

    // UI 등에서 보여줄 데이터
    public int GetCurrentStack() => currentStack;
}
