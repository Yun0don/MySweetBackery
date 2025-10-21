using UnityEngine;
using UnityEngine.InputSystem;   // 새 입력 시스템

public class OvenTester : MonoBehaviour
{
    [SerializeField] BakingBread oven;

    void Update()
    {
        if (Keyboard.current.uKey.wasPressedThisFrame)   // U 키 눌림 체크
        {
            if (oven != null)
            {
                if (oven.CurrentLevel < oven.MaxLevel)
                {
                    bool upgraded = oven.TryUpgrade();
                    Debug.Log(upgraded 
                        ? $"업그레이드 성공! 현재 레벨: {oven.CurrentLevel}, 최대 스택: {oven.MaxStack}, 굽는 시간: {oven.BakeDur}" 
                        : $"업그레이드 실패. 돈이 부족하거나 조건 불충분. 현재 레벨: {oven.CurrentLevel}");
                }
                else
                {
                    Debug.Log("이미 최대 레벨입니다!");
                }
            }
        }
    }
}