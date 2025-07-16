using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityVicon;
using static OptitrackStreamingClient;

public class MotionClientDirector : DirectorBase
{
	[SerializeField]
	private ViconDataStreamClient _viconData = null;
	public ViconDataStreamClient ViconDataStreamClient { get; private set; } = null;

	[SerializeField]
	private OptitrackStreamingClient _optitrackStreamingClient = null;
	public OptitrackStreamingClient OptitrackStreamingClient { get; private set; } = null;

    [SerializeField]
	private GameObject _motionClientAsset = null;

	[SerializeField]
	private SubjectScript_for12 _viconActor = null;
	private SubjectScript_for12 _referenceActor = null;

	private MotionSender _motionSender = null;
	public MotionSender MotionSender
	{
		get { return _motionSender; }
		set
		{
            _motionSender = value;

            if (ManagerHub.Instance.DataManager.Config.EquipmentType == "Vicon 1.12")
            {
                CurrentCaptureType = MotionCaptureType.Vicon1_12;

				_referenceActor = Instantiate(_viconActor).GetComponent<SubjectScript_for12>();
                _referenceActor.Client = ViconDataStreamClient;
                _referenceActor.SubjectName = ManagerHub.Instance.DataManager.Config.TagName;
				var boneTracer =_motionSender.gameObject.AddComponent<BoneTracer>();
				boneTracer.TargetAnimator = _referenceActor.gameObject.GetComponent<Animator>();
            }
            if (ManagerHub.Instance.DataManager.Config.EquipmentType == "OptiTrack")
            {
                CurrentCaptureType = MotionCaptureType.OptiTrack;

                _optitrackSkeletonAnimator = _motionSender.gameObject.AddComponent<OptitrackSkeletonAnimator>();
                _optitrackSkeletonAnimator.StreamingClient = _optitrackStreamingClient;
                _optitrackSkeletonAnimator.DestinationAvatar = _motionSender.Animator.avatar;
                _optitrackSkeletonAnimator.SkeletonAssetName = ManagerHub.Instance.DataManager.Config.TagName;
            }
		}
	}

	private SubjectScript_for12 _subjectScript_For12 = null;
	public SubjectScript_for12 SubjectScript_For12 => _subjectScript_For12;

	private OptitrackSkeletonAnimator _optitrackSkeletonAnimator = null;
	public OptitrackSkeletonAnimator OptitrackSkeletonAnimator => _optitrackSkeletonAnimator;

	private string _tagName = null;
	public string TagName
	{
		get { return _tagName; }
		set
		{
			_subjectScript_For12.SubjectName = value;
			_optitrackSkeletonAnimator.SkeletonAssetName = value;
		}
	}
	private MotionCaptureType _currentCaptureType = MotionCaptureType.None;
	public MotionCaptureType CurrentCaptureType
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

                        OptitrackStreamingClient = Instantiate(_optitrackStreamingClient).gameObject.GetComponent<OptitrackStreamingClient>();


                        foreach (object item in Enum.GetValues(typeof(OptitrackStreamingClient.ClientConnectionType)))
                        {
                            if (item.ToString() == ManagerHub.Instance.DataManager.Config.EquipmentType)
                            {
								OptitrackStreamingClient.ConnectionType = (ClientConnectionType)item;
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

						ViconDataStreamClient = Instantiate(_viconData).gameObject.GetComponent<ViconDataStreamClient>();

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

	private void Start()
	{
		RegisterDirector();
		Instantiate(_motionClientAsset);
	}

	protected override void RegisterDirector()
	{
		ManagerHub.Instance.AppManager.MotionClientDirector = this;
	}
}
