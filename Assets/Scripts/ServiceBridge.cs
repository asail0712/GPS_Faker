using UnityEngine;

public class ServiceBridge : MonoBehaviour
{
    public void OnServiceTick(string msg)
    {
        Debug.Log("[From Service] " + msg);
    }

    void Start()
    {
        KeepAlive.StartService();
    }

    void OnDestroy()
    {
        KeepAlive.StopService();
    }
}
