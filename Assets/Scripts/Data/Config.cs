using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Config : ScriptableObject
{
    public string LocalIP = null;
    public string SendlPort = null;
    public string MultiCastIP = null;

    public string EquipmentType;

    public string TagName;

    public string HostName;
    public int Port;
    public string SubjectFilter;
    public bool UsePreFetch;
    public bool IsRetimed;
    public float Offset;
    public bool Log;
    public bool ConfigureWireless;

    public string ConnectionType;
    public string LocalAddress;
    public string ServerAddress;
    public int ServerCommandPort;
    public int ServerDataPort;
    public bool DrawMarkers;
    public string BoneNamingConvention;
}
