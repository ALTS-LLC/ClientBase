using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ClientBase_MotionCapture;

public static class MotionCaptureStream
{
    public static ViconDataStreamClient ViconDataStreamClient { get; set; } = null;
	public static OptitrackStreamingClient OptitrackStreamingClient { get;set; } = null;

	public static Transform TargetModel = null;
}
