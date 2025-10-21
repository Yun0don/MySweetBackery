using UnityEngine;

public class TableStateConfig : MonoBehaviour
{
    [Header("Seating (옵션)")]
    public Transform seatPoint;     // 좌석 1개만 쓸 땐 여기에 넣기
    public Transform faceTarget;    // 앉았을 때 바라볼 대상

    [Header("Timing")]
    public float eatDuration = 3.0f;

    [Header("Trash (옵션)")]
    public GameObject trashPrefab;
    public Vector3 trashOffset = new Vector3(0.2f, 0f, 0.1f);
}