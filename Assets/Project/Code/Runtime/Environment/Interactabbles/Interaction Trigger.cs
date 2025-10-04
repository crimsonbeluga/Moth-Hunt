using MothHunt.Input;
using Unity.VisualScripting;
using UnityEngine;

public class InteractionTrigger : MonoBehaviour
{
    private bool playerInside = false;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player")) // if the object that entered the trigger is the player
        {
            playerInside = true;        
        }
    }
    private void OnTriggerStay(Collider other)
    {
      if ( playerInside && PlayerInputRouter.InteractPressedThisFrame) //if are player is currently inside the triggger and we pressed interact this frame
      {

            

      }
    }
}
