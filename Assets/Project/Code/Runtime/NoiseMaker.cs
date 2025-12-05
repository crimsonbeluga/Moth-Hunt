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
            findEnemy(this.transform.position, _noiseCollider.radius);
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



    public void findEnemy(Vector3 center, float radius)
    {
        Collider[] hitColliders = Physics.OverlapSphere(center, radius);//array of all colissions within the sphere
        foreach (var hitCollider in hitColliders)
        {// for each collider
            if (hitCollider.gameObject.layer == 6)// Layer 6 is Enemy, therfore if the game object is on enemy layer
            {
                //can safely be removed later
                Debug.Log("Found An ENEMY!");

                //create a vector for the raycast to follow
                Vector3 directionToTarget = (hitCollider.transform.position - this.gameObject.transform.position).normalized;
                //shoot a raycast to the enemy
                Physics.Raycast(transform.position, directionToTarget, out RaycastHit hitInfo, _noiseCollider.radius);
                //if raycasts hits the enemy object found in collider
                if(hitInfo.collider == hitCollider)
                {
                    //increase enemy suspicion
                    hitCollider.gameObject.GetComponent<EnemyBrain>().listen(noiseVolume);
                }
                else
                {
                    //no line of sight, no noise made. object blocked
                    Debug.Log(hitInfo.collider.gameObject.name + " is in the way.");
                }


            }
        }

    }




}
