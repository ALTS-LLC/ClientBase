using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using ClientBaseUtility;

public class OptiPanel : UIBase, IUseIinterface
{
	[SerializeField]
	private TMP_Dropdown _connectionTypeDropdown = null;
	public TMP_Dropdown ConnectionTypeDropdown => _connectionTypeDropdown;

	[SerializeField]
	private TMP_InputField _localAddressField = null;
	public TMP_InputField LocalAddressField => _localAddressField;

	[SerializeField]
	private TMP_InputField _serverAddressField = null;
	public TMP_InputField ServerAddressField => _serverAddressField;

	[SerializeField]
	private TMP_InputField _serverCommandPortField = null;
	public TMP_InputField ServerCommandPortField => _serverCommandPortField;

	[SerializeField]
	private TMP_InputField _serverDataPortField = null;
	public TMP_InputField ServerDataPortField => _serverDataPortField;

	[SerializeField]
	private Toggle _drawMakerToggle = null;
	public Toggle DrawMakerToggle => _drawMakerToggle;

	[SerializeField]
	private TMP_Dropdown _boneNamingConventionDropdown = null;
	public TMP_Dropdown BoneNamingConventionDropdown => _boneNamingConventionDropdown;


	private void Start()
	{
		Register();

		_localAddressField.text = ConfigUtility.Config.CaptureSystemConfig.OptiConfig.LocalAddress;
		_serverAddressField.text = ConfigUtility.Config.CaptureSystemConfig.OptiConfig.ServerAddress;
		_serverCommandPortField.text = ConfigUtility.Config.CaptureSystemConfig.OptiConfig.ServerCommandPort.ToString();
		_serverDataPortField.text = ConfigUtility.Config.CaptureSystemConfig.OptiConfig.ServerCommandPort.ToString();
		_drawMakerToggle.isOn = ConfigUtility.Config.CaptureSystemConfig.OptiConfig.DrawMarkers;

		int i = 0;
		foreach (var item in Enum.GetValues(typeof(OptitrackStreamingClient.ClientConnectionType)))
		{
			_connectionTypeDropdown.options.Add(new TMP_Dropdown.OptionData { text = item.ToString() });
			if (item.ToString() == ConfigUtility.Config.CaptureSystemConfig.CaputureSystemType)
			{
				_connectionTypeDropdown.value = i;
			}
			i++;
		}
		_connectionTypeDropdown.onValueChanged.AddListener((value) =>
		{
			if (value == 0)
			{
				MotionCaptureStream.OptitrackStreamingClient.ConnectionType = OptitrackStreamingClient.ClientConnectionType.Multicast;
			}
			if (value == 1)
			{
				MotionCaptureStream.OptitrackStreamingClient.ConnectionType = OptitrackStreamingClient.ClientConnectionType.Unicast;
			}
			ConfigUtility.Config.CaptureSystemConfig.OptiConfig.ConnectionType = _connectionTypeDropdown.options[_connectionTypeDropdown.value].text;
		});

		_localAddressField.onEndEdit.AddListener(delegate
		{
			MotionCaptureStream.OptitrackStreamingClient.LocalAddress = _localAddressField.text;
			ConfigUtility.Config.CaptureSystemConfig.OptiConfig.LocalAddress = _localAddressField.text;
		});

		_serverAddressField.onEndEdit.AddListener(delegate
		{
			MotionCaptureStream.OptitrackStreamingClient.ServerAddress = _serverAddressField.text;
			ConfigUtility.Config.CaptureSystemConfig.OptiConfig.ServerAddress = _serverAddressField.text;
		});

		_serverCommandPortField.onEndEdit.AddListener(delegate
		{
			try
			{
				MotionCaptureStream.OptitrackStreamingClient.ServerCommandPort = ushort.Parse(_serverCommandPortField.text);
				ConfigUtility.Config.CaptureSystemConfig.OptiConfig.ServerCommandPort = ushort.Parse(_serverCommandPortField.text);
			}
			catch { }
		});

		_serverDataPortField.onEndEdit.AddListener(delegate
		{
			try
			{
				MotionCaptureStream.OptitrackStreamingClient.ServerDataPort = ushort.Parse(_serverDataPortField.text);
				ConfigUtility.Config.CaptureSystemConfig.OptiConfig.ServerCommandPort = ushort.Parse(_serverDataPortField.text);
			}
			catch { }
		});

		_drawMakerToggle.onValueChanged.AddListener((value) =>
		{
			MotionCaptureStream.OptitrackStreamingClient.DrawMarkers = value;
			ConfigUtility.Config.CaptureSystemConfig.OptiConfig.DrawMarkers = _drawMakerToggle.isOn;
		});

		i = 0;
		foreach (object item in Enum.GetValues(typeof(OptitrackBoneNameConvention)))
		{
			_boneNamingConventionDropdown.options.Add(new TMP_Dropdown.OptionData { text = item.ToString() });
			if (item.ToString() == ConfigUtility.Config.CaptureSystemConfig.OptiConfig.BoneNamingConvention)
			{
				_boneNamingConventionDropdown.value = i;
			}
			i++;
		}
		_boneNamingConventionDropdown.onValueChanged.AddListener((value) =>
		{
			if (value == 0)
			{
				MotionCaptureStream.OptitrackStreamingClient.BoneNamingConvention = OptitrackBoneNameConvention.Motive;
			}
			if (value == 1)
			{
				MotionCaptureStream.OptitrackStreamingClient.BoneNamingConvention = OptitrackBoneNameConvention.FBX;
			}
			if (value == 2)
			{
				MotionCaptureStream.OptitrackStreamingClient.BoneNamingConvention = OptitrackBoneNameConvention.BVH;
			}
			ConfigUtility.Config.CaptureSystemConfig.OptiConfig.BoneNamingConvention = _boneNamingConventionDropdown.options[_boneNamingConventionDropdown.value].text;
		});
	}

	private int _ui_ID = 0;
	public int UI_ID => _ui_ID;

	public void Register()
	{
		_ui_ID = ManagerHub.Instance.UIManager.IssueID(this);
	}
}
