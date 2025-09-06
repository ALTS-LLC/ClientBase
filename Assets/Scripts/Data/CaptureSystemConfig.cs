using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CaptureSystemConfig", menuName = "ScriptableObjects/Create CaptureSystemConfig")]
public class CaptureSystemConfig : ScriptableObject
{
    public string CaputureSystemType;
    public string CaputureType;
    public string TagName;

    public ViconConfig ViconConfig;
    public OptiConfig OptiConfig;
}
