using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityVicon;
using ClientBase_MotionCapture;
using static OptitrackStreamingClient;
using ClientBaseUtility;

[DefaultExecutionOrder(-98)]
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
        foreach (object item in Enum.GetValues(typeof(CaptureSystemType)))
        {
            if (item.ToString() == ConfigUtility.Config.CaptureSystemConfig.CaputureSystemType)
            {
                _captureSystemType = (CaptureSystemType)item;
            }
        }

        foreach (object item in Enum.GetValues(typeof(CaptureType)))
        {
            if (item.ToString() == ConfigUtility.Config.CaptureSystemConfig.CaputureType)
            {
                _captureType = (CaptureType)item;
            }
        }

        switch (_captureSystemType)
        {
            case CaptureSystemType.OptiTrack:
                MotionCaptureStream.OptitrackStreamingClient = GameObject.FindAnyObjectByType<OptitrackStreamingClient>();

                foreach (var item in Enum.GetValues(typeof(OptitrackStreamingClient.ClientConnectionType)))
                {
                    if (item.ToString() == ConfigUtility.Config.CaptureSystemConfig.OptiConfig.ConnectionType)
                    {
                        MotionCaptureStream.OptitrackStreamingClient.ConnectionType = (OptitrackStreamingClient.ClientConnectionType)item;
                    }
                }

                MotionCaptureStream.OptitrackStreamingClient.LocalAddress = ConfigUtility.Config.CaptureSystemConfig.OptiConfig.LocalAddress;
                MotionCaptureStream.OptitrackStreamingClient.ServerAddress = ConfigUtility.Config.CaptureSystemConfig.OptiConfig.ServerAddress;
                MotionCaptureStream.OptitrackStreamingClient.ServerCommandPort = (UInt16)ConfigUtility.Config.CaptureSystemConfig.OptiConfig.ServerCommandPort;
                MotionCaptureStream.OptitrackStreamingClient.ServerDataPort = (UInt16)ConfigUtility.Config.CaptureSystemConfig.OptiConfig.ServerDataPort;
                MotionCaptureStream.OptitrackStreamingClient.DrawMarkers = ConfigUtility.Config.CaptureSystemConfig.OptiConfig.DrawMarkers;


                foreach (object item in Enum.GetValues(typeof(OptitrackBoneNameConvention)))
                {
                    if (item.ToString() == ConfigUtility.Config.CaptureSystemConfig.OptiConfig.BoneNamingConvention)
                    {
                        MotionCaptureStream.OptitrackStreamingClient.BoneNamingConvention = (OptitrackBoneNameConvention)item;
                    }
                }



                switch (_captureType)
                {
                    case CaptureType.Motion:
                        _optitrackSkeletonAnimator = GameObject.FindAnyObjectByType<OptitrackSkeletonAnimator>();
                        _optitrackSkeletonAnimator.SkeletonAssetName = ConfigUtility.Config.CaptureSystemConfig.TagName;
                        break;
                    case CaptureType.Prop:
                        _optitrackRigidBody = GameObject.FindAnyObjectByType<OptitrackRigidBody>();
                        if (int.TryParse(ConfigUtility.Config.CaptureSystemConfig.TagName, out int id))
                        {
                            _optitrackRigidBody.RigidBodyId = id;
                        }
                        break;
                    default:
                        break;
                }
                break;
            case CaptureSystemType.Vicon1_12:

                MotionCaptureStream.ViconDataStreamClient = GameObject.FindAnyObjectByType<ViconDataStreamClient>();
                MotionCaptureStream.ViconDataStreamClient.HostName = ConfigUtility.Config.CaptureSystemConfig.ViconConfig.HostName;
                MotionCaptureStream.ViconDataStreamClient.Port = ConfigUtility.Config.CaptureSystemConfig.ViconConfig.Port.ToString();
                MotionCaptureStream.ViconDataStreamClient.SubjectFilter = ConfigUtility.Config.CaptureSystemConfig.ViconConfig.SubjectFilter;
                MotionCaptureStream.ViconDataStreamClient.UsePreFetch = ConfigUtility.Config.CaptureSystemConfig.ViconConfig.UsePreFetch;
                MotionCaptureStream.ViconDataStreamClient.IsRetimed = ConfigUtility.Config.CaptureSystemConfig.ViconConfig.IsRetimed;
                MotionCaptureStream.ViconDataStreamClient.Offset = ConfigUtility.Config.CaptureSystemConfig.ViconConfig.Offset;
                MotionCaptureStream.ViconDataStreamClient.ConfigureWireless = ConfigUtility.Config.CaptureSystemConfig.ViconConfig.ConfigureWireless;



                switch (_captureType)
                {
                    case CaptureType.Motion:
                        _referenceActor = GameObject.FindAnyObjectByType<SubjectScript_for12>();
                        _referenceActor.SubjectName = ConfigUtility.Config.CaptureSystemConfig.TagName;
                        break;
                    case CaptureType.Prop:
                        _rbScript_For12 = GameObject.FindAnyObjectByType<RBScript_for12>();
                        _rbScript_For12.ObjectName = ConfigUtility.Config.CaptureSystemConfig.TagName;
                        break;
                    default:
                        break;
                }
                break;
            default:
                break;
        }
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
