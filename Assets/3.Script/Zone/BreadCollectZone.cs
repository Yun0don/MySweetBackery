using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))] // OvenZone에 붙임(트리거)
public class BreadCollectZone : MonoBehaviour
{
    [Header("Who can use")]
    [SerializeField] string playerTag = "Player";

    [Header("Area (BoxCollider만 사용)")]
    [SerializeField] BoxCollider zoneArea;          // OvenZone의 BoxCollider
    [Header("Collect")]
    [SerializeField] PlayerBreadStack playerStack;  // 비워두면 들어온 플레이어에서 찾음
    [SerializeField] float scanInterval = 0.08f;    // 존 안에 있는 동안만 주기 수거
    [SerializeField] string breadTag = "Bread";
    [SerializeField] LayerMask breadLayers = ~0;

    PlayerBreadStack activeStack;
    Collider currentPlayer;
    Coroutine loop;

    void OnValidate()
    {
        if (!zoneArea) zoneArea = GetComponent<BoxCollider>();
        if (zoneArea) zoneArea.isTrigger = true;
    }

    public void OnZoneEnter(Collider who)
    {
        if (!who || !who.CompareTag(playerTag)) return;

        currentPlayer = who;
        activeStack = playerStack ?? who.GetComponentInParent<PlayerBreadStack>();
        if (loop == null) loop = StartCoroutine(CollectLoop());
    }

    public void OnZoneExit(Collider who)
    {
        if (who != currentPlayer) return;
        currentPlayer = null;
        activeStack = null;
        if (loop != null) { StopCoroutine(loop); loop = null; }
    }

    IEnumerator CollectLoop()
    {
        var hits = new Collider[32];
        while (activeStack != null)
        {
            if (!activeStack.IsFull)
            {
                // BoxCollider 월드값으로 OverlapBoxNonAlloc
                Vector3 center = zoneArea.transform.TransformPoint(zoneArea.center);
                Vector3 halfExtents = Vector3.Scale(zoneArea.size, zoneArea.transform.lossyScale) * 0.5f;
                Quaternion orient = zoneArea.transform.rotation;

                int count = Physics.OverlapBoxNonAlloc(
                    center, halfExtents, hits, orient, breadLayers, QueryTriggerInteraction.Collide);

                for (int i = 0; i < count && !activeStack.IsFull; i++)
                {
                    var c = hits[i];
                    if (!c || !c.CompareTag(breadTag)) continue;

                    var inst = c.GetComponent<BreadInstance>();
                    if (!inst) continue;

                    // 이미 누가 집는 중이면(콜라이더 꺼짐 등) 스킵
                    var breadCol = c.GetComponent<Collider>();
                    if (breadCol != null && !breadCol.enabled) continue;

                    activeStack.TryCollectBread(inst);
                }
            }
            yield return new WaitForSeconds(scanInterval);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!zoneArea) return;
        Gizmos.color = Color.yellow;
        Vector3 center = zoneArea.transform.TransformPoint(zoneArea.center);
        Vector3 halfExtents = Vector3.Scale(zoneArea.size, zoneArea.transform.lossyScale) * 0.5f;
        Gizmos.matrix = Matrix4x4.TRS(center, zoneArea.transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, halfExtents * 2f);
    }
#endif
}
