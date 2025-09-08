using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class ManagerHub : SingletonMonoBehaviour<ManagerHub>
{

	private AppManager _appManager = null;
	public AppManager AppManager
	{
		get { return _appManager; }
		set
		{
			if (value is ManagerBase && value is AppManager)
			{
				_appManager = value;
			}
		}
	}

	private UIManager _uiManager = null;
	public UIManager UIManager
	{
		get { return _uiManager; }
		set
		{
			if (value is ManagerBase && value is UIManager)
			{
				_uiManager = value;
			}
		}
	}

	private NetworkManager _networkManager = null;
	public NetworkManager NetworkManager
	{
		get { return _networkManager; }
		set
		{
			if (value is ManagerBase && value is NetworkManager)
			{
				_networkManager = value;
			}
		}
	}
}