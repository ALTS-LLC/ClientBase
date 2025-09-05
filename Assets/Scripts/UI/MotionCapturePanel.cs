using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Evila_MotionCapture;
public class MotionCapturePanel : UIBase, IUseIinterface
{
	[SerializeField]
	private TMP_Dropdown _selectMotionCpatureDropDown = null;
	[SerializeField]
	private OptiPanel _optiPanel = null;
	[SerializeField]
	private ViconPanel _viconPanel = null;

	private int _ui_ID = 0;
	public int UI_ID => _ui_ID;

	private void Start()
	{
		Register();

		_selectMotionCpatureDropDown.onValueChanged.AddListener((value) => 
		{
			ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.CaputureSystemType = _selectMotionCpatureDropDown.options[_selectMotionCpatureDropDown.value].text;
			if (value == 0)
			{
				_optiPanel.gameObject.SetActive(true);
				_viconPanel.gameObject.SetActive(false);
				MotionCaptureStream.CurrentCaptureType = CaptureSystemType.OptiTrack;
			}
			if (value == 1)
			{
				_optiPanel.gameObject.SetActive(false);
				_viconPanel.gameObject.SetActive(true);
				MotionCaptureStream.CurrentCaptureType = CaptureSystemType.Vicon1_12;
			}
		});

		for (int i = 0; i < _selectMotionCpatureDropDown.options.Count; i++)
		{
			if (_selectMotionCpatureDropDown.options[i].text == ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.CaputureSystemType)
			{
				_selectMotionCpatureDropDown.value = i;
			}
		}

	}

	public void Register()
	{
		_ui_ID = ManagerHub.Instance.UIManager.IssueID(this);
	}
}
