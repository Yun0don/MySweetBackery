using System;
using UnityEngine;

public class ArrowNavi : MonoBehaviour
{
    public static ArrowNavi Instance { get; private set; }
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (!origin) origin = transform.parent ? transform.parent : transform;

        var e = transform.rotation.eulerAngles;
        basePitch = e.x; baseRoll = e.z;
    }

    [Header("Origin (보통 플레이어)")]
    [SerializeField] Transform origin;

    [Header("Targets")]
    [SerializeField] Transform[] points = new Transform[5];
    [SerializeField, Range(0,4)] int currentIndex = 0;
    [SerializeField] Transform currentTarget;

    [Header("원 배치")]
    [SerializeField] float radius = 0.6f;                  // 플레이어 주위 원 반지름
    [SerializeField] float height = 0.05f;                 // Y 높이(원은 XZ 평면)
    [SerializeField] float moveLerp = 15f;                 // 0이면 즉시, 값↑일수록 부드럽게

    [Header("회전")]
    [SerializeField] float rotateSpeedDeg = 720f;
    [SerializeField] float yawOffsetDeg   = 0f;            // 모델 정면 보정(+X면 90)
    [SerializeField] bool  lockPitchRoll  = true;
    float basePitch, baseRoll;

    Vector3 lastDir = Vector3.forward;                     // 타겟이 너무 가까울 때 유지용

    public void SelectTarget(int idx)
    {
        if (idx >= 0 && idx < points.Length && points[idx] != null)
        {
            currentIndex  = idx;
            currentTarget = points[idx];
        }
    }
    public void SetTarget(Transform t) => currentTarget = t;

    void LateUpdate()
    {
        if (!origin) return;

        Vector3 from = origin.position;

        // 1) 수평 방향만 추출 (Y 무시)
        if (currentTarget)
        {
            Vector3 flat = Vector3.ProjectOnPlane(currentTarget.position - from, Vector3.up);
            if (flat.sqrMagnitude > 0.0001f) lastDir = flat.normalized;
        }

        // 2) 원 둘레 위 위치 = origin + height*Y + dir*radius
        Vector3 targetPos = from + new Vector3(0, height, 0) + lastDir * radius;

        if (moveLerp <= 0f) transform.position = targetPos;
        else
        {
            // 지수형 Lerp로 프레임 독립 부드러운 이동
            float t = 1f - Mathf.Exp(-moveLerp * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, targetPos, t);
        }

        // 3) 요(yaw)만 회전
        float yaw = Mathf.Atan2(lastDir.x, lastDir.z) * Mathf.Rad2Deg + yawOffsetDeg;
        Quaternion q = Quaternion.Euler(lockPitchRoll ? basePitch : 0f,
                                        yaw,
                                        lockPitchRoll ? baseRoll  : 0f);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, q, rotateSpeedDeg * Time.deltaTime);
    }

    public void DisableArrow()              
    {
        gameObject.SetActive(false);
    }
}
