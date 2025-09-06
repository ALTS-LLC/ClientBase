using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MotionClientBehavior : IBehavior
{
    private string _configJsonPath = null;
    private string _captureSystemConfigJsonPath = null;
    private string _optiConfigJsonPath = null;
    private string _viconConfigJsonPath = null;

    public void OnStart()
    {
        _configJsonPath = Application.dataPath + "/StreamingAssets/Config_json/config.json";
        _captureSystemConfigJsonPath = Application.dataPath + "/StreamingAssets/Config_json/capture_system_config.json";
        _optiConfigJsonPath = Application.dataPath + "/StreamingAssets/Config_json/opti_config.json";
        _viconConfigJsonPath = Application.dataPath + "/StreamingAssets/Config_json/vicon_config.json";


		ManagerHub.Instance.DataManager.Config = ManagerHub.Instance.DataManager.JsonToSBParser<Config>(ManagerHub.Instance.DataManager.Config, _configJsonPath);

        if (ManagerHub.Instance.DataManager.Config.CaptureSystemConfig == null)
        {
            ManagerHub.Instance.DataManager.ConfigObjectSerialize();
        }
        else
        {

            ManagerHub.Instance.DataManager.Config.CaptureSystemConfig = ManagerHub.Instance.DataManager.JsonToSBParser<CaptureSystemConfig>(ManagerHub.Instance.DataManager.Config.CaptureSystemConfig, _captureSystemConfigJsonPath);
        }

        ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.OptiConfig = ManagerHub.Instance.DataManager.JsonToSBParser<OptiConfig>(ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.OptiConfig, _optiConfigJsonPath);

        ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.ViconConfig = ManagerHub.Instance.DataManager.JsonToSBParser<ViconConfig>(ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.ViconConfig, _viconConfigJsonPath);


    }
    public void OnQuit()
    {
        ManagerHub.Instance.DataManager.SBtoJsonParser<Config>(ManagerHub.Instance.DataManager.Config, _configJsonPath);
        ManagerHub.Instance.DataManager.Config.CaptureSystemConfig = ManagerHub.Instance.DataManager.SBtoJsonParser<CaptureSystemConfig>(ManagerHub.Instance.DataManager.Config.CaptureSystemConfig, _captureSystemConfigJsonPath);
        ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.OptiConfig = ManagerHub.Instance.DataManager.SBtoJsonParser<OptiConfig>(ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.OptiConfig, _optiConfigJsonPath);
        ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.ViconConfig = ManagerHub.Instance.DataManager.SBtoJsonParser<ViconConfig>(ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.ViconConfig, _viconConfigJsonPath);

    }
}