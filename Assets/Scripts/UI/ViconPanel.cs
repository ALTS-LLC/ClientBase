using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ClientBaseUtility;

public class ViconPanel : UIBase, IUseIinterface
{
    [SerializeField]
    private TMP_InputField _hostNameField = null;
    public TMP_InputField HostNameField => _hostNameField;

    [SerializeField]
    private TMP_InputField _portField = null;
    public TMP_InputField PortField => _portField;

    [SerializeField]
    private TMP_InputField _subjectFilterField = null;
    public TMP_InputField SubjectFilterField => _subjectFilterField;

    [SerializeField]
    private Toggle _usePreFetchToggle = null;
    public Toggle UsePreFetchToggle => _usePreFetchToggle;

    [SerializeField]
    private Toggle _isRetimedToggle = null;
    public Toggle IsRetimedToggle => _isRetimedToggle;

    [SerializeField]
    private TMP_InputField _offsetField = null;
    public TMP_InputField OffsetField => _offsetField;

    [SerializeField]
    private Toggle _logToggle = null;
    public Toggle LogToggle => _logToggle;

    [SerializeField]
    private Toggle _configureWirelessToggle = null;
    public Toggle ConfigureWirelessToggle => _configureWirelessToggle;

    private int _ui_ID = 0;
    public int UI_ID => _ui_ID;

    private void Start()
    {
        Register();

        _hostNameField.text = ConfigUtility.Config.CaptureSystemConfig.ViconConfig.HostName;
        _portField.text = ConfigUtility.Config.CaptureSystemConfig.ViconConfig.Port.ToString();
        _subjectFilterField.text = ConfigUtility.Config.CaptureSystemConfig.ViconConfig.SubjectFilter;
        _usePreFetchToggle.isOn = ConfigUtility.Config.CaptureSystemConfig.ViconConfig.UsePreFetch;
        _isRetimedToggle.isOn = ConfigUtility.Config.CaptureSystemConfig.ViconConfig.IsRetimed;
        _offsetField.text = ConfigUtility.Config.CaptureSystemConfig.ViconConfig.Offset.ToString();
        _logToggle.isOn = ConfigUtility.Config.CaptureSystemConfig.ViconConfig.Log;
        _configureWirelessToggle.isOn = ConfigUtility.Config.CaptureSystemConfig.ViconConfig.ConfigureWireless;

        _hostNameField.onEndEdit.AddListener(delegate
        {
            try
            {
                ConfigUtility.Config.CaptureSystemConfig.ViconConfig.HostName = _hostNameField.text;
                MotionCaptureStream.ViconDataStreamClient.HostName = _hostNameField.text;
            }
            catch { }
        });

        _portField.onEndEdit.AddListener(delegate
        {
            try
            {
                ConfigUtility.Config.CaptureSystemConfig.ViconConfig.Port = int.Parse(_portField.text);
                MotionCaptureStream.ViconDataStreamClient.Port = _portField.text;
            }
            catch { }
        });

        _subjectFilterField.onEndEdit.AddListener(delegate
        {
            try
            {
                ConfigUtility.Config.CaptureSystemConfig.ViconConfig.SubjectFilter = _subjectFilterField.text;
                MotionCaptureStream.ViconDataStreamClient.SubjectFilter = _subjectFilterField.text;
            }
            catch { }
        });

        _usePreFetchToggle.onValueChanged.AddListener((value) =>
        {
            ConfigUtility.Config.CaptureSystemConfig.ViconConfig.UsePreFetch = _usePreFetchToggle.isOn;
            MotionCaptureStream.ViconDataStreamClient.ConfigureWireless = _usePreFetchToggle.isOn;
        });

        _isRetimedToggle.onValueChanged.AddListener((value) =>
        {
            ConfigUtility.Config.CaptureSystemConfig.ViconConfig.IsRetimed = _isRetimedToggle.isOn;
            MotionCaptureStream.ViconDataStreamClient.IsRetimed = _isRetimedToggle.isOn;
        });

        _offsetField.onEndEdit.AddListener(delegate
        {
            try
            {
                ConfigUtility.Config.CaptureSystemConfig.ViconConfig.Offset = float.Parse(_offsetField.text);
                MotionCaptureStream.ViconDataStreamClient.Offset = float.Parse(_offsetField.text);
            }
            catch { }
        });

        _logToggle.onValueChanged.AddListener((value) =>
        {
            ConfigUtility.Config.CaptureSystemConfig.ViconConfig.Log = _logToggle.isOn;
            MotionCaptureStream.ViconDataStreamClient.Log = _logToggle.isOn;
        });

        _configureWirelessToggle.onValueChanged.AddListener((value) =>
        {
            ConfigUtility.Config.CaptureSystemConfig.ViconConfig.ConfigureWireless = _configureWirelessToggle.isOn;
            MotionCaptureStream.ViconDataStreamClient.ConfigureWireless = _configureWirelessToggle.isOn;
        });
    }

    public void Register()
    {        
        _ui_ID = ManagerHub.Instance.UIManager.IssueID(this);
    }
}
