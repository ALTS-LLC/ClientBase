using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityVicon;
using Evila_MotionCapture;
using static OptitrackStreamingClient;

public class MotionClientDirector : DirectorBase
{
    [SerializeField]
	private GameObject _motionClientAsset = null;

	[SerializeField]
	private SubjectScript_for12 _viconActor = null;

	private SubjectScript_for12 _referenceActor = null;
	private OptitrackSkeletonAnimator _optitrackSkeletonAnimator = null;

	private MotionSender _motionSender = null;
	public MotionSender MotionSender
	{
		get { return _motionSender; }
		set
		{
            _motionSender = value;

            if (ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.CaputureSystemType == "Vicon 1.12")
            {
                MotionCaptureStream.CurrentCaptureType = CaptureSystemType.Vicon1_12;

				_referenceActor = Instantiate(_viconActor).GetComponent<SubjectScript_for12>();
                _referenceActor.Client = MotionCaptureStream.ViconDataStreamClient;
                _referenceActor.SubjectName = ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.TagName;
				var boneTracer =_motionSender.gameObject.AddComponent<BoneTracer>();
				boneTracer.TargetAnimator = _referenceActor.gameObject.GetComponent<Animator>();
            }
            if (ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.CaputureSystemType== "OptiTrack")
            {
				return;
				MotionCaptureStream.CurrentCaptureType = CaptureSystemType.OptiTrack;

                _optitrackSkeletonAnimator.StreamingClient = MotionCaptureStream.OptitrackStreamingClient;
                _optitrackSkeletonAnimator.DestinationAvatar = _motionSender.Animator.avatar;
                _optitrackSkeletonAnimator.SkeletonAssetName = ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.TagName;
            }
		}
	}


	private string _tagName = null;
	public string TagName
	{
		get { return _tagName; }
		set
		{
			if (_tagName != value && _referenceActor != null)
			{
                _referenceActor.SubjectName = value;
            }
			if (_tagName != value && _optitrackSkeletonAnimator != null)
			{
                _optitrackSkeletonAnimator.SkeletonAssetName = value;
            }			
		}
	}

	private CaptureSystemType _motionCaptureType = CaptureSystemType.None;
	public CaptureSystemType MotionCaptureType => _motionCaptureType;
	private CaptureType _captureType = CaptureType.Motion;
	public CaptureType  CaptureType => _captureType;



    private void Awake()
    {
		_optitrackSkeletonAnimator = GameObject.FindAnyObjectByType<OptitrackSkeletonAnimator>();
    }

    private void Start()
	{
		RegisterDirector();
		InstanceDirectorAsset();
    }

	protected override void RegisterDirector()
	{
		ManagerHub.Instance.AppManager.MotionClientDirector = this;
	}

    protected override void InstanceDirectorAsset()
    {
        Instantiate(_motionClientAsset);
    }

	public void Initialize()
	{

	}
}

namespace Evila_MotionCapture
{
    public enum CaptureSystemType
    {
        None,
        OptiTrack,
        Vicon1_12
    }
    public enum CaptureType
    {
		Motion,
		Prop
    }
}
