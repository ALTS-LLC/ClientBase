using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Config : ScriptableObject
{
    public string LocalIP = null;
    public string SendlPort = null;
    public string MultiCastIP = null;

    public CaptureSystemConfig CaptureSystemConfig;
}
