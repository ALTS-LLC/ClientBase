using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ClientBase_MotionCapture;

public class MotionCaptureStream : MonoBehaviour
{
    public static ViconDataStreamClient ViconDataStreamClient { get; private set; } = null;
	public static OptitrackStreamingClient OptitrackStreamingClient { get; private set; } = null;

	public static Transform TargetModel = null;

    private void Awake()
    {
		OptitrackStreamingClient = GameObject.FindAnyObjectByType<OptitrackStreamingClient>();
		ViconDataStreamClient = GameObject.FindAnyObjectByType<ViconDataStreamClient>();

        ViconDataStreamClient.HostName = ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.ViconConfig.HostName;
    }

    private void OnApplicationQuit()
    {
		TargetModel = null;
    }
    private void OnDestroy()
    {
		TargetModel = null;
	}
}
