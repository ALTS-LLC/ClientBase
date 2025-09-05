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

    private void Awake()
    {
		OptitrackStreamingClient = GameObject.FindAnyObjectByType<OptitrackStreamingClient>();
		ViconDataStreamClient = GameObject.FindAnyObjectByType<ViconDataStreamClient>();
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
