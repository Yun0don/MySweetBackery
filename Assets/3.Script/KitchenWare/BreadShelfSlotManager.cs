using System.Collections.Generic;
using UnityEngine;

public class BreadShelfSlotManager : MonoBehaviour
{
    [Header("Shelf Slots (size=4)")]
    [SerializeField] Transform[] slotPoints;   // 4개 등록

    readonly Dictionary<int, GameObject> ownerByIndex = new();
    readonly Dictionary<GameObject, int> indexByOwner = new();

    public bool TryAcquire(GameObject owner, out Vector3 slotPos, out int slotIndex)
    {
        // 이미 갖고 있으면 그걸 반환
        if (indexByOwner.TryGetValue(owner, out var idx) && slotPoints[idx])
        {
            slotIndex = idx;
            slotPos   = slotPoints[idx].position;
            return true;
        }

        // 빈 슬롯 탐색
        for (int i = 0; i < slotPoints.Length; i++)
        {
            if (!slotPoints[i]) continue;                   // 비어있는 배열 칸 방지
            if (ownerByIndex.ContainsKey(i)) continue;     // 이미 점유

            ownerByIndex[i]   = owner;
            indexByOwner[owner] = i;

            slotIndex = i;
            slotPos   = slotPoints[i].position;
            return true;
        }

        // 모두 찼음
        slotIndex = -1;
        slotPos   = default;
        return false;
    }

    public void Release(GameObject owner)
    {
        if (!indexByOwner.TryGetValue(owner, out var idx)) return;
        indexByOwner.Remove(owner);
        ownerByIndex.Remove(idx);
    }

    public bool HasFreeSlot()
    {
        int used = ownerByIndex.Count;
        int total = 0;
        foreach (var t in slotPoints) if (t) total++;
        return used < total;
    }

    void OnDrawGizmos()
    {
        if (slotPoints == null) return;
        Gizmos.color = Color.green;
        for (int i = 0; i < slotPoints.Length; i++)
        {
            if (!slotPoints[i]) continue;
            Gizmos.DrawWireSphere(slotPoints[i].position, 0.25f);
            UnityEditor.Handles.Label(slotPoints[i].position + Vector3.up * 0.25f, $"Shelf {i}");
        }
    }
}
