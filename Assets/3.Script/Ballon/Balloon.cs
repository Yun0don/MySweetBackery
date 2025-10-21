using UnityEngine;
using TMPro;

public class Balloon : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] GameObject stateRoot;  
    [SerializeField] GameObject main;       
    [SerializeField] GameObject croissant;  
    [SerializeField] GameObject pay;        
    [SerializeField] GameObject table;      
    [SerializeField] GameObject exitIcon;   
    [SerializeField] TextMeshPro countText; 

    void Awake() => HideAll();

    public void HideAll()
    {
        if (stateRoot) stateRoot.SetActive(false);
    }

    void EnableOnly(params GameObject[] on)
    {
        if (!stateRoot) return;
        stateRoot.SetActive(true);

        if (main) main.SetActive(false);
        if (croissant) croissant.SetActive(false);
        if (pay) pay.SetActive(false);
        if (exitIcon) exitIcon.SetActive(false);
        if (table) table.SetActive(false);
        if (countText) countText.enabled = false;

        foreach (var go in on) if (go) go.SetActive(true);
    }
    public void ShowBread(int wantCount)
    {
        EnableOnly(main, croissant);
        if (countText)
        {
            countText.enabled = true;
            countText.text = wantCount.ToString();
        }
    }

    public void ShowPos()
    {
        EnableOnly(main, pay);
    }

    public void ShowTable()
    {
        EnableOnly(main,table);
    }

    public void ShowExitOnce(float autoHideDelay = 1.5f)
    {
        EnableOnly(exitIcon);
        CancelInvoke(nameof(HideAll));
        Invoke(nameof(HideAll), autoHideDelay);
    }
}