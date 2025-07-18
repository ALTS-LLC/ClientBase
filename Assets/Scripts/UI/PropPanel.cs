using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PropPanel : UIBase, IUseIinterface
{
	[SerializeField]
	private TMP_InputField _tagNameField = null;

	private int _ui_ID = 0;
	public int UI_ID => _ui_ID;

	private void Start()
	{
		Register();

		_tagNameField.text = ManagerHub.Instance.DataManager.Config.TagName;

		_tagNameField.onEndEdit.AddListener(delegate
		{
			ManagerHub.Instance.DataManager.Config.TagName = _tagNameField.text;
			ManagerHub.Instance.AppManager.PropClientDirector.TagName = _tagNameField.text;
		});
	}

	public void Register()
	{
		_ui_ID = ManagerHub.Instance.UIManager.IssueID(this);
	}
}
