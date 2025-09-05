using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OptiConfig : ScriptableObject
{
    public string ConnectionType;
    public string LocalAddress;
    public string ServerAddress;
    public int ServerCommandPort;
    public int ServerDataPort;
    public bool DrawMarkers;
    public string BoneNamingConvention;
}
