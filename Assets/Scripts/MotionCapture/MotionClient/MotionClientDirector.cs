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

	private MotionSender _motionSender = null;
	public MotionSender MotionSender
	{
		get { return _motionSender; }
		set
		{
            _motionSender = value;
			
            if (MotionCaptureStream.CurrentCaptureType == MotionCaptureStream.MotionCaptureType.Vicon1_12)
            {
                MotionCaptureStream.CurrentCaptureType = MotionCaptureStream.MotionCaptureType.Vicon1_12;

				_referenceActor = Instantiate(_viconActor).GetComponent<SubjectScript_for12>();
                _referenceActor.Client = MotionCaptureStream.ViconDataStreamClient;
                _referenceActor.SubjectName = ManagerHub.Instance.DataManager.Config.TagName;
				var boneTracer =_motionSender.gameObject.AddComponent<BoneTracer>();
				boneTracer.TargetAnimator = _referenceActor.gameObject.GetComponent<Animator>();
            }
            if (MotionCaptureStream.CurrentCaptureType == MotionCaptureStream.MotionCaptureType.OptiTrack)
            {
				MotionCaptureStream.CurrentCaptureType = MotionCaptureStream.MotionCaptureType.OptiTrack;

                _optitrackSkeletonAnimator = _motionSender.gameObject.AddComponent<OptitrackSkeletonAnimator>();
                _optitrackSkeletonAnimator.StreamingClient = MotionCaptureStream.OptitrackStreamingClient;
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
