using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MotionCaptureStream : MonoBehaviour
{
	[SerializeField]
	private ViconDataStreamClient _viconData = null;
	private static ViconDataStreamClient _refViconData = null;
	public static ViconDataStreamClient ViconDataStreamClient { get; private set; } = null;

	[SerializeField]
	private OptitrackStreamingClient _optitrackStreamingClient = null;
	private static OptitrackStreamingClient _refOptitrackStreamingClient = null;
	public static OptitrackStreamingClient OptitrackStreamingClient { get; private set; } = null;

	private static MotionCaptureType _currentCaptureType = MotionCaptureType.None;
	public static MotionCaptureType CurrentCaptureType
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
					case MotionCaptureType.OptiTrack:
						if (OptitrackStreamingClient != null)
						{
							Destroy(OptitrackStreamingClient.gameObject);
						}

						OptitrackStreamingClient = Instantiate(_refOptitrackStreamingClient).gameObject.GetComponent<OptitrackStreamingClient>();


						foreach (object item in Enum.GetValues(typeof(OptitrackStreamingClient.ClientConnectionType)))
						{
							if (item.ToString() == ManagerHub.Instance.DataManager.Config.EquipmentType)
							{
								OptitrackStreamingClient.ConnectionType = (OptitrackStreamingClient.ClientConnectionType)item;
							}
						}
						OptitrackStreamingClient.LocalAddress = ManagerHub.Instance.DataManager.Config.LocalAddress;
						OptitrackStreamingClient.ServerAddress = ManagerHub.Instance.DataManager.Config.ServerAddress;
						OptitrackStreamingClient.ServerCommandPort = (ushort)ManagerHub.Instance.DataManager.Config.ServerCommandPort;
						OptitrackStreamingClient.ServerDataPort = (ushort)ManagerHub.Instance.DataManager.Config.ServerDataPort;
						OptitrackStreamingClient.DrawMarkers = ManagerHub.Instance.DataManager.Config.DrawMarkers;
						foreach (object item in Enum.GetValues(typeof(OptitrackBoneNameConvention)))
						{
							if (item.ToString() == ManagerHub.Instance.DataManager.Config.BoneNamingConvention)
							{
								OptitrackStreamingClient.BoneNamingConvention = (OptitrackBoneNameConvention)item;
							}
						}

						break;
					case MotionCaptureType.Vicon1_12:
						if (ViconDataStreamClient != null)
						{
							Destroy(ViconDataStreamClient.gameObject);
						}

						ViconDataStreamClient = Instantiate(_refViconData).gameObject.GetComponent<ViconDataStreamClient>();

						ViconDataStreamClient.HostName = ManagerHub.Instance.DataManager.Config.HostName;
						ViconDataStreamClient.Port = ManagerHub.Instance.DataManager.Config.Port.ToString();
						ViconDataStreamClient.SubjectFilter = ManagerHub.Instance.DataManager.Config.SubjectFilter;
						ViconDataStreamClient.UsePreFetch = ManagerHub.Instance.DataManager.Config.UsePreFetch;
						ViconDataStreamClient.IsRetimed = ManagerHub.Instance.DataManager.Config.IsRetimed;
						ViconDataStreamClient.Offset = ManagerHub.Instance.DataManager.Config.Offset;
						ViconDataStreamClient.Log = ManagerHub.Instance.DataManager.Config.Log;
						ViconDataStreamClient.ConfigureWireless = ManagerHub.Instance.DataManager.Config.ConfigureWireless;
						break;
					default:
						break;
				}
			}
			_currentCaptureType = value;
		}
	}
	public enum MotionCaptureType
	{
		None,
		OptiTrack,
		Vicon1_12,
	}

	private void Awake()
	{
		_refViconData = _viconData;
		_refOptitrackStreamingClient = _optitrackStreamingClient;
	}
}
