using System;
using UnityEngine;
using TMPro;
using Unity.VisualScripting;

[DisallowMultipleComponent]
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] TextMeshProUGUI amountText;   // UICanvas 안의 Text (TMP)
    [SerializeField] int startingMoney = 0;
    [SerializeField] private TextMeshProUGUI tmpText;


    public int Current => current;
    int current;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (!amountText) amountText = GetComponentInChildren<TextMeshProUGUI>(true);

        SetMoneyInstant(startingMoney);
    }

    private void Start()
    {
        
        if (!tmpText) return;
        
        tmpText.outlineWidth = 0.1f;
        
        tmpText.outlineColor = Color.black;
    }

    public void AddMoneyInstant(int delta)
        => SetMoneyInstant(current + delta);

    public void SetMoneyInstant(int value)
    {
        current = Mathf.Max(0, value);
        if (amountText) amountText.text = current.ToString();
    }

    public void HideDragText()
    {
        if (tmpText) tmpText.gameObject.SetActive(false);
    }

}