using UnityEngine;
using System.Collections;
using DG.Tweening;

class PosState : IState
{
    Customer c;
    CustomerPay pay;

    bool playing;                  // 결제 코루틴 재진입 방지
    bool tableBalloonSwitched;     // 테이블 풍선 1회 전환
    bool quickPayStarted;          // TABLE 빠른결제 1회 보장

    public PosState(Customer c) { this.c = c; }

    public void Enter()
    {
        pay = c.pay ? c.pay : c.GetComponent<CustomerPay>();
        if (!pay)
        {
            Debug.LogError($"[PosState.Enter] {c.name}: CustomerPay 없음");
            return;
        }
        bool joined = (c.routeType == CustomerRoute.POS)
            ? pay.TryJoinPosQueue()
            : pay.TryJoinTableQueue();

        Debug.Log($"[PosState.Enter] {c.name} route={c.routeType} joined={joined}");

        if (!joined)
        {
            Debug.LogWarning($"[PosState.Enter] {c.name}: {(c.routeType == CustomerRoute.POS ? "POS" : "TABLE")} 대기열 가득 참");
            return;
        }

        c.Balloon?.ShowPos();
        tableBalloonSwitched = false;
        quickPayStarted = false;
        playing = false;
    }

    public void Tick()
    {
        if (pay == null) return;
        if (playing) return;

        pay.RefreshQueueTarget();
        pay.ApplyQueueFacing(c);

        bool atFront     = pay.IsFront();
        bool arrived     = c.Arrived();
        bool facingReady = !c.agent.updateRotation;

        if (c.routeType == CustomerRoute.TABLE)
        {
            // 포스 슬롯 정렬 전이면 대기
            if (!(atFront && arrived && facingReady))
                return;

            // (1) 포스 도착 → 말풍선 1회 전환
            if (!tableBalloonSwitched)
            {
                c.Balloon?.ShowTable();
                tableBalloonSwitched = true;
                Debug.Log($"[TABLE] {c.name} 풍선 전환 -> Table");
            }

            // (2) 테라스 UNLOCK되어야만 결제 가능
            bool unlocked = (TerraceManager.Instance && TerraceManager.Instance.IsUnlocked);
            if (!unlocked)
                return;

            // (3) UNLOCK 통과하면, 카운터 점유와 무관하게 빠른 결제 시작(1회만)
            if (!quickPayStarted)
            {
                quickPayStarted = true;
                Debug.Log($"[TABLE] {c.name} QuickPay 시작 (UNLOCK OK, CanPayNow 무시)");
                c.StartCoroutine(PayTableQuickRoutine());
            }
            return;
        }
        // POS 손님
        if (c.routeType == CustomerRoute.POS && atFront && arrived && facingReady)
        {
            if (pay.CanPayNow())
            {
                Debug.Log($"[POS] {c.name} PayAndPackRoutine 시작");
                c.StartCoroutine(PayAndPackRoutine(goTableAfter: false));
            }
            // else Debug.Log($"[POS] {c.name} CanPayNow=false");
        }
    }

    public void Exit()
    {
        if (c.agent) c.agent.updateRotation = true;
    }

