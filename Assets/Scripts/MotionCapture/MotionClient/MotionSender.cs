using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
public class MotionSender : MonoBehaviour
{
    private IPEndPoint _multicastEndpoint;
    private UdpClient _udpClient;
    public UdpClient UdpClient => _udpClient;

    private Animator _animator = null;
    public Animator Animator => _animator;

    private HumanPoseHandler _targetHundler = null;
    private Transform _hip = null;
    public Transform Hip => _hip;

    private void Start()
    {
        foreach (var item in gameObject.GetComponentsInChildren<Transform>())
        {
            if (item.gameObject.TryGetComponent<Animator>(out Animator animator))
            {
                _animator = animator;
            }
        }
        _targetHundler = new HumanPoseHandler(_animator.avatar, _animator.transform);
        _hip = _animator.GetBoneTransform(HumanBodyBones.Hips);
        ManagerHub.Instance.AppManager.MotionClientDirector.MotionSender = this;
        MotionCaptureStream.TargetModel = gameObject.transform;
    }

    public void UDPConnect()
	{
        IPAddress interfaceAddress = IPAddress.Parse(ManagerHub.Instance.DataManager.Config.LocalIP);
        IPAddress multicastAddress = IPAddress.Parse(ManagerHub.Instance.DataManager.Config.MultiCastIP);
        IPEndPoint localEndPoint = new IPEndPoint(interfaceAddress, int.Parse(ManagerHub.Instance.DataManager.Config.SendlPort));

        _udpClient = new UdpClient(localEndPoint);
        _multicastEndpoint = new IPEndPoint(multicastAddress, int.Parse(ManagerHub.Instance.DataManager.Config.SendlPort));
    }

    byte[] GetMessageByte(string message)
    {
        return Encoding.UTF8.GetBytes(message);
    }

    private void Update()
    {
		if (_udpClient == null)
		{
            UDPConnect();
		}

        var message = SendMuscle();
        var messageByte = GetMessageByte(message);

        _udpClient.Send(messageByte, messageByte.Length, _multicastEndpoint);
    }

    private string SendMuscle()
    {
        HumanPose humanPose = new HumanPose();
        _targetHundler.GetHumanPose(ref humanPose);

        string str = "";
        foreach (float item in humanPose.muscles)
        {

            str += (item).ToString() + "=";
        }

        str += (_hip.position.x).ToString() + "=" + (_hip.position.y).ToString() + "=" + (_hip.position.z).ToString() + "=" +
                    _hip.transform.rotation.x.ToString() + "=" + _hip.transform.rotation.y.ToString() + "=" + _hip.transform.rotation.z.ToString() + "=" + _hip.transform.rotation.w.ToString() + "=" +
                   gameObject.transform.localScale.x;
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
