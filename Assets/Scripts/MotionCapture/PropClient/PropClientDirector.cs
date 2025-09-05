using Evila_MotionCapture;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityVicon;

public class PropClientDirector : DirectorBase
{
    [SerializeField]
    private GameObject _propClientAsset = null;

    private RBScript_for12 _rbScript = null;
    private OptitrackRigidBody _optitrackRigidBody = null;
    private PropSender _propSender = null;
	public PropSender PropSender
	{
        get { return _propSender; }
        set
		{
            _propSender = value;
            if (ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.CaputureSystemType == "Vicon 1.12")
			{
                MotionCaptureStream.CurrentCaptureType = CaptureSystemType.Vicon1_12;

                _rbScript = _propSender.gameObject.AddComponent<RBScript_for12>();
                _rbScript.Client = MotionCaptureStream.ViconDataStreamClient;
                _rbScript.ObjectName = ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.TagName;
            }
            if (ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.CaputureSystemType== "OptiTrack")
			{
                MotionCaptureStream.CurrentCaptureType = CaptureSystemType.OptiTrack;

                _optitrackRigidBody = _propSender.gameObject.AddComponent<OptitrackRigidBody>();
                _optitrackRigidBody.StreamingClient = MotionCaptureStream.OptitrackStreamingClient;

				if (int.TryParse(ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.TagName,out  int id))
				{
                    _optitrackRigidBody.RigidBodyId =id;
                }
                else
				{
                    _optitrackRigidBody.RigidBodyId = 0;

                }             
            }
        }
	}


	private string _tagName = null;
    public string TagName
    {
        get { return _tagName; }
        set
        {
            if (_tagName != value && _rbScript != null)
            {
                _rbScript.ObjectName= value;
            }
            if (_tagName != value && _optitrackRigidBody != null)
            {
				if (Int32.TryParse(value, out  Int32 id))
				{
                    _optitrackRigidBody.RigidBodyId = id;
                }                
            }
        }
    }

    private void Start()
    {
        RegisterDirector();
        InstanceDirectorAsset();
    }

    protected override void InstanceDirectorAsset()
    {
        Instantiate(_propClientAsset);
    }

    protected override void RegisterDirector()
    {
        ManagerHub.Instance.AppManager.PropClientDirector = this;
    }
}
