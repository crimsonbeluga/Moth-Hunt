using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseMaker : MonoBehaviour
{
    //reference
    public SphereCollider _noiseCollider;

    [Header("Range")]
    //hardcoded ranges for noise. Might be replaced with one range and scaled based on state
    public float _walkSuspicionRange;
    public float _sprintSuspicionRange;
    public float _jumpSuspicionRange;
    public float _crouchSuspicionRange;

    [Header("Terrain")]
    //set up a multiplier for each type of terrain

    public bool _isOnGrass;
    public bool _isOnMetal;
    public bool _isOnDirt;
    public bool _isOnGravel;

    [Header("Volume")]
    public float noiseVolume;

    [Header("Logic")]
    public bool _isNoiseMadeThisFrame;
    private Ray ray;


    public void onTick()
    {
        //if noise made this frame
        if(_isNoiseMadeThisFrame)
        {
            //set to false so cant keep at max size
            _isNoiseMadeThisFrame = false;

        }
        else
        {
            if(_noiseCollider.radius < 1)
            { 
                _noiseCollider.radius = 1; 
            }
            _noiseCollider.radius -= .2f;
        }

        //check if enemy is within range
    }



    public void MakeNoise(float vol)
    {
        _isNoiseMadeThisFrame = true;
        _noiseCollider.radius = vol;
    }


    //set up line of sight checks
    //if _noiseCollider overlaps object tagged enemy
    //send a raycast to that direction.
    //if raycast.hit is enemy
    //add to that enemies suspicion

    public void findEnemy()
    {
        //_noiseCollider.Raycast( ray, out RaycastHit  hitInfo);
        



    }

    void OnCollisionEnter(Collision collision) //on enter collision
    {

        foreach (ContactPoint contact in collision.contacts) //for each object in collision
        {
            Debug.Log(contact.thisCollider.gameObject.name);
            Type type = contact.GetType(); //save type to use if needed
            if(type == typeof(EnemyMotor)) //if contact has an EnemyMotor
            {
                Debug.Log(contact.thisCollider.gameObject.name);

                if (Physics.Linecast(this.gameObject.transform.position, contact.thisCollider.transform.position)) //linecast between this object and the enemy
                {
                    //if connection is broken
                    Debug.Log("Something in the way.");
                }//if linecast succeeds
                else
                {
                    //add connection to enemy, and increase suspicion
                    contact.thisCollider.gameObject.GetComponent<EnemyBrain>().listen(noiseVolume);
                }


            }

        }
    }



}
