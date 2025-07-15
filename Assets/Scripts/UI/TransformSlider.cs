using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.EventSystems;

public class TransformSlider : MonoBehaviour
{
	[SerializeField]
	private float _sliderSensitivity = 10;

	[SerializeField, Header("キャラクター移動用スライダー")]
	private Slider _posXSlider = null;
	[SerializeField]
	private Slider _posYSlider, _posZSlider = null;
	[SerializeField, Header("キャラクター移動用インプットフィールド")]
	private TMP_InputField _posXField = null;
	[SerializeField]
	private TMP_InputField _posYField, _posZField = null;

	[SerializeField, Header("キャラクター回転用スライダー")]
	private Slider _rotXSlider = null;
	[SerializeField]
	private Slider _rotYSlider, _rotZSlider = null;
	[SerializeField, Header("キャラクター回転用インプットフィールド")]
	private TMP_InputField _rotXField = null;
	[SerializeField]
	private TMP_InputField _rotYField, _rotZField = null;

	[SerializeField, Header("キャラクターサイズ調整用スライダー")]
	private Slider _sizeSlider = null;
	[SerializeField, Header("キャラクター回転用インプットフィールド")]
	private TMP_InputField _sizeField = null;

	private bool _isPosXSliderEdit, _isPosYSliderEdit, _isPosZSliderEdit = false;
	private bool _isRotXSliderEdit, _isRotYSliderEdit, _isRotZSliderEdit = false;
	private bool _isSizeSliderEdit = false;

	private void Start()
	{
		ListenerSetup(_posXSlider, () => _isPosXSliderEdit = true, () => _isPosXSliderEdit = false);
		ListenerSetup(_posYSlider, () => _isPosYSliderEdit = true, () => _isPosYSliderEdit = false);
		ListenerSetup(_posZSlider, () => _isPosZSliderEdit = true, () => _isPosZSliderEdit = false);

		ListenerSetup(_rotXSlider, () => _isRotXSliderEdit = true, () => _isRotXSliderEdit = false);
		ListenerSetup(_rotYSlider, () => _isRotYSliderEdit = true, () => _isRotYSliderEdit = false);
		ListenerSetup(_rotZSlider, () => _isRotZSliderEdit = true, () => _isRotZSliderEdit = false);

		ListenerSetup(_sizeSlider, () => _isSizeSliderEdit = true, () => _isSizeSliderEdit = false);
	}

	private void Update()
	{
		
		try
		{			
			if (_posXField.isFocused || _posYField.isFocused || _posZField.isFocused)
			{
				ManagerHub.Instance.AppManager.MotionClientDirector.MotionSender.gameObject.transform.position = new Vector3(
																														float.Parse(_posXField.text),
																														float.Parse(_posYField.text),
																														float.Parse(_posZField.text)
																													);
			}
			else
			{
				_posXField.text = ManagerHub.Instance.AppManager.MotionClientDirector.MotionSender.gameObject.transform.position.x.ToString();
				_posYField.text = ManagerHub.Instance.AppManager.MotionClientDirector.MotionSender.gameObject.transform.position.y.ToString();
				_posZField.text = ManagerHub.Instance.AppManager.MotionClientDirector.MotionSender.gameObject.transform.position.z.ToString();
			}

			if (_rotXField.isFocused || _rotYField.isFocused || _rotZField.isFocused)
			{
				ManagerHub.Instance.AppManager.MotionClientDirector.MotionSender.gameObject.transform.localEulerAngles = new Vector3(
																													float.Parse(_rotXField.text),
																													float.Parse(_rotYField.text),
																													float.Parse(_rotZField.text)
																								);
			}
			else
			{
				_rotXField.text = ManagerHub.Instance.AppManager.MotionClientDirector.MotionSender.gameObject.transform.localEulerAngles.x.ToString();
				_rotYField.text = ManagerHub.Instance.AppManager.MotionClientDirector.MotionSender.gameObject.transform.localEulerAngles.y.ToString();
				_rotZField.text = ManagerHub.Instance.AppManager.MotionClientDirector.MotionSender.gameObject.transform.localEulerAngles.z.ToString();
			}

			if (_sizeField.isFocused)
			{
				ManagerHub.Instance.AppManager.MotionClientDirector.MotionSender.gameObject.transform.localScale = new Vector3(
																															float.Parse(_sizeField.text),
																															float.Parse(_sizeField.text),
																															float.Parse(_sizeField.text)
																														);
			}
			else
			{
				_sizeField.text = ManagerHub.Instance.AppManager.MotionClientDirector.MotionSender.gameObject.transform.localScale.x.ToString();
			}
		}
		catch
		{ }


		SliderMove();
	}

	private void SliderMove()
	{
		if (ManagerHub.Instance.AppManager.MotionClientDirector.MotionSender != null)
		{
			if (_isPosXSliderEdit)
			{
				 ManagerHub.Instance.AppManager.MotionClientDirector.MotionSender.gameObject.transform.position += new Vector3(-_posXSlider.value * _sliderSensitivity*Time.deltaTime, 0, 0);
			}
			if (_isPosYSliderEdit)
			{
				 ManagerHub.Instance.AppManager.MotionClientDirector.MotionSender.gameObject.transform.position += new Vector3(0, _posYSlider.value * _sliderSensitivity * Time.deltaTime, 0);
			}
			if (_isPosZSliderEdit)
			{
				 ManagerHub.Instance.AppManager.MotionClientDirector.MotionSender.gameObject.transform.position += new Vector3(0, 0, _posZSlider.value * _sliderSensitivity * Time.deltaTime);
			}

			if (_isRotXSliderEdit)
			{
				 ManagerHub.Instance.AppManager.MotionClientDirector.MotionSender.gameObject.transform.localEulerAngles += new Vector3(_rotXSlider.value * _sliderSensitivity * Time.deltaTime, 0, 0);
			}
			if (_isRotYSliderEdit)
			{
				 ManagerHub.Instance.AppManager.MotionClientDirector.MotionSender.gameObject.transform.localEulerAngles += new Vector3(0, _rotYSlider.value * _sliderSensitivity * Time.deltaTime, 0);
			}
			if (_isRotZSliderEdit)
			{
				 ManagerHub.Instance.AppManager.MotionClientDirector.MotionSender.gameObject.transform.localEulerAngles += new Vector3(0, 0, _rotZSlider.value * _sliderSensitivity * Time.deltaTime);
			}

			if (_isSizeSliderEdit)
			{
				 ManagerHub.Instance.AppManager.MotionClientDirector.MotionSender.gameObject.transform.localScale += new Vector3(_sizeSlider.value * _sliderSensitivity * Time.deltaTime, _sizeSlider.value * _sliderSensitivity * Time.deltaTime, _sizeSlider.value * _sliderSensitivity * Time.deltaTime);
			}
		}
	}

	void ListenerSetup(Slider slider, Action onPointerDown, Action onPointerUp)
	{
		// EventTriggerコンポーネントを取得
		EventTrigger trigger = slider.gameObject.GetComponent<EventTrigger>();

		// PointerDownイベントの設定
		EventTrigger.Entry entryDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
		entryDown.callback.AddListener((eventData) => onPointerDown());
		trigger.triggers.Add(entryDown);

		// PointerUpイベントの設定
		EventTrigger.Entry entryUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
		entryUp.callback.AddListener((eventData) =>
		{
			onPointerUp();
			slider.value = 0;
		});
		trigger.triggers.Add(entryUp);
	}
}
