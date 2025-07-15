using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityVicon;

public class MotionClientDirector : DirectorBase
{
	[SerializeField]
	private ViconDataStreamClient _viconData = null;
	public ViconDataStreamClient ViconDataStreamClient => _viconData;

	[SerializeField]
	private OptitrackStreamingClient _optitrackStreamingClient = null;
	public OptitrackStreamingClient OptitrackStreamingClient => _optitrackStreamingClient;

	[SerializeField]
	private GameObject _motionClientAsset = null;

	private MotionSender _motionSender = null;
	public MotionSender MotionSender
	{
		get { return _motionSender; }
		set
		{
			_motionSender = value;

			 _optitrackSkeletonAnimator =  _motionSender.gameObject.AddComponent<OptitrackSkeletonAnimator>();
			_optitrackSkeletonAnimator.StreamingClient = _optitrackStreamingClient;
			_optitrackSkeletonAnimator.DestinationAvatar = _motionSender.Animator.avatar;
			_optitrackSkeletonAnimator.SkeletonAssetName = ManagerHub.Instance.DataManager.Config.TagName;

			_subjectScript_For12 = _motionSender.gameObject.AddComponent<SubjectScript_for12>();
			_subjectScript_For12.Client = _viconData;
			_subjectScript_For12.SubjectName = ManagerHub.Instance.DataManager.Config.TagName;
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
	private MotionCaptureType _currentCaptureType = MotionCaptureType.OptiTrack;
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
						 
						break;
					case MotionCaptureType.Vicon1_12:
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
