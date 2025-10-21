using System.Collections.Generic;
using UnityEngine;

public class PosQueueManager : MonoBehaviour
{
    public static PosQueueManager Instance { get; private set; }
    void Awake() => Instance = this;

    [Header("POS Wait Slots (4개)")]
    [SerializeField] Transform[] slotPoints;   // 0 = 카운터 앞

    readonly List<GameObject> queue = new List<GameObject>();

    public bool TryJoin(GameObject owner, out int index, out Vector3 pos)
    {
        int existing = queue.IndexOf(owner);
        if (existing >= 0)
        {
            index = existing;
            pos = GetSlotPosition(index);
            return true;
        }

        if (queue.Count >= Capacity)
        {
            index = -1; pos = default;
            return false;
        }

        queue.Add(owner);
        index = queue.Count - 1;
        pos = GetSlotPosition(index);
        return true;
    }

    public void Leave(GameObject owner)
    {
        int idx = queue.IndexOf(owner);
        if (idx < 0) return;
        queue.RemoveAt(idx);
    }

    public int GetIndexOf(GameObject owner) => queue.IndexOf(owner);

    public Vector3 GetSlotPosition(int index)
    {
        if (index < 0 || index >= slotPoints.Length || !slotPoints[index]) return default;
        return slotPoints[index].position;
    }

    public bool IsFront(GameObject owner) => GetIndexOf(owner) == 0;

    public int Capacity
    {
        get
        {
            int c = 0;
            foreach (var t in slotPoints)
                if (t) c++;
            return c;
        }
    }

}