using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ClientBaseUtility;

public class ActorPanel : UIBase, IUseIinterface
{
	[SerializeField]
	private TMP_InputField _tagNameField = null;

	private int _ui_ID = 0;
	public int UI_ID => _ui_ID;

	private void Start()
	{
		Register();

		_tagNameField.text = ConfigUtility.Config.CaptureSystemConfig.TagName;

		_tagNameField.onEndEdit.AddListener(delegate
		{
			ConfigUtility.Config.CaptureSystemConfig.TagName = _tagNameField.text;
			ManagerHub.Instance.AppManager.MotionClientDirector.TagName = _tagNameField.text;
		});
	}

	public void Register()
	{
		_ui_ID = ManagerHub.Instance.UIManager.IssueID(this);
	}
}
