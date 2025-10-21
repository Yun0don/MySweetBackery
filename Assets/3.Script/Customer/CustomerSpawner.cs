using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public Customer customerPrefab;
    public Transform[] spawnPoints;

    [Header("Pattern Settings")]
    public int posCount = 3;    // 패턴에서 POS 몇 명
    public int tableCount = 1;  // 패턴에서 TABLE 몇 명

    Customer[] activeCustomers;

    void Start()
    {
        activeCustomers = new Customer[spawnPoints.Length];
        SpawnAll();
    }

    void SpawnAll()
    {
        int patternTotal = Mathf.Max(1, posCount + tableCount); // 0으로 나눠지는 일 방지

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (activeCustomers[i] != null) continue;

            Transform spawn = spawnPoints[i];
            Customer c = Instantiate(customerPrefab, spawn.position, spawn.rotation);

            // 패턴 분배: 예) 3명 POS → 1명 TABLE 반복
            int patternIndex = i % patternTotal;
            if (patternIndex < posCount)
                c.routeType = CustomerRoute.POS;
            else
                c.routeType = CustomerRoute.TABLE;

            // 충돌 방지 우선순위 랜덤
            if (c.agent != null)
                c.agent.avoidancePriority = Random.Range(0, 100);

            activeCustomers[i] = c;
        }
    }
}