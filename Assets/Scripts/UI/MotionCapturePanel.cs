using ClientBase_MotionCapture;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class MotionCapturePanel : UIBase, IUseIinterface
{
	[SerializeField]
	private OptiPanel _optiPanel = null;
	[SerializeField]
	private ViconPanel _viconPanel = null;

	private int _ui_ID = 0;
	public int UI_ID => _ui_ID;

	private void Start()
	{
		Register();

		switch (ManagerHub.Instance.AppManager.MotionClientDirector.CaptureSystemType)
		{
			case CaptureSystemType.OptiTrack:
				Instantiate(_optiPanel, parent: transform);
				break;
			case CaptureSystemType.Vicon1_12:
                Instantiate(_viconPanel, parent: transform);
                break;
		}
	}

    public void Register()
	{
		_ui_ID = ManagerHub.Instance.UIManager.IssueID(this);
	}
}
