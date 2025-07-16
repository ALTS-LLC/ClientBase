using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PropClientDirector : DirectorBase
{
    [SerializeField]
    private GameObject _propClientAsset = null;

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
