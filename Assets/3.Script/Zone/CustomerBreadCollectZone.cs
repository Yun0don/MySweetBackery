using System.Collections;
using UnityEngine;

public class CustomerBreadCollectZone : MonoBehaviour
{
    [Header("Scan")]
    [SerializeField] Transform probeCenter;
    [SerializeField] float probeRadius = 1.2f;
    [SerializeField] float scanInterval = 0.1f;
    [SerializeField] int maxPickPerScan = 1;

    [Header("Filter")]
    [SerializeField] string breadTag = "Bread";
    [SerializeField] LayerMask breadLayers = ~0;

    [Header("Depot (optional)")]
    [SerializeField] BreadReturnZone depot;

    Customer customer;
    Coroutine loop;

    void OnValidate()
    {
        if (!probeCenter) probeCenter = transform; // 고객 위치 기준
        if (probeRadius <= 0f) probeRadius = 0.5f;
        if (maxPickPerScan < 1) maxPickPerScan = 1;
        if (breadLayers.value == 0) breadLayers = ~0;
    }

    public void BeginFor(Customer c)
    {
        customer = c;
        probeCenter = c.transform; // 고객 중심으로 스캔
        if (loop == null) loop = StartCoroutine(CollectLoop());
    }

    public void EndFor()
    {
        customer = null;
        if (loop != null) { StopCoroutine(loop); loop = null; }
    }

    IEnumerator CollectLoop()
    {
        var hits = new Collider[32];

        while (customer != null && customer.breadStack.CurrentCount < customer.breadStack.TargetCount)
        {
            Physics.SyncTransforms();

            int count = Physics.OverlapSphereNonAlloc(
                probeCenter.position, probeRadius, hits, breadLayers.value, QueryTriggerInteraction.Collide);

            int picked = 0;
            for (int i = 0;
                 i < count && picked < maxPickPerScan
                           && customer.breadStack.CurrentCount < customer.breadStack.TargetCount;
                 i++)
            {
                var inst = FindBreadInstance(hits[i]);
                if (!inst) continue;
                if (!string.IsNullOrEmpty(breadTag) && !inst.CompareTag(breadTag)) continue;
                if (inst.isClaimed) continue;

                inst.isClaimed = true;

                var owner = depot ? depot : inst.GetComponentInParent<BreadReturnZone>();
                if (owner != null && !owner.TryTakeOut(inst))
                {
                    inst.isClaimed = false;
                    continue;
                }

                picked++;
                yield return customer.StartCoroutine(customer.breadStack.TakeBread(inst));
            }

            yield return new WaitForSeconds(scanInterval);
        }
        loop = null;
    }

    static BreadInstance FindBreadInstance(Collider col)
    {
        if (!col) return null;
        if (col.attachedRigidbody)
        {
            var inst = col.attachedRigidbody.GetComponent<BreadInstance>();
            if (inst) return inst;
        }
        return col.GetComponentInParent<BreadInstance>();
    }
}










