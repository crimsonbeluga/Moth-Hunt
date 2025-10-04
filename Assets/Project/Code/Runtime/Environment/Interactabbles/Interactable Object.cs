using UnityEditor.EditorTools;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class InteractableObject : MonoBehaviour
{
    [Header("Configuration")]

    [Tooltip("Whether or not this object will have collision")]
    public bool startAsTrigger = false;
    private BoxCollider _col;
    private BoxCollider _interactionCollider;
    private void Awake()
    {
        _col = GetComponent<BoxCollider>();

        _col.isTrigger = startAsTrigger;

        _interactionCollider = GetComponentInChildren<BoxCollider>();
    }



}
