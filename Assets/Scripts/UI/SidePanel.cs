using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SidePanel : UIBase, IUseIinterface
{
	[SerializeField]
	private Button _unfoldButton = null;

	[SerializeField]
	private float _animTime = 0;
	[SerializeField]
	private Vector2 _referencePos = Vector2.zero;

	private int _ui_ID = 0;
	public int UI_ID => _ui_ID;
	private float _startTime = 0;
	private SidePanelState _currentState = SidePanelState.Stay;
	private bool _isOpen = false;


	private void Start()
	{
		Register();
		_unfoldButton.onClick.AddListener(() =>
		{
			if (_currentState == SidePanelState.Anim) return;
			_isOpen = !_isOpen;
			_currentState = SidePanelState.Anim;
			_unfoldButton.enabled = false;
			_startTime = Time.time;
		});
	}
	private void Update()
	{
		if (gameObject.GetComponent<RectTransform>().anchoredPosition.x == (_isOpen ? _referencePos.y : _referencePos.x))
		{
			_currentState = SidePanelState.Stay;
			_unfoldButton.enabled = true;
		}

		if (_currentState == SidePanelState.Anim)
		{
			gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2()
			{
				x = base.AnimSidePanel(_referencePos, _startTime, _animTime, _isOpen),
				y = gameObject.GetComponent<RectTransform>().anchoredPosition.y
			};
		}
	}

	public void Register()
	{
		_ui_ID = ManagerHub.Instance.UIManager.IssueID(this);
	}
}
