using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Collider))]
public class BreadReturnZone : MonoBehaviour
{
    [Header("Who can use ")]
    [SerializeField] string playerTag = "Player";

    [Header("Target (리턴 위치)")]
    [SerializeField] Transform dropRoot;                // 없으면 zone(자기 transform) 사용
    [SerializeField] Vector3 baseLocalOffset = Vector3.zero;

    [Header("Grid (2 x 4)")]
    [SerializeField, Min(1)] int capacity = 8;          // 최대 수용
    [SerializeField] float cellX = 0.22f;               // 좌우 간격
    [SerializeField] float cellZ = 0.18f;               // 앞뒤 간격(행 간격)
    [SerializeField] float gridY = 0.00f;               // 높이 오프셋
    [SerializeField] bool invertZ = false;              // 행 진행 방향 반전(원하면 체크)

    [Header("Return Motion")]
    [SerializeField] float takeDuration = 0.22f;        // 한 개 내려놓는 시간
    [SerializeField] float arcHeight   = 0.45f;         // 베지어 피크 높이
    [SerializeField] float depositInterval = 0.05f;     // 개당 텀
    [SerializeField] float depositYaw = 45f;             // 최종 로컬 Y 회전(도)

    // 내부 상태
    PlayerBreadStack activeStack;
    Coroutine loop;
    readonly List<Transform> depotStack = new List<Transform>();
    Collider currentPlayer;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    // InteractionZone.onEnter 에 연결 (Dynamic Collider)
    public void OnZoneEnter(Collider other)
    {
       
        if (other == null || !other.CompareTag(playerTag)) return;

        currentPlayer = other;
        activeStack = other.GetComponentInParent<PlayerBreadStack>();
        if (activeStack == null)
        {
            Debug.LogWarning("[BreadReturnZone] PlayerBreadStack을 찾지 못했습니다.");
            return;
        }

        if (dropRoot == null) dropRoot = transform; // 기본값

        // Any State 전환을 위한 현재 상태 동기화(들고 있으면 true)
        var animCtrl = other.GetComponentInParent<AnimationController>();
        animCtrl?.SetHasStack(activeStack.Count > 0);

        if (loop == null) loop = StartCoroutine(ReturnLoop());
    }

    // InteractionZone.onExit 에 연결 (Dynamic Collider)
    public void OnZoneExit(Collider other)
    {
        if (other != currentPlayer) return;
        currentPlayer = null;
        activeStack = null;
        if (loop != null) { StopCoroutine(loop); loop = null; }
    }
    
    public bool TryTakeOut(BreadInstance inst)
    {
        int idx = depotStack.FindIndex(t => t == inst.transform);
        if (idx < 0) return false;
        depotStack.RemoveAt(idx);
        return true;
    }

    IEnumerator ReturnLoop()
    {
        ArrowPointer.Instance.GotoIndex(2);
        int localCapacity = capacity;
        // 애니 보정용
        AnimationController animCtrl = null;
        if (currentPlayer) animCtrl = currentPlayer.GetComponentInParent<AnimationController>();

        while (activeStack != null)
        {
            // 만약 보관소가 가득 찼다면 "반납 불가" 상태: Pop 하지 말고 대기(또는 피드백)만 한다.
            if (depotStack.Count >= localCapacity)
            {
                yield return null;
                continue;
            }
            // 더 들고 있는 게 없으면 HasStack false 보정하고 종료/대기
            var carried = activeStack.PopOne();
            if (carried == null)
            {
                animCtrl?.SetHasStack(false); // Any State → DefaultSM 복귀 트리거
                yield return null;
                continue;
            }

            // 수용 초과 방지
            if (depotStack.Count >= capacity)
            {
                // 꽉 찼다면 마지막 슬롯에만 쌓거나(선택) 그냥 취소
                animCtrl?.SetHasStack(activeStack.Count > 0);
                yield return null;
                continue;
            }

            // ── 2×4 그리드: 12 / 34 / 56 / 78
            int slot = depotStack.Count;                 // 0..capacity-1
            int row  = slot / 2;                         // 0..3
            int col  = slot % 2;                         // 0(left), 1(right)

            float x = (col == 0 ? -0.5f : 0.5f) * cellX;
            float z = (invertZ ? -1f : 1f) * row * cellZ;

            Vector3 gridCenter = baseLocalOffset;
            Vector3 targetLocal = gridCenter + new Vector3(x, gridY, z);

            Vector3 startWorld = carried.position;
            Vector3 endWorld   = dropRoot.TransformPoint(targetLocal);

            // 물리/충돌 끄기
            var rb  = carried.GetComponent<Rigidbody>();
            if (rb) { rb.isKinematic = true; rb.useGravity = false; rb.detectCollisions = true; }
            var colr = carried.GetComponent<Collider>();
            if (colr)
            {
                colr.enabled = true; 
                colr.isTrigger = false;
            }

            // 베지어로 부드럽게 이동
            Vector3 mid = (startWorld + endWorld) * 0.5f; mid.y += arcHeight;
            DOTween.Kill(carried);
            yield return DOTween.To(() => 0f, t =>
            {
                Vector3 a = Vector3.Lerp(startWorld, mid, t);
                Vector3 b = Vector3.Lerp(mid, endWorld, t);
                carried.position = Vector3.Lerp(a, b, t);
            }, 1f, takeDuration)
            .SetEase(Ease.OutQuad)
            .WaitForCompletion();

            // 존에 고정 + 최종 로컬 Y 회전 적용
            carried.SetParent(dropRoot);
            carried.localPosition = targetLocal;
            carried.localRotation = Quaternion.Euler(0f, depositYaw, 0f);

            depotStack.Add(carried);
            SoundManager.Instance.PlayGetObject();

            // 남은 개수에 맞춰 Any State 전환 토글(들고 있으면 true, 아니면 false)
            animCtrl?.SetHasStack(activeStack.Count > 0);

            // 개당 텀
            if (depositInterval > 0f) yield return new WaitForSeconds(depositInterval);
        }
    }
}
