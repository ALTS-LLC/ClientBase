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

            if (ManagerHub.Instance.DataManager.Config.EquipmentType == "Vicon 1.12")
            {
                MotionCaptureStream.CurrentCaptureType = MotionCaptureStream.MotionCaptureType.Vicon1_12;

				_referenceActor = Instantiate(_viconActor).GetComponent<SubjectScript_for12>();
                _referenceActor.Client = MotionCaptureStream.ViconDataStreamClient;
                _referenceActor.SubjectName = ManagerHub.Instance.DataManager.Config.TagName;
				var boneTracer =_motionSender.gameObject.AddComponent<BoneTracer>();
				boneTracer.TargetAnimator = _referenceActor.gameObject.GetComponent<Animator>();
            }
            if (ManagerHub.Instance.DataManager.Config.EquipmentType == "OptiTrack")
            {
				return;
				MotionCaptureStream.CurrentCaptureType = MotionCaptureStream.MotionCaptureType.OptiTrack;

                _optitrackSkeletonAnimator.StreamingClient = MotionCaptureStream.OptitrackStreamingClient;
                _optitrackSkeletonAnimator.DestinationAvatar = _motionSender.Animator.avatar;
                _optitrackSkeletonAnimator.SkeletonAssetName = ManagerHub.Instance.DataManager.Config.TagName;
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
}
