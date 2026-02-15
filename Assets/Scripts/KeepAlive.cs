using UnityEngine;

public static class KeepAlive
{
    private const string ServiceClass = "com.example.keepalive.KeepAliveService";

    public static void StartService()
    {
#if UNITY_ANDROID && !UNITY_EDITOR

    using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
    using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

    using var intent = new AndroidJavaObject(
        "android.content.Intent",
        activity,
        new AndroidJavaClass("java.lang.Class").CallStatic<AndroidJavaObject>(
            "forName",
            "com.example.keepalive.KeepAliveService"
        )
    );

    using var version = new AndroidJavaClass("android.os.Build$VERSION");
    int sdk = version.GetStatic<int>("SDK_INT");

    if (sdk >= 26)
    {
        activity.Call("startForegroundService", intent);
    }
    else
    {
        activity.Call("startService", intent);
    }

#endif
    }


    public static void StopService()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

        using var serviceClass = new AndroidJavaClass(ServiceClass);
        using var intent = new AndroidJavaObject("android.content.Intent", activity, serviceClass);

        activity.Call<bool>("stopService", intent);
#endif
    }
}
