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

	private Transform _targrtObj = null;

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
		if (_targrtObj == null)
		{
			_targrtObj = MotionCaptureStream.TargetModel;
		}

		try
		{			
			if (_posXField.isFocused || _posYField.isFocused || _posZField.isFocused)
			{
				_targrtObj.position = new Vector3(
																														float.Parse(_posXField.text),
																														float.Parse(_posYField.text),
																														float.Parse(_posZField.text)
																													);
			}
			else
			{
				_posXField.text = _targrtObj.position.x.ToString();
				_posYField.text = _targrtObj.position.y.ToString();
				_posZField.text = _targrtObj.position.z.ToString();
			}

			if (_rotXField.isFocused || _rotYField.isFocused || _rotZField.isFocused)
			{
				_targrtObj.localEulerAngles = new Vector3(
																													float.Parse(_rotXField.text),
																													float.Parse(_rotYField.text),
																													float.Parse(_rotZField.text)
																								);
			}
			else
			{
				_rotXField.text = _targrtObj.localEulerAngles.x.ToString();
				_rotYField.text = _targrtObj.localEulerAngles.y.ToString();
				_rotZField.text = _targrtObj.localEulerAngles.z.ToString();
			}

			if (_sizeField.isFocused)
			{
				_targrtObj.localScale = new Vector3(
																															float.Parse(_sizeField.text),
																															float.Parse(_sizeField.text),
																															float.Parse(_sizeField.text)
																														);
			}
			else
			{
				_sizeField.text = _targrtObj.localScale.x.ToString();
			}
		}
		catch
		{ }


		SliderMove();
	}

	private void SliderMove()
	{
		if (_isPosXSliderEdit)
		{
			_targrtObj.position += new Vector3(-_posXSlider.value * _sliderSensitivity * Time.deltaTime, 0, 0);
		}
		if (_isPosYSliderEdit)
		{
			_targrtObj.position += new Vector3(0, _posYSlider.value * _sliderSensitivity * Time.deltaTime, 0);
		}
		if (_isPosZSliderEdit)
		{
			_targrtObj.position += new Vector3(0, 0, _posZSlider.value * _sliderSensitivity * Time.deltaTime);
		}

		if (_isRotXSliderEdit)
		{
			_targrtObj.localEulerAngles += new Vector3(_rotXSlider.value * _sliderSensitivity * Time.deltaTime, 0, 0);
		}
		if (_isRotYSliderEdit)
		{
			_targrtObj.localEulerAngles += new Vector3(0, _rotYSlider.value * _sliderSensitivity * Time.deltaTime, 0);
		}
		if (_isRotZSliderEdit)
		{
			_targrtObj.localEulerAngles += new Vector3(0, 0, _rotZSlider.value * _sliderSensitivity * Time.deltaTime);
		}

		if (_isSizeSliderEdit)
		{
			_targrtObj.localScale += new Vector3(_sizeSlider.value * _sliderSensitivity * Time.deltaTime, _sizeSlider.value * _sliderSensitivity * Time.deltaTime, _sizeSlider.value * _sliderSensitivity * Time.deltaTime);
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
