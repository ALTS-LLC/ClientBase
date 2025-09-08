using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ClientBaseUtility;

[DefaultExecutionOrder(-99)]
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

        ConfigUtility.Config = ConfigUtility.JsonToSBParser<Config>(ConfigUtility.Config, _configJsonPath);
        ConfigUtility.Config.CaptureSystemConfig = ConfigUtility.JsonToSBParser<CaptureSystemConfig>(ConfigUtility.Config.CaptureSystemConfig, _captureSystemConfigJsonPath);
        ConfigUtility.Config.CaptureSystemConfig.OptiConfig = ConfigUtility.JsonToSBParser<OptiConfig>(ConfigUtility.Config.CaptureSystemConfig.OptiConfig, _optiConfigJsonPath);
        ConfigUtility.Config.CaptureSystemConfig.ViconConfig = ConfigUtility.JsonToSBParser<ViconConfig>(ConfigUtility.Config.CaptureSystemConfig.ViconConfig, _viconConfigJsonPath);

        ManagerHub.Instance.AppManager.MotionClientDirector.Initialize();
    }
    public void OnQuit()
    {
        ConfigUtility.SBtoJsonParser<Config>(ConfigUtility.Config, _configJsonPath);
        ConfigUtility.Config.CaptureSystemConfig = ConfigUtility.SBtoJsonParser<CaptureSystemConfig>(ConfigUtility.Config.CaptureSystemConfig, _captureSystemConfigJsonPath);
        ConfigUtility.Config.CaptureSystemConfig.OptiConfig = ConfigUtility.SBtoJsonParser<OptiConfig>(ConfigUtility.Config.CaptureSystemConfig.OptiConfig, _optiConfigJsonPath);
        ConfigUtility.Config.CaptureSystemConfig.ViconConfig = ConfigUtility.SBtoJsonParser<ViconConfig>(ConfigUtility.Config.CaptureSystemConfig.ViconConfig, _viconConfigJsonPath);

    }
}