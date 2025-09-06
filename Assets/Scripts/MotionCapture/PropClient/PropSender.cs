using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Serialization;

public class PropSender : MonoBehaviour
{
	private IPEndPoint _multicastEndpoint;
	private UdpClient _udpClient;

	private void Start()
	{
		IPAddress interfaceAddress = IPAddress.Parse(ManagerHub.Instance.DataManager.Config.LocalIP);
		IPAddress multicastAddress = IPAddress.Parse(ManagerHub.Instance.DataManager.Config.MultiCastIP);		
		IPEndPoint localEndPoint = new IPEndPoint(interfaceAddress, int.Parse(ManagerHub.Instance.DataManager.Config.SendlPort));

		_udpClient = new UdpClient(localEndPoint);
		_multicastEndpoint = new IPEndPoint(multicastAddress, int.Parse(ManagerHub.Instance.DataManager.Config.SendlPort));
        MotionCaptureStream.TargetModel = gameObject.transform;
	}


	byte[] GetMessageByte(string message)
	{
		return Encoding.UTF8.GetBytes(message);
	}

	private void Update()
	{
		//gameObject.transform.eulerAngles = new Vector3(gameObject.transform.eulerAngles.x,
		//    gameObject.transform.eulerAngles.y + 180, -gameObject.transform.eulerAngles.z);

		var message = SendCamera();
		var messageByte = GetMessageByte(message);
		//ameObject.transform.rotation = new Quaternion(gameObject.transform.rotation.x,_hip.transform.rotation.y,gameObject.transform.rotation.z,gameObject.transform.rotation.w);

		_udpClient.Send(messageByte, messageByte.Length, _multicastEndpoint);
	}

	private string SendCamera()
	{
		string str = "";
		str += gameObject.transform.position.x + "=" +  //0
			   gameObject.transform.position.y + "=" +  //1
			   gameObject.transform.position.z + "=" +  //2
			   gameObject.transform.rotation.x + "=" +  //3
			   gameObject.transform.rotation.y + "=" +  //4
			   gameObject.transform.rotation.z + "=" +  //5
			   gameObject.transform.rotation.w;         //6
		return str;
	}


	public T Load<T>(string path)
	{
		try
		{
			StreamReader reader = new StreamReader(path);
			string datastr = reader.ReadToEnd();
			reader.Close();
			return JsonUtility.FromJson<T>(datastr);
		}
		catch
		{
			Debug.LogWarning("���[�h���s");
			return default;
		}
	}

	private void OnDestroy()
	{
		_udpClient.Close();
	}
}