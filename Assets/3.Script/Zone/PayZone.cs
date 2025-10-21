using System.Collections;
using UnityEngine;
using DG.Tweening;
using TMPro;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class PayZone : MonoBehaviour
{
    [Header("Detect")]
    public string playerTag = "Player";
    public Transform playerPocket;      // 지불 출발(플레이어)
    public Transform payAnchor;         // 도착(결제 포인트)

    [Header("Cost")]
    public int unlockCost = 30;         // 총 필요 금액
    [SerializeField] float payDelay = 0.01f; // $1 간격 (0~0.01 매우 빠름)

    [Header("FX (optional)")]
    public GameObject moneyPrefab;      // 시각용 $ 프리팹(없어도 동작)
    public float arcHeight = 0.35f;     // 베지어 피크 높이
    public float flyDuration = 0.08f;   // 한 장 비행 시간
    public Ease  flyEase = Ease.OutQuad;

    [Header("Progress UI")]
    public TextMeshProUGUI costText;    // “30” 표시 TMP
    public GameObject progressRoot;     // (선택) TableCanvas 같은 진행 UI 루트

    [Header("Toggle on Unlock")]
    public GameObject tableFloor;       // 끌 대상
    public GameObject tableWall;        // 끌 대상
    public GameObject terace;           // 켤 대상

    int paid; bool inside; bool paying; bool unlocked;

    void Reset()
    {
        var col = GetComponent<Collider>(); if (col) col.isTrigger = true;
        var rb  = GetComponent<Rigidbody>(); if (rb) { rb.isKinematic = true; rb.useGravity = false; }
    }

    void OnEnable() => RefreshLabel();

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag) || unlocked) return;
        inside = true;
        if (!playerPocket) playerPocket = other.transform;
        if (!paying) StartCoroutine(PayRoutine());
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        inside = false;
    }

    IEnumerator PayRoutine()
    {
        SoundManager.Instance.PlayCostMoney();
        paying = true;

        while (inside && !unlocked)
        {
            var ui = UIManager.Instance;
            if (!ui) yield break;

            if (Remaining <= 0) { Unlock(); break; }
            if (ui.Current <= 0) { yield return null; continue; }

            // === $1 지불 ===
            ui.AddMoneyInstant(-1);
            paid++;
            RefreshLabel();

            // === 베지어 연출 (PlayerPocket -> payAnchor) ===
            if (moneyPrefab && playerPocket && (payAnchor || transform))
            {
                var bill = Instantiate(moneyPrefab, playerPocket.position, Quaternion.identity);
                var t = bill.transform;

                Vector3 start = t.position;
                Vector3 end   = payAnchor ? payAnchor.position : transform.position;
                Vector3 mid   = (start + end) * 0.5f; mid.y += arcHeight;

                DOTween.Kill(t);
                DOTween.To(() => 0f, u =>
                {
                    Vector3 a = Vector3.Lerp(start, mid, u);
                    Vector3 b = Vector3.Lerp(mid, end, u);
                    t.position = Vector3.Lerp(a, b, u);
                }, 1f, flyDuration)
                .SetEase(flyEase)
                .OnComplete(() => Destroy(bill));
            }

            if (paid >= unlockCost) { Unlock(); break; }

            if (payDelay > 0f) yield return new WaitForSeconds(payDelay);
            else yield return null;
        }

        paying = false;
    }

    void RefreshLabel()
    {
        if (costText) costText.text = Remaining.ToString();
    }

    void Unlock()
    {
        unlocked = true;

        // 진행 UI 정리
        if (progressRoot) progressRoot.SetActive(false);
        else RefreshLabel(); // 루트가 없으면 텍스트만 0 유지

        // 토글
        if (tableFloor) tableFloor.SetActive(false);
        if (tableWall)  tableWall.SetActive(false);
        if (terace)     terace.SetActive(true);

        var col = GetComponent<Collider>(); if (col) col.enabled = false;
        
        ArrowPointer.Instance.DisableArrow();
        ArrowNavi.Instance.DisableArrow();
        SoundManager.Instance.PlaySuccess();
        CameraDirector.Instance.CutToPOI2_OneTime(1.5f);
        
        if (TerraceManager.Instance) TerraceManager.Instance.Unlock();
    }

    public int Paid      => paid;
    public int Remaining => Mathf.Max(0, unlockCost - paid);
}
