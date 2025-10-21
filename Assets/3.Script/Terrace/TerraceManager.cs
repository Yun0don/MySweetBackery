using System;
using UnityEngine;

public class TerraceManager : MonoBehaviour
{
    public static TerraceManager Instance { get; private set; }
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject); // 필요 없으면 제거
    }

    [SerializeField] bool unlocked = false;
    public bool IsUnlocked => unlocked;

    public event Action OnUnlocked;

    public void Unlock()
    {
        if (unlocked) return;
        unlocked = true;
        OnUnlocked?.Invoke();
        Debug.Log("[TerraceManager] Terrace Unlocked!");
    }

    public void ResetLock()
    {
        unlocked = false;
        OnUnlocked = null;
    }
}