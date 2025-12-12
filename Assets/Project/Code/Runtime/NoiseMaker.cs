using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseMaker : MonoBehaviour
{
    public SphereCollider _noiseCollider;
    public float _volumeDistance;
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
            _noiseCollider.radius -= .5f;
        }
    }



    public void MakeNoise(float vol)
    {
        _isNoiseMadeThisFrame = true;
        _noiseCollider.radius = vol;
    }
}
