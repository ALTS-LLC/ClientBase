using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityVicon;
using ClientBase_MotionCapture;
using static OptitrackStreamingClient;

public class MotionClientDirector : DirectorBase
{
    [SerializeField]
    private GameObject _motionClientAsset = null;

    [SerializeField]
    private SubjectScript_for12 _viconActor = null;

    private OptitrackSkeletonAnimator _optitrackSkeletonAnimator = null;
    private SubjectScript_for12 _referenceActor = null;

    private OptitrackRigidBody _optitrackRigidBody  = null;
    private RBScript_for12 _rbScript_For12 = null;


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

    private CaptureSystemType _captureSystemType = CaptureSystemType.OptiTrack;
    public CaptureSystemType CaptureSystemType => _captureSystemType;
    private CaptureType _captureType = CaptureType.Motion;
    public CaptureType CaptureType => _captureType;



    private void Awake()
    {
        foreach (object item in Enum.GetValues(typeof(CaptureSystemType)))
        {
            if (item.ToString() == ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.CaputureSystemType)
            {
                _captureSystemType = (CaptureSystemType)item;
            }
        }

        foreach (object item in Enum.GetValues(typeof(CaptureType)))
        {
            if (item.ToString() == ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.CaputureType)
            {
                _captureType = (CaptureType)item;
            }
        }

        switch (_captureSystemType)
        {
            case CaptureSystemType.OptiTrack:
                switch (_captureType)
                {
                    case CaptureType.Motion:
                        _optitrackSkeletonAnimator = GameObject.FindAnyObjectByType<OptitrackSkeletonAnimator>();
                        _optitrackSkeletonAnimator.SkeletonAssetName = ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.TagName;
                        break;
                    case CaptureType.Prop:
                        _optitrackRigidBody = GameObject.FindAnyObjectByType<OptitrackRigidBody>();
                        if (int.TryParse(ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.TagName, out int id))
                        {
                            _optitrackRigidBody.RigidBodyId = id;
                        }                       
                        break;
                    default:
                        break;
                }
                break;
            case CaptureSystemType.Vicon1_12:
                switch (_captureType)
                {
                    case CaptureType.Motion:
                        _referenceActor = GameObject.FindAnyObjectByType<SubjectScript_for12>();
                        _referenceActor.SubjectName = ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.TagName;
                        break;
                    case CaptureType.Prop:
                        _rbScript_For12 = GameObject.FindAnyObjectByType<RBScript_for12>();
                        _rbScript_For12.ObjectName = ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.TagName;
                        break;
                    default:
                        break;
                }
                break;
            default:
                break;
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

    public void Initialize()
    {

    }
}

namespace ClientBase_MotionCapture
{
    public enum CaptureSystemType
    {
        OptiTrack,
        Vicon1_12
    }
    public enum CaptureType
    {
        Motion,
        Prop
    }
}
