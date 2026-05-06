using UnityEngine;

public static class GPSMock
{
    public static bool SetLocation(double lat, double lng)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using var unityPlayer =
                new AndroidJavaClass("com.unity3d.player.UnityPlayer");

            using var activity =
                unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            using var plugin =
                new AndroidJavaObject("com.example.gpsmock.GPSMockPlugin");

            plugin.Call("setMockLocation", activity, lat, lng);
            return true;
        }
        catch (AndroidJavaException e)
        {
            if (e.Message.Contains("MOCK_LOCATION"))
            {
                // Android 拒絕 MOCK_LOCATION 時，通常代表尚未在開發人員選項中
                // 將此 App 指定為「模擬位置應用程式」。
                Debug.LogError(
                    "MOCK_LOCATION is not allowed. Select this app as the mock location app in Android developer options."
                );
            }
            else
            {
                Debug.LogException(e);
            }

            return false;
        }
#else
        return false;
#endif
    }
}
