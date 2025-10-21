using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MoneyAbsorbZone : MonoBehaviour
{
    [Header("Refs")]
    public MoneyStack stack;            // 같은 오브젝트에 있으면 자동 할당 가능
    public Transform playerPocket;      // 돈이 흡입될 목표(플레이어의 앵커)
    [SerializeField] string playerTag = "Player";

    [Header("FX")]
    public float arcHeight = 0.55f;
    public float duration  = 0.25f;
    public float eachDelay = 0.01f;

    bool absorbing;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
        if (!stack) stack = GetComponent<MoneyStack>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (absorbing || !stack) return;

        if (!playerPocket) playerPocket = other.transform;

        StartCoroutine(AbsorbAllNow());
    }

    IEnumerator AbsorbAllNow()
    {
        ArrowPointer.Instance.GotoIndex(4);
        absorbing = true;

        while (stack.Count > 0)
        {
            int units = stack.Count; // 현재 있는 금액 전체를 한 번에
            yield return stack.AbsorbTo(
                target: playerPocket,
                units: units,
                arcHeight: arcHeight,
                duration: duration,
                eachDelay: eachDelay,
                onEachCollected: (d) => UIManager.Instance.AddMoneyInstant(d)
            );
        }

        absorbing = false;
    }
}