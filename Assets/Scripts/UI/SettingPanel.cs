using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class SettingPanel : UIBase, IUseIinterface
{
	[SerializeField]
	private TMP_Dropdown _ipDropDown = null;
	public TMP_Dropdown IpDropDown => _ipDropDown;

	[SerializeField]
	private TMP_InputField _sendPortField = null;
	public TMP_InputField SendPortField => _sendPortField;

	[SerializeField]
	private TMP_InputField _sendIPField = null;
	public TMP_InputField SendIPField => _sendIPField;

	[SerializeField]
	private Button _reconnectButton = null;

	private void Awake()
	{
		_sendPortField.onEndEdit.AddListener(delegate
		{
			ManagerHub.Instance.DataManager.Config.SendlPort = _sendPortField.text;
		});
		_sendIPField.onEndEdit.AddListener(delegate
		{
			ManagerHub.Instance.DataManager.Config.MultiCastIP = _sendIPField.text;
		});
		_ipDropDown.onValueChanged.AddListener((value) =>
		{
			ManagerHub.Instance.DataManager.Config.LocalIP = _ipDropDown.options[value].text;
		});
		_reconnectButton.onClick.AddListener(() => 
		{
			SceneManager.LoadScene("ClientMain");
		});
	}

	private void Start()
	{
		Register();
		foreach (var item in ManagerHub.Instance.NetworkManager.LocalIPAddressList)
		{
			_ipDropDown.options.Add(new TMP_Dropdown.OptionData { text = item });
		}
		_sendPortField.text = ManagerHub.Instance.DataManager.Config.SendlPort;
		_sendIPField.text = ManagerHub.Instance.DataManager.Config.MultiCastIP;
	}

	private void Update()
	{
		_ipDropDown.captionText.text = _ipDropDown.options[_ipDropDown.value].text;
	}

	private int _ui_ID = 0;
	public int UI_ID => _ui_ID;

	public void Register()
	{
		_ui_ID = ManagerHub.Instance.UIManager.IssueID(this);
	}
}
