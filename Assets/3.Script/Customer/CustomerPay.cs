using UnityEngine;
using UnityEngine.AI;

public class CustomerPay : MonoBehaviour
{
    public enum QueueKind { None, POS, TABLE }
    
    public PosQueueManager posQueue;
    public TableQueueManager tableQueue;  
    public PosCounterZone posCounterZone;

    QueueKind currentQueue = QueueKind.None;
    
    [HideInInspector] public int queueIndex = -1;
    [HideInInspector] public Vector3 queueTarget;

    Customer customer;
    CustomerBreadStack stack;
    NavMeshAgent agent;

    [Header("Packing Visuals")]
    public GameObject bagPrefab;
    public Transform bagCounterAnchor;
    public Transform bagMouth;

    public string animOpenTrigger = "Open";
    public string animCloseTrigger = "Close";
    public string openStateName = "Open";
    public string closeStateName = "Close";

    public float packDuration = 0.25f;
    public float packArcHeight = 0.45f;
    public float eachPackDelay = 0.04f;
    public DG.Tweening.Ease packEase = DG.Tweening.Ease.OutQuad;
    
    public int pricePerBread = 5;


    [Header("Bag Carry (손 위치)")]
    public Transform bagCarryRoot;

    [Header("Money Stack")]
    public MoneyStack moneyStack;

    [Header("Facing POS")]
    public Transform lookAtTarget;
    public float startFaceDistance = 1.2f;
    public float turnSpeedDegPerSec = 540f;

    // ▶ 지금 들고 있는 봉투 참조(리셋 시 파괴)
    GameObject currentBag; 
    Animator  currentBagAnim;

    [HideInInspector] public bool HasPaid;

    void Awake()
    {
        customer = GetComponent<Customer>();
        stack    = GetComponent<CustomerBreadStack>();
        agent    = GetComponent<NavMeshAgent>();
        ResetPayment();
    }
    public void ResetPayment() => HasPaid = false;
    void MarkPaid() => HasPaid = true;

    public bool TryJoinPosQueue()
    {
        if (!posQueue) return false;
        if (posQueue.TryJoin(gameObject, out var idx, out var pos))
        {
            currentQueue = QueueKind.POS;
            queueIndex   = idx;
            queueTarget  = pos;
            SetDestination(queueTarget);
            return true;
        }
        return false;
    }
    public bool TryJoinTableQueue()
    {
        if (!tableQueue) return false;
        if (tableQueue.TryJoin(gameObject, out var idx, out var pos))
        {
            currentQueue = QueueKind.TABLE;
            queueIndex   = idx;
            queueTarget  = pos;
            SetDestination(queueTarget);
            return true;
        }
        return false;
    }
    public void LeaveQueue()
    {
        if (currentQueue == QueueKind.POS && posQueue)
            posQueue.Leave(gameObject);
        else if (currentQueue == QueueKind.TABLE && tableQueue)
            tableQueue.Leave(gameObject);

        currentQueue = QueueKind.None;
        queueIndex   = -1;
        queueTarget  = default;
    }

    public bool IsFront()
    {
        if (currentQueue == QueueKind.POS)   return posQueue && posQueue.IsFront(gameObject);
        if (currentQueue == QueueKind.TABLE) return tableQueue && tableQueue.IsFront(gameObject);
        return false;
    }

    public void RefreshQueueTarget()
    {
        if (currentQueue == QueueKind.POS && posQueue)
        {
            int idxNow = posQueue.GetIndexOf(gameObject);
            if (idxNow < 0) return;
            queueIndex  = idxNow;
            queueTarget = posQueue.GetSlotPosition(idxNow);
            SetDestination(queueTarget);
            return;
           
        }
        if (currentQueue == QueueKind.TABLE && tableQueue)
        {
            int idxNow = tableQueue.GetIndexOf(gameObject);
            if (idxNow < 0) return;
            queueIndex  = idxNow;
            queueTarget = tableQueue.GetSlotPosition(idxNow);
            SetDestination(queueTarget);
            
        }
    }

    public bool CanPayNow()
    {
        return posCounterZone && posCounterZone.CanPay(customer);
    }

    public int CalculatePayment()
    {
        int count = stack ? stack.CurrentCount : 0;
        return count * Mathf.Max(0, pricePerBread);
    }

    void SetDestination(Vector3 worldPos)
    {
        if (agent) agent.SetDestination(worldPos);
    }
    public GameObject SpawnBag()
    {
        SoundManager.Instance.PlayCash();

        Vector3 pos = bagCounterAnchor ? bagCounterAnchor.position : transform.position;
        Quaternion rot = bagCounterAnchor ? bagCounterAnchor.rotation : transform.rotation;

        currentBag = Instantiate(bagPrefab, pos, rot);
        currentBagAnim = currentBag ? currentBag.GetComponent<Animator>() : null;

        if (!bagMouth && currentBag) bagMouth = currentBag.transform; // 안전판
        return currentBag;
    }

    // ▶ 봉투 부착(현재 봉투 사용)
    public void AttachBagToCarryPoint(GameObject bag)
    {
        CameraDirector.Instance.CutToPOI1_OneTime(1.5f);

        if (!bag) return;
        if (!bagCarryRoot)
        {
            Debug.LogWarning($"{name}: bagCarryRoot가 비었습니다. 인스펙터에서 손/스택 포인트를 할당하세요.");
            return;
        }

        bag.transform.SetParent(bagCarryRoot, worldPositionStays: false);
        bag.transform.localPosition = Vector3.zero;
        bag.transform.localRotation = Quaternion.identity;
    }

    public void ApplyQueueFacing(Customer c)
    {
        if (!c || !c.agent || !lookAtTarget) return;

        Vector3 toSlot = queueTarget - c.transform.position;
        toSlot.y = 0f;
        float dist = toSlot.magnitude;

        bool shouldFace =
            dist <= Mathf.Max(startFaceDistance, c.agent.stoppingDistance + 0.05f);

        c.agent.updateRotation = !shouldFace;
        if (!shouldFace) return;

        Vector3 dir = lookAtTarget.position - c.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        c.transform.rotation = Quaternion.RotateTowards(
            c.transform.rotation, targetRot, turnSpeedDegPerSec * Time.deltaTime);
    }

    public void ClearBag()
    {
        if (currentBag)
        {
            Destroy(currentBag);
            currentBag = null;
        }
        currentBagAnim = null;
        // 봉투 입구 참조도 초기화(다음 SpawnBag에서 다시 셋업)
        bagMouth = null;
    }
    
}
