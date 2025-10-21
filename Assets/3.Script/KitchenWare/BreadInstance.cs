using UnityEngine;

public class BreadInstance : MonoBehaviour
{
    public BakingBread sourceOven;

    // 중복 집기 방지용 플래그
    [HideInInspector] public bool isClaimed;

    public void Init(BakingBread oven)
    {
        sourceOven = oven;
        isClaimed = false; // 스폰 시 초기화
    }
}