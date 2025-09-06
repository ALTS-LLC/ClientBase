using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ViconConfig", menuName = "ScriptableObjects/Create ViconConfig")]
public class ViconConfig : ScriptableObject
{
    public string HostName;
    public int Port;
    public string SubjectFilter;
    public bool UsePreFetch;
    public bool IsRetimed;
    public float Offset;
    public bool Log;
    public bool ConfigureWireless;
}
