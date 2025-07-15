using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIBase : MonoBehaviour
{
	protected enum SidePanelState
	{
		Anim,
		Stay
	}

	protected float AnimSidePanel(Vector2 refPos, float startTime, float animTime, bool isOpen)
	{
		var diff = Time.time - startTime;
		var rate = diff / animTime;
		return Mathf.Lerp(isOpen ? refPos.x : refPos.y, isOpen ? refPos.y : refPos.x, rate);
	}
}
