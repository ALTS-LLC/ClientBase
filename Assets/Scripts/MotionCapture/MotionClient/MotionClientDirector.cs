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
			_subjectScript_For12.SubjectName = value;
			_optitrackSkeletonAnimator.SkeletonAssetName = value;
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
