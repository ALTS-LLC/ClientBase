using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ViewController : MonoBehaviour
{
	[Header("ÉJÉÅÉâà⁄ìÆê›íË")]
	public float lookSpeed = 2.0f;
	public float moveSpeed = 5.0f;
	public float sprintMultiplier = 2.0f;
	[SerializeField]
	private float _dragSpeed = .5f;
	[SerializeField]
	private float zoomSpeed = 50.0f;

	private float rotationX = 0.0f;
	private float rotationY = 0.0f;

	private bool _isLeftFirstInputAfterClick = true;
	private bool _isCenterFirstInputAfterClick = true;

	[SerializeField]
	private EventSystem _eventSystem = null;

	void Start()
	{
		Vector3 currentEuler = transform.localEulerAngles;
		rotationY = currentEuler.y;
		rotationX = currentEuler.x;

		_isLeftFirstInputAfterClick = true;
	}

	void Update()
	{
		if (EventSystem.current.IsPointerOverGameObject())
		{
			return;
		}


			if (Input.GetMouseButton(0))
		{
			if (_isLeftFirstInputAfterClick)
			{
				Input.GetAxis("Mouse X");
				Input.GetAxis("Mouse Y");
				_isLeftFirstInputAfterClick = false;
			}
			else
			{
				rotationY += Input.GetAxis("Mouse X") * lookSpeed;
				rotationX -= Input.GetAxis("Mouse Y") * lookSpeed;
				rotationX = Mathf.Clamp(rotationX, -90f, 90f);

				transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0.0f);
			}
		}
		else
		{
			_isLeftFirstInputAfterClick = true;
		}

		if (Input.GetMouseButton(2))
		{
			if (_isCenterFirstInputAfterClick)
			{
				Input.GetAxis("Mouse X");
				Input.GetAxis("Mouse Y");
				_isCenterFirstInputAfterClick = false;
			}
			else
			{
				Vector3 moveDirection = Vector3.zero;
				moveDirection += transform.right * -Input.GetAxis("Mouse X") * _dragSpeed;
				moveDirection += transform.up * -Input.GetAxis("Mouse Y") * _dragSpeed;

				transform.position += moveDirection * Time.deltaTime;
			}

		}
		else
		{
			_isCenterFirstInputAfterClick = true;
		}

		if (Input.GetKeyDown(KeyCode.F))
		{
			transform.LookAt(Vector3.zero);
			Vector3 afterLookAtEuler = transform.localEulerAngles;
			rotationY = afterLookAtEuler.y;
			rotationX = afterLookAtEuler.x;
			rotationX = Mathf.Clamp(rotationX, -90f, 90f);
		}

		float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
		if (scrollDelta != 0)
		{
			transform.position += transform.forward * scrollDelta * zoomSpeed * Time.deltaTime;
		}

		if (Input.GetKeyDown(KeyCode.Escape))
		{
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
		}
	}
}
