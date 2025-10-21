using UnityEngine;

public interface IState
{
    void Enter();
    void Tick();
    void Exit();
}

public class StateMachine
{
    IState current;

    public void ChangeState(IState next)
    {
        current?.Exit();
        current = next;
        current?.Enter();
    }

    public void Tick() => current?.Tick();
}

class BreadState : IState
{
    Customer c;
    CustomerBreadCollectZone collector;
    bool balloonShown;

    public BreadState(Customer c) { this.c = c; }

    public void Enter()
    {
        if (c.TryAssignShelfSlot()) c.GoTo(c.assignedSlotPosition);

        collector = c.GetComponent<CustomerBreadCollectZone>();
        if (collector) collector.BeginFor(c);

        balloonShown = false; // 도착 후에만 말풍선 표시
    }

    public void Tick()
    {
        if (!balloonShown && c.Arrived())
        {
            balloonShown = true;
            c.Balloon?.ShowBread(c.breadStack.TargetCount);
        }

        // 목표 개수 채우면 결제하러 이동
        if (c.breadStack.CurrentCount >= c.breadStack.TargetCount)
            c.fsm.ChangeState(new PosState(c));
    }

    public void Exit()
    {
        if (collector) collector.EndFor();
        c.ReleaseShelfSlot();
    }
}

class ExitState : IState
{
    Customer c;
    public bool loopCustomer = true; // 기본 true

    public ExitState(Customer c, bool loop = true)
    {
        this.c = c;
        loopCustomer = loop;
    }

    public void Enter()
    {
        c.Balloon?.ShowExitOnce(1.5f);
        Debug.Log("[ExitState] 이동 시작 → Exit");
        c.GoTo(c.exitPoint);
    }

    public void Tick()
    {
        if (!c.Arrived()) return;

        if (loopCustomer)
        {
            c.ResetForNextRound();
            c.fsm.ChangeState(new BreadState(c));
        }
    }

    public void Exit() { }
}
