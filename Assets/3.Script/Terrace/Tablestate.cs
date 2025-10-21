using UnityEngine;
using UnityEngine.AI;
using System.Collections;

class TableState : IState
{
    Customer c;

    readonly Transform seatPoint;
    readonly Transform faceTarget;
    readonly float eatDuration;
    readonly GameObject trashPrefab;
    readonly Vector3 trashOffset;

    // ▶ 추가: 좌석의 NavMesh상 위치 캐시
    Vector3 seatNavPos;
    bool    seatNavPosValid;

    const float reachRadius     = 0.35f;
    const float repathInterval  = 0.5f;
    const float pathFailTimeout = 3.0f;

    bool  started;
    float lastRepathAt;
    float pathFailTimer;
    float oldStopping;

    public TableState(Customer c, TableStateConfig cfg = null)
    {
        this.c = c;
        if (!cfg) cfg = Object.FindObjectOfType<TableStateConfig>();

        seatPoint   = cfg ? cfg.seatPoint   : null;
        faceTarget  = cfg ? cfg.faceTarget  : null;
        eatDuration = cfg ? Mathf.Max(0f, cfg.eatDuration) : 3f;
        trashPrefab = cfg ? cfg.trashPrefab : null;
        trashOffset = cfg ? cfg.trashOffset : new Vector3(0.2f, 0f, 0.1f);
    }

    public void Enter()
    {
        c.pay?.ClearBag();
        c.SetCarrying(false);
        if (c.animator) c.animator.SetBool("HasStack", false);

        seatNavPosValid = false;
        if (seatPoint)
        {
            if (NavMesh.SamplePosition(seatPoint.position, out var hit, 0.6f, NavMesh.AllAreas))
            {
                seatNavPos = hit.position;  // 바닥에 스냅된 좌표
                seatNavPosValid = true;
            }
            else
            {
                Debug.LogWarning($"[TableState.Enter] seatPoint 주변에서 NavMesh를 찾지 못함: {seatPoint.position}");
            }
        }

        // NavMeshAgent 세팅
        if (c.agent)
        {
            oldStopping = c.agent.stoppingDistance;
            c.agent.stoppingDistance = Mathf.Min(oldStopping, reachRadius * 0.5f);
            c.agent.isStopped = false;
            c.agent.updateRotation = true;
            c.agent.ResetPath();
        }

        // 목적지 지정
        if (seatNavPosValid) c.GoTo(seatNavPos);
        else if (seatPoint)  c.GoTo(seatPoint.position); // 안전판

        c.Balloon?.ShowTable();
        Debug.Log($"[TableState.Enter] seat={(seatPoint? seatPoint.name : "null")}, navValid={seatNavPosValid}, target={(seatNavPosValid? seatNavPos : (seatPoint? seatPoint.position : c.transform.position))}");
    }

    public void Tick()
    {
        if (started) return;

        if (!seatPoint)
        {
            Debug.LogWarning("[TableState] seatPoint=null → 즉시 식사 시작");
            started = true;
            c.StartCoroutine(EatAndFinishRoutine());
            return;
        }

        // 경로 계산 중이면 대기
        if (c.agent && c.agent.pathPending) return;

        // 경로 상태 체크
        if (c.agent && (!c.agent.hasPath || c.agent.pathStatus != NavMeshPathStatus.PathComplete))
        {
            pathFailTimer += Time.deltaTime;

            if (Time.time - lastRepathAt > repathInterval)
            {
                lastRepathAt = Time.time;
                if (seatNavPosValid) c.GoTo(seatNavPos);
                else                  c.GoTo(seatPoint.position);
                // Debug.Log($"[TableState] repath: hasPath={c.agent.hasPath}, status={c.agent.pathStatus}");
            }

            // 타임아웃 → NavMesh 위치로 워프
            if (pathFailTimer > pathFailTimeout && seatNavPosValid && c.agent)
            {
                Debug.LogWarning("[TableState] path fail timeout → agent.Warp to seatNavPos");
                c.agent.Warp(seatNavPos);
            }
            return;
        }
        else
        {
            pathFailTimer = 0f;
        }

        // 충분히 도착했는지 (거리로도 보수 체크)
        var target = seatNavPosValid ? seatNavPos : seatPoint.position;
        float dist = Vector3.Distance(c.transform.position, target);
        if (!(c.Arrived() && dist <= reachRadius)) return;

        // 회전 + 착석
        SnapFacingToSeat();
        if (c.animator) c.animator.SetBool("HasSit", true);

        started = true;
        Debug.Log("[TableState] seat reached → start Eat");
        c.StartCoroutine(EatAndFinishRoutine());
    }

    public void Exit()
    {
        if (c.agent) c.agent.stoppingDistance = oldStopping;
    }

    IEnumerator EatAndFinishRoutine()
    {
        if (c.agent)
        {
            c.agent.isStopped = true;
            c.agent.updateRotation = false;
            c.agent.ResetPath();
        }

        if (eatDuration > 0f) yield return new WaitForSeconds(eatDuration);

        if (trashPrefab)
        {
            var basePos = seatNavPosValid ? seatNavPos : (seatPoint ? seatPoint.position : c.transform.position);
            Object.Instantiate(trashPrefab, basePos + trashOffset, Quaternion.identity);
            SoundManager.Instance?.PlayTrash();
        }

        if (c.animator) c.animator.SetBool("HasSit", false);

        if (TableQueueManager.Instance)
            TableQueueManager.Instance.Leave(c.gameObject);

        c.pay?.ClearBag();
        c.SetCarrying(false);
        if (c.animator) c.animator.SetBool("HasStack", false);

        if (c.agent)
        {
            c.agent.isStopped = false;
            c.agent.updateRotation = true;
        }

        Debug.Log("[TableState] finish → BreadState");
        c.ResetForNextRound();
        c.fsm.ChangeState(new BreadState(c));
    }

    void SnapFacingToSeat()
    {
        Vector3 fwd;
        if (faceTarget)
        {
            var dir = faceTarget.position - c.transform.position;
            dir.y = 0;
            fwd = (dir.sqrMagnitude < 1e-4f) ? c.transform.forward : dir.normalized;
        }
        else if (seatPoint)
        {
            fwd = seatPoint.forward; fwd.y = 0;
        }
        else fwd = c.transform.forward;

        if (fwd.sqrMagnitude > 1e-4f)
            c.transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
    }
}
