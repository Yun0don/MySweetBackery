using UnityEngine;
using UnityEngine.AI;

public enum CustomerRoute
{
    POS,
    TABLE
}

public class Customer : MonoBehaviour
{
    public NavMeshAgent agent;
    public Animator animator;

    public Transform exitPoint;         // 퇴장 지점
    
    [Header("Components")]
    public CustomerBreadStack breadStack;   
    public StateMachine fsm;
    public CustomerPay pay;

    [Header("Shelf Slots")]
    public BreadShelfSlotManager shelfSlots;   // 슬롯 매니저 참조 (씬에 1개)

    // 슬롯 배정 상태
    public int assignedSlotIndex = -1;
    public Vector3 assignedSlotPosition;

    static readonly int HashSpeed    = Animator.StringToHash("Speed");
    static readonly int HashCarrying = Animator.StringToHash("HasStack");

    [SerializeField] private Balloon balloon;  
    public Balloon Balloon => balloon;    
    
    public CustomerRoute routeType = CustomerRoute.POS;  // 기본 POS, Inspector에서 Table 지정 가능
    
    public TableStateConfig tableConfig;
    void Awake()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (!breadStack) breadStack = GetComponent<CustomerBreadStack>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!pay) pay = GetComponent<CustomerPay>();   // 추가
        fsm = new StateMachine();
    }

    void Start()
    {
        SetCarrying(false);
        breadStack.TargetCount = Random.Range(1, 4);   // 목표 빵 수 (1~3)
        fsm.ChangeState(new BreadState(this));
    }

    void Update()
    {
        fsm.Tick();

        float normalized = agent.speed > 0f
            ? agent.velocity.magnitude / agent.speed
            : 0f;
        animator.SetFloat(HashSpeed, normalized, 0.1f, Time.deltaTime);
    }

    void OnDisable()
    {
        ReleaseShelfSlot();
    }

    public void GoTo(Transform t)
    {
        if (t) agent.SetDestination(t.position);
    }

    public void GoTo(Vector3 worldPos)
    {
        agent.SetDestination(worldPos);
    }

    public bool Arrived()
    {
        if (agent.pathPending) return false;
        return agent.remainingDistance <= agent.stoppingDistance
               && (!agent.hasPath || agent.velocity.sqrMagnitude < 0.01f);
    }

    public void SetCarrying(bool on)
        => animator.SetBool(HashCarrying, on);

    public bool TryAssignShelfSlot()
    {
        if (!shelfSlots) return false;

        if (shelfSlots.TryAcquire(gameObject, out var pos, out var index))
        {
            assignedSlotIndex = index;
            assignedSlotPosition = pos;
            return true;
        }
        return false;
    }

    public void ReleaseShelfSlot()
    {
        if (!shelfSlots) return;

        shelfSlots.Release(gameObject);
        assignedSlotIndex = -1;
        assignedSlotPosition = default;
    }
    public void ResetForNextRound()
    {
        SetCarrying(false);
        Balloon?.HideAll();

        if (pay) pay.LeaveQueue();

        if (pay) pay.ClearBag();

        if (breadStack)
        {
            breadStack.ClearStack();
            breadStack.TargetCount = Random.Range(1, 4);
        }
        ReleaseShelfSlot();

        if (agent)
        {
            agent.isStopped = false;
            agent.updateRotation = true;
            agent.ResetPath();
        }
    }
}
