using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

public class AppManager : ManagerBase
{
	[Inject]
	private IBehavior _currentBehavior = null;

	private MotionClientDirector _motionClientDirector = null;
	public MotionClientDirector MotionClientDirector
	{
		get { return _motionClientDirector; }
		set
		{
			if (value is DirectorBase && value is MotionClientDirector)
			{
				_motionClientDirector = value;
			}
		}
	}

	private void Awake()
	{
		RegisterManager();		
		DontDestroyOnLoad(new GameObject("WindowSizeAdjuster", typeof(WindowSizeAdjuster)));
	}

	private void Start()
	{
		_currentBehavior.OnStart();
	}

	protected override void RegisterManager()
	{
		ManagerHub.Instance.AppManager = this;
	}

	public void Dispose()
	{
		_currentBehavior.OnQuit();
	}

	private void OnApplicationQuit()
	{
		_currentBehavior.OnQuit();
	}

	private void OnDestroy()
	{
		_currentBehavior.OnQuit();
	}
}