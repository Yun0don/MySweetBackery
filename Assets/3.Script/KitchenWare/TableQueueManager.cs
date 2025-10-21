using System.Collections.Generic;
using UnityEngine;

public class TableQueueManager : MonoBehaviour
{
    public static TableQueueManager Instance { get; private set; }
    void Awake() => Instance = this;

    [Header("Table Wait Slots (2개)")]
    [SerializeField] Transform[] tableSlots;

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
        if (index < 0 || index >= tableSlots.Length || !tableSlots[index]) return default;
        return tableSlots[index].position;
    }

    public bool IsFront(GameObject owner) => GetIndexOf(owner) == 0;

    public int Capacity
    {
        get
        {
            int c = 0;
            foreach (var t in tableSlots)
                if (t) c++;
            return c;
        }
    }

}