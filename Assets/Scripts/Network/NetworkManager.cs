using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;

public class NetworkManager : ManagerBase
{
	private List<string> _localIPAddressList = new List<string>();
    public List<string> LocalIPAddressList
	{
        get
		{
			if (_localIPAddressList.Count == 0)
			{
                _localIPAddressList = GetLocalIPAddress();
			}
            return _localIPAddressList;
		}
	}

	private void Awake()
	{
		RegisterManager();
        GetLocalIPAddress();
    }
	protected override void RegisterManager()
	{
		ManagerHub.Instance.NetworkManager = this;
	}

    private List<string> GetLocalIPAddress()
    {
        List<string> ipList = new List<string>();
        string hostName = Dns.GetHostName();
        IPHostEntry ipEntry = Dns.GetHostEntry(hostName);

        foreach (IPAddress ip in ipEntry.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork) // InterNetwork‚ÍIPv4‚ðŽw‚·
            {
                ipList.Add(ip.ToString());
            }
        }
        return ipList;
    }
}
