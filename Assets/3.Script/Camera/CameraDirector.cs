using System.Collections;
using UnityEngine;
using Cinemachine;

public class CameraDirector : MonoBehaviour
{
    public static CameraDirector Instance { get; private set; }

    [Header("VCams")]
    public CinemachineVirtualCamera vcamPlayer;
    public CinemachineVirtualCamera vcamPOI1;
    public CinemachineVirtualCamera vcamPOI2;

    [Header("Blend Settings")]
    public float defaultCutSeconds = 2f;

    const int PLAYER_BASE = 10;
    const int POI_BASE = 9;
    const int CUT_PRIO = 20;

    bool inCut;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (vcamPlayer) vcamPlayer.Priority = PLAYER_BASE;
        if (vcamPOI1)   vcamPOI1.Priority   = POI_BASE;
        if (vcamPOI2)   vcamPOI2.Priority   = POI_BASE;
    }

    public System.Action<float> CutToPOI1_OneTime = (seconds) =>
    {
        if (Instance == null) return;
        Instance.StartCoroutine(Instance.CutRoutine(Instance.vcamPOI1, seconds));
        Instance.CutToPOI1_OneTime = (_) =>
        {
        };
    };

    public System.Action<float> CutToPOI2_OneTime = (seconds) =>
    {
        if (Instance == null) return;

        Instance.Invoke(nameof(Instance.InvokePOI2Cut), 1.5f);

        Instance.CutToPOI2_OneTime = (_) =>
        {
        };
    };

    void InvokePOI2Cut()
    {
        StartCoroutine(CutRoutine(vcamPOI2, defaultCutSeconds));
    }

    public void BackToPlayerNow()
    {
        if (vcamPlayer) vcamPlayer.Priority = CUT_PRIO;
        if (vcamPOI1)   vcamPOI1.Priority   = POI_BASE;
        if (vcamPOI2)   vcamPOI2.Priority   = POI_BASE;
    }

    IEnumerator CutRoutine(CinemachineVirtualCamera target, float seconds)
    {
        if (!target || inCut) yield break;
        inCut = true;

        float dur = (seconds < 0f) ? defaultCutSeconds : seconds;

        target.Priority = CUT_PRIO;
        yield return new WaitForSeconds(dur);

        BackToPlayerNow();

        inCut = false;
    }
}
