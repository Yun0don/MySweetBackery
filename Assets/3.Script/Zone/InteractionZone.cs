using UnityEngine;
using UnityEngine.Events;
using System.Linq;

public class InteractionZone : MonoBehaviour
{
    [SerializeField] string[] targetTags = { "Player", "Customer" };
    [SerializeField] UnityEvent<Collider> onEnter;
    [SerializeField] UnityEvent<Collider> onExit;
    

    void OnTriggerEnter(Collider other)
    {
        if (targetTags.Contains(other.tag))
        {
            Debug.Log($"[Zone] {other.tag} 들어옴");
            onEnter?.Invoke(other);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (targetTags.Contains(other.tag))
        {
            Debug.Log($"[Zone] {other.tag} 나감");
            onExit?.Invoke(other);
        }
    }
}