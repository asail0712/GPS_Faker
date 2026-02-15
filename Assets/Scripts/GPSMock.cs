using UnityEngine;

public static class GPSMock
{
    public static void SetLocation(double lat, double lng)
    {
#if UNITY_ANDROID && !UNITY_EDITOR

        using var unityPlayer =
            new AndroidJavaClass("com.unity3d.player.UnityPlayer");

        using var activity =
            unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

        using var plugin =
            new AndroidJavaObject("com.example.gpsmock.GPSMockPlugin");

        plugin.Call("setMockLocation", activity, lat, lng);

#endif
    }
}
