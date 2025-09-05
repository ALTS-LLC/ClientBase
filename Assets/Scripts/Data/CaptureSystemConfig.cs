using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CaptureSystemConfig : ScriptableObject
{
    public string CaputureSystemType;
    public string CaputureType;
    public string TagName;

    public ViconConfig ViconConfig;
    public OptiConfig OptiConfig;
}
