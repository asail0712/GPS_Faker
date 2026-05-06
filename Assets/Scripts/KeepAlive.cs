using UnityEngine;

public static class KeepAlive
{
    private const string ServiceClass = "com.example.gpsmock.GPSMockPlugin$KeepAliveService";

    public static void StartService()
    {
        StartService(null, null);
    }

    public static void StartService(double lat, double lng)
    {
        StartService(lat, lng, 50.0);
    }

    public static void StartService(double lat, double lng, double radiusMeters)
    {
        StartService((double?)lat, (double?)lng, (double?)radiusMeters);
    }

    private static void StartService(double? lat, double? lng)
    {
        StartService(lat, lng, null);
    }

    private static void StartService(double? lat, double? lng, double? radiusMeters)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // C# 只負責啟動 Android service。
        // 持續刷新假定位的工作放在 Java service 裡執行。
        using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

        // 不使用 Class.forName，避免 Unity/Android classloader 先丟例外。
        // Android 會依照 manifest component 啟動 service。
        using var intent = new AndroidJavaObject("android.content.Intent");
        intent.Call<AndroidJavaObject>("setClassName", activity, ServiceClass);

        if (lat.HasValue && lng.HasValue)
        {
            // onStartCommand 會接收這些值並更新目前的假定位座標。
            intent.Call<AndroidJavaObject>("putExtra", "mock_lat", lat.Value);
            intent.Call<AndroidJavaObject>("putExtra", "mock_lng", lng.Value);

            if (radiusMeters.HasValue)
            {
                intent.Call<AndroidJavaObject>("putExtra", "mock_radius_meters", radiusMeters.Value);
            }
        }

        using var version = new AndroidJavaClass("android.os.Build$VERSION");
        int sdk = version.GetStatic<int>("SDK_INT");

        if (sdk >= 26)
        {
            // Android 8 以上必須用 foreground service 啟動 API。
            activity.Call<AndroidJavaObject>("startForegroundService", intent);
        }
        else
        {
            activity.Call<AndroidJavaObject>("startService", intent);
        }
#endif
    }

    public static void StopService()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

        using var intent = new AndroidJavaObject("android.content.Intent");
        intent.Call<AndroidJavaObject>("setClassName", activity, ServiceClass);

        activity.Call<bool>("stopService", intent);
#endif
    }
}
