using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseMaker : MonoBehaviour
{
    public float volumeDistance;

    public void MakeNoise(float vol)
    {
        volumeDistance = vol;
    }
}
