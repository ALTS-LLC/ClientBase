using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Config", menuName = "ScriptableObjects/Create Config")]
public class Config : ScriptableObject
{
    public string LocalIP = null;
    public string SendlPort = null;
    public string MultiCastIP = null;
    public CaptureSystemConfig CaptureSystemConfig;
}
