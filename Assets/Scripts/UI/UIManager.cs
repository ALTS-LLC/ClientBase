using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : ManagerBase
{
	private List<UIBase> _uis = new List<UIBase>();
	public List<UIBase> UIs => _uis;

	private void Awake()
	{
		RegisterManager();
	}
	protected override void RegisterManager()
	{
		ManagerHub.Instance.UIManager = this;
	}

	public int IssueID(UIBase uIBase)
	{
		_uis.Add(uIBase);
		return _uis.Count;
	}
}
