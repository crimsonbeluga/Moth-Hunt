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

    [Header("Logic")]
    public bool _isNoiseMadeThisFrame;

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
}
