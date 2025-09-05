using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Evila_MotionCapture;

public class MotionCaptureStream : MonoBehaviour
{
    public static ViconDataStreamClient ViconDataStreamClient { get; private set; } = null;
	public static OptitrackStreamingClient OptitrackStreamingClient { get; private set; } = null;

	public static Transform TargetModel = null;

	private static CaptureSystemType _currentCaptureType = CaptureSystemType.None;
	public static CaptureSystemType CurrentCaptureType
	{
		get
		{
			return _currentCaptureType;
		}
		set
		{
			if (_currentCaptureType != value)
			{
				switch (value)
				{
					case CaptureSystemType.OptiTrack:
						//if (OptitrackStreamingClient != null)
						//{
						//	Destroy(OptitrackStreamingClient.gameObject);
						//}


						foreach (object item in Enum.GetValues(typeof(OptitrackStreamingClient.ClientConnectionType)))
						{
							if (item.ToString() == ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.CaputureSystemType)
							{
								OptitrackStreamingClient.ConnectionType = (OptitrackStreamingClient.ClientConnectionType)item;
							}
						}
						OptitrackStreamingClient.LocalAddress = ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.OptiConfig.LocalAddress;
						OptitrackStreamingClient.ServerAddress = ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.OptiConfig.ServerAddress;
						OptitrackStreamingClient.ServerCommandPort = (ushort)ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.OptiConfig.ServerCommandPort;
						OptitrackStreamingClient.ServerDataPort = (ushort)ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.OptiConfig.ServerDataPort;
						OptitrackStreamingClient.DrawMarkers = ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.OptiConfig.DrawMarkers;
						foreach (object item in Enum.GetValues(typeof(OptitrackBoneNameConvention)))
						{
							if (item.ToString() == ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.OptiConfig.BoneNamingConvention)
							{
								OptitrackStreamingClient.BoneNamingConvention = (OptitrackBoneNameConvention)item;
							}
						}

						break;
					case CaptureSystemType.Vicon1_12:
						//if (ViconDataStreamClient != null)
						//{
						//	Destroy(ViconDataStreamClient.gameObject);
						//}

						ViconDataStreamClient.HostName = ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.ViconConfig.HostName;
						ViconDataStreamClient.Port = ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.ViconConfig.Port.ToString();
						ViconDataStreamClient.SubjectFilter = ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.ViconConfig.SubjectFilter;
						ViconDataStreamClient.UsePreFetch = ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.ViconConfig.UsePreFetch;
						ViconDataStreamClient.IsRetimed = ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.ViconConfig.IsRetimed;
						ViconDataStreamClient.Offset = ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.ViconConfig.Offset;
						ViconDataStreamClient.Log = ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.ViconConfig.Log;
						ViconDataStreamClient.ConfigureWireless = ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.ViconConfig.ConfigureWireless;
						break;
					default:
						break;
				}
			}
			_currentCaptureType = value;
		}
	}
    private void Awake()
    {
		OptitrackStreamingClient = GameObject.FindAnyObjectByType<OptitrackStreamingClient>();
		ViconDataStreamClient = GameObject.FindAnyObjectByType<ViconDataStreamClient>();
    }

    private void OnApplicationQuit()
    {
		_currentCaptureType = CaptureSystemType.None;
		TargetModel = null;
    }
    private void OnDestroy()
    {
        _currentCaptureType = CaptureSystemType.None;
		TargetModel = null;
	}
}
