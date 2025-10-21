using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleBeamTune : MonoBehaviour
{
    [Header("Beam Shape")]
    [Min(0)] public float radius = 0.05f;    
    [Min(0)] public float angle = 0f;        
    [Min(0)] public float startSize = 0.4f;  

    [Header("Beam Length (speed * lifetime)")]
    [Min(0)] public float speed = 5f;        
    [Min(0)] public float lifetime = 0.5f;   

    [Header("Space & Orientation")]
    public bool simulationLocal = true;      
    public Vector3 localRotationOffset;      

    [Header("Always On (불처럼 켜두기)")]
    public bool loop = true;
    [Min(0)] public float rateOverTime = 30f; // 초당 입자 수

    ParticleSystem ps;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        Apply();           
    }

    void OnEnable() { Apply(); }
    void OnValidate() { if (ps == null) ps = GetComponent<ParticleSystem>(); Apply(); }

    void Apply()
    {
        if (!ps) return;

        var main     = ps.main;
        var shape    = ps.shape;
        var emission = ps.emission;
        var vol      = ps.velocityOverLifetime;

        // 항상 켜두기
        main.loop = loop;
        emission.enabled = true;
        emission.rateOverTime = rateOverTime;

        // 길이/크기
        main.startLifetime = lifetime;
        main.startSpeed    = speed;      // 0으로 두면 제자리에서 퍼짐
        main.startSize     = startSize;

        // 시뮬레이션 공간
        main.simulationSpace = simulationLocal
            ? ParticleSystemSimulationSpace.Local
            : ParticleSystemSimulationSpace.World;

        // 콘을 "직선"으로
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle  = Mathf.Max(0f, angle); // 0 = 완전 직선
        shape.radius = radius;

        // 방향 락(필요시 로컬 보정)
        main.startRotation3D = true;
        main.startRotationX  = Mathf.Deg2Rad * localRotationOffset.x;
        main.startRotationY  = Mathf.Deg2Rad * localRotationOffset.y;
        main.startRotationZ  = Mathf.Deg2Rad * localRotationOffset.z;

        // 혹시 Shape 퍼짐/카메라 빌보드 영향 회피용: 로컬 Z로만 밀어주기
        vol.enabled = true;
        vol.space   = simulationLocal ? ParticleSystemSimulationSpace.Local
                                      : ParticleSystemSimulationSpace.World;
        vol.x = 0f;  vol.y = 0f;  vol.z = speed; // 항상 "앞(로컬 Z)"으로 밀기
    }
}
