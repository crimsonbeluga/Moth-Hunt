using MothHunt.Input;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Lever : MonoBehaviour
{
    public LeverActivatedObject target;
    bool _playerInside;

    void Awake()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            _playerInside = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            _playerInside = false;
    }

    void Update()
    {
        if (_playerInside && PlayerInputRouter.InteractPressedThisFrame)
        {
            if (target) target.Activate();
        }
    }
}
