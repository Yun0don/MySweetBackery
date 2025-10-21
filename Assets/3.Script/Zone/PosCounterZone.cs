using UnityEngine;

public class PosCounterZone : MonoBehaviour
{
    [SerializeField] string playerTag = "Player";
    [SerializeField] PosQueueManager queue;

    bool playerIn;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag)) playerIn = true;
    }
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag)) playerIn = false;
    }

    public bool CanPay(Customer c) => playerIn && queue && queue.IsFront(c.gameObject);
}