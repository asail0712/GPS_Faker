package com.example.keepalive;

import android.app.Notification;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.app.Service;
import android.content.Intent;
import android.os.Build;
import android.os.Handler;
import android.os.IBinder;
import android.os.Looper;
import android.util.Log;

import com.unity3d.player.UnityPlayer;

public class KeepAliveService extends Service {

    private static final String TAG = "KeepAliveService";
    private static final String CHANNEL_ID = "keepalive_channel";
    private static final int NOTIF_ID = 1001;

    private final Handler handler = new Handler(Looper.getMainLooper());
    private Runnable ticker;

    @Override
    public void onCreate() {
        super.onCreate();
        startForeground(NOTIF_ID, buildNotification("Service running"));

        ticker = new Runnable() {
            int count = 0;

            @Override
            public void run() {
                count++;

                // 這裡就是你的背景工作（示範：每秒印 log）
                Log.i(TAG, "Tick: " + count);

                //（可選）回傳訊息給 Unity 場景物件
                // 你需要場景裡有個 GameObject 叫 "ServiceBridge"
                // 並且有一個方法：OnServiceTick(string msg)
                try {
                    UnityPlayer.UnitySendMessage("ServiceBridge", "OnServiceTick", "Tick: " + count);
                } catch (Exception ignored) { }

                handler.postDelayed(this, 1000);
            }
        };

        handler.post(ticker);
    }

    @Override
    public int onStartCommand(Intent intent, int flags, int startId) {
        // START_STICKY：被系統殺掉後，可能會嘗試重啟
        return START_STICKY;
    }

    @Override
    public void onDestroy() {
        super.onDestroy();
        if (ticker != null) handler.removeCallbacks(ticker);
        Log.i(TAG, "Service destroyed");
    }

    @Override
    public IBinder onBind(Intent intent) {
        return null;
    }

    private Notification buildNotification(String text) {
        NotificationManager nm = (NotificationManager) getSystemService(NOTIFICATION_SERVICE);

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            NotificationChannel ch = new NotificationChannel(
                    CHANNEL_ID,
                    "Keep Alive",
                    NotificationManager.IMPORTANCE_LOW
            );
            nm.createNotificationChannel(ch);
        }

        Notification.Builder b =
                (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O)
                        ? new Notification.Builder(this, CHANNEL_ID)
                        : new Notification.Builder(this);

        b.setContentTitle("Unity Foreground Service")
         .setContentText(text)
         .setSmallIcon(android.R.drawable.ic_menu_info_details);

        return b.build();
    }
}