    // -------------------- POS 전용(포장 애니 있는 결제) --------------------
    IEnumerator PayAndPackRoutine(bool goTableAfter)
    {
        playing = true;
        if (c.agent) c.agent.isStopped = true;

        int revenue = pay.CalculatePayment();
        Debug.Log($"[POS] {c.name} 결제 시작 (revenue={revenue})");

        GameObject bag = pay.SpawnBag();
        Animator bagAnim = bag ? bag.GetComponent<Animator>() : null;

        if (bagAnim && !string.IsNullOrEmpty(pay.openStateName))
            yield return PlayAndWait(bagAnim, pay.animOpenTrigger, pay.openStateName);

        Transform mouth = pay.bagMouth ? pay.bagMouth : (bag ? bag.transform : null);

        while (c.breadStack.CurrentCount > 0)
        {
            var t = c.breadStack.PopOne();
            if (!t) break;

            Vector3 startWorld = t.position;
            Vector3 endWorld = mouth ? mouth.position
                                     : (pay.bagCounterAnchor ? pay.bagCounterAnchor.position : startWorld);

            Vector3 mid = (startWorld + endWorld) * 0.5f;
            mid.y += pay.packArcHeight;

            DOTween.Kill(t);
            yield return DOTween.To(() => 0f, u =>
            {
                Vector3 a = Vector3.Lerp(startWorld, mid, u);
                Vector3 b = Vector3.Lerp(mid, endWorld, u);
                t.position = Vector3.Lerp(a, b, u);
            }, 1f, pay.packDuration)
            .SetEase(pay.packEase)
            .WaitForCompletion();

            Object.Destroy(t.gameObject);
            if (pay.eachPackDelay > 0f) yield return new WaitForSeconds(pay.eachPackDelay);
        }

        if (bagAnim && !string.IsNullOrEmpty(pay.closeStateName))
            yield return PlayAndWait(bagAnim, pay.animCloseTrigger, pay.closeStateName);

        pay.AttachBagToCarryPoint(bag);
        c.SetCarrying(true);

        Debug.Log($"[POS] {c.name} 결제 완료 revenue={revenue}");
        if (pay.moneyStack && revenue > 0)
            yield return pay.moneyStack.AddUnitsBatch(revenue);

        pay.LeaveQueue();

        if (c.agent) c.agent.isStopped = false;

        if (goTableAfter)
        {
            Debug.Log($"[POS] {c.name} -> TableState 전환");
            c.fsm.ChangeState(new TableState(c, c.tableConfig ?? Object.FindObjectOfType<TableStateConfig>()));
        }
        else
        {
            Debug.Log($"[POS] {c.name} -> ExitState 전환");
            c.fsm.ChangeState(new ExitState(c, true));
        }

        playing = false;
    }

    // -------------------- TABLE 전용(빠른 결제: 포장 없음) --------------------
    IEnumerator PayTableQuickRoutine()
    {
        if (playing) yield break; // 이중 실행 방지
        playing = true;

        if (c.agent) c.agent.isStopped = true;

        int revenue = pay.CalculatePayment();
        Debug.Log($"[TABLE] {c.name} QuickPay revenue={revenue}");

        SoundManager.Instance?.PlayCash();
        if (pay.moneyStack && revenue > 0)
            yield return pay.moneyStack.AddUnitsBatch(revenue);
        pay.HasPaid = true;
        
        pay.LeaveQueue();

        if (c.agent) c.agent.isStopped = false;

        var cfg = c.tableConfig ? c.tableConfig : Object.FindObjectOfType<TableStateConfig>();
        Debug.Log($"[TABLE] {c.name} QuickPay 완료 → TableState 전환 (cfg={(cfg ? cfg.name : "null")})");
        c.fsm.ChangeState(new TableState(c, cfg));
        playing = false;
    }

    // -------------------- 공용: 애니 대기 --------------------
    IEnumerator PlayAndWait(Animator anim, string trigger, string stateName, int layer = 0, float timeout = 5f)
    {
        if (!anim) yield break;

        if (!string.IsNullOrEmpty(trigger))
        {
            anim.ResetTrigger(trigger);
            anim.SetTrigger(trigger);
            yield return null; // 전이 반영
        }

        float t = 0f;
        while (!anim.GetCurrentAnimatorStateInfo(layer).IsName(stateName))
        {
            if ((t += Time.deltaTime) > timeout) yield break;
            yield return null;
        }

        t = 0f;
        while (true)
        {
            var st = anim.GetCurrentAnimatorStateInfo(layer);
            if (!anim.IsInTransition(layer) && st.IsName(stateName) && st.normalizedTime >= 1f) break;
            if ((t += Time.deltaTime) > timeout) break;
            yield return null;
        }
    }
}
