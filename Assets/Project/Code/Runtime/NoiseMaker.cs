using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseMaker : MonoBehaviour
{

    public float _volumeDistance;
    public bool _isNoiseMadeThisFrame;

    public void onTick(float _curRange, float _maxRange)
    {
        //if noise made this frame
        if(_isNoiseMadeThisFrame)
        {
            //set range to max
            _curRange = _maxRange;

            //set to false so cant keep at max size
            _isNoiseMadeThisFrame = false;

        }
        else
        {
            if(_curRange < 1)
            { 
                _curRange = 1; 
            }
            _curRange -= .5f;
        }
    }



    public void MakeNoise(float vol)
    {
        _volumeDistance = vol;
    }
}
