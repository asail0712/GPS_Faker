package com.example.gpsmock;

import android.app.Notification;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.app.Service;
import android.content.Context;
import android.content.Intent;
import android.location.Location;
import android.location.LocationManager;
import android.os.Build;
import android.os.Bundle;
import android.os.Handler;
import android.os.IBinder;
import android.os.Looper;
import android.util.Log;

import java.util.Random;

import com.unity3d.player.UnityPlayer;

public class GPSMockPlugin {

    public void setMockLocation(Context context, double lat, double lng) {
        LocationManager lm = (LocationManager) context.getSystemService(Context.LOCATION_SERVICE);

        // Google Maps 在一般瀏覽時常吃 fused/network/cache 位置，
        // 導航時才更積極吃 GPS。因此 GPS 和 NETWORK 都要刷新。
        setMockLocationForProvider(lm, LocationManager.GPS_PROVIDER, lat, lng);
        setMockLocationForProvider(lm, LocationManager.NETWORK_PROVIDER, lat, lng);
    }

    private void setMockLocationForProvider(LocationManager lm, String provider, double lat, double lng) {
        try {
            // 將指定 provider 註冊成測試 provider。重複呼叫時 provider 可能已存在，
            // 因此下面會忽略已存在的例外。
            lm.addTestProvider(
                    provider,
                    false, false, false, false,
                    true, true, true,
                    1, 1
            );
        } catch (IllegalArgumentException ignored) {
            // 測試 provider 已經被加入過。
        }

        // App 必須先在開發人員選項中被指定為模擬位置應用程式。
        lm.setTestProviderEnabled(provider, true);

        Location location = new Location(provider);
        location.setLatitude(lat);
        location.setLongitude(lng);
        location.setAccuracy(1.0f);
        location.setTime(System.currentTimeMillis());

        // 給 Google Play services / fused location 更多可用訊號。
        location.setAltitude(0.0);
        location.setSpeed(0.0f);
        location.setBearing(0.0f);
        Bundle extras = new Bundle();
        extras.putInt("satellites", 12);
        location.setExtras(extras);

        // Android 17 以上注入 location 時需要 elapsedRealtimeNanos。
        location.setElapsedRealtimeNanos(System.nanoTime());

        lm.setTestProviderLocation(provider, location);
    }

    public static class KeepAliveService extends Service {

        private static final String TAG = "KeepAliveService";
        private static final String CHANNEL_ID = "keepalive_channel";
        private static final int NOTIF_ID = 1001;
        private static final String EXTRA_LAT = "mock_lat";
        private static final String EXTRA_LNG = "mock_lng";
        private static final String EXTRA_RADIUS_METERS = "mock_radius_meters";
        private static final double DEFAULT_RANDOM_RADIUS_METERS = 50.0;
        private static final double METERS_PER_DEGREE_LATITUDE = 111_320.0;

        private final Handler handler = new Handler(Looper.getMainLooper());
        private final GPSMockPlugin gpsMockPlugin = new GPSMockPlugin();
        private final Random random = new Random();
        private Runnable ticker;
        private boolean hasMockLocation;
        private double lat;
        private double lng;
        private double radiusMeters = DEFAULT_RANDOM_RADIUS_METERS;

        @Override
        public void onCreate() {
            super.onCreate();
            startForeground(NOTIF_ID, buildNotification("Service running"));

            // App 切到背景後 Unity coroutine 可能暫停，因此假定位刷新放在
            // foreground service 裡，讓它能獨立於 Unity 持續執行。
            ticker = new Runnable() {
                int count = 0;

                @Override
                public void run() {
                    count++;
                    Log.i(TAG, "Tick: " + count);

                    if (hasMockLocation) {
                        try {
                            double[] randomized = randomizeAround(lat, lng, radiusMeters);

                            // 持續重送最後一次設定的座標；有些 App 會忽略太久沒更新的
                            // mock location，所以 provider 需要定期刷新。
                            gpsMockPlugin.setMockLocation(KeepAliveService.this, randomized[0], randomized[1]);
                            Log.i(TAG, "Set mock location: " + randomized[0] + ", " + randomized[1]);
                        } catch (Exception e) {
                            Log.e(TAG, "Failed to set mock location", e);
                        }
                    }

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
            // 使用者送出新座標時，Unity 會再次呼叫 startService。
            // service 不需要重建，只要更新目標座標即可。
            if (intent != null && intent.hasExtra(EXTRA_LAT) && intent.hasExtra(EXTRA_LNG)) {
                lat = intent.getDoubleExtra(EXTRA_LAT, 0.0);
                lng = intent.getDoubleExtra(EXTRA_LNG, 0.0);
                radiusMeters = Math.max(
                        0.0,
                        intent.getDoubleExtra(EXTRA_RADIUS_METERS, DEFAULT_RANDOM_RADIUS_METERS)
                );
                hasMockLocation = true;
                Log.i(TAG, "Mock location updated: " + lat + ", " + lng + ", radius=" + radiusMeters);
            }

            // 盡量要求 Android 在 process 被回收後重建 service。
            return START_STICKY;
        }

        @Override
        public void onDestroy() {
            super.onDestroy();
            if (ticker != null) handler.removeCallbacks(ticker);
            Log.i(TAG, "Service destroyed");
        }

        private double[] randomizeAround(double centerLat, double centerLng, double radiusMeters) {
            // 用平方根修正半徑分布，避免隨機點過度集中在圓心。
            double distanceMeters = radiusMeters * Math.sqrt(random.nextDouble());
            double angle = random.nextDouble() * Math.PI * 2.0;
            double northMeters = Math.cos(angle) * distanceMeters;
            double eastMeters = Math.sin(angle) * distanceMeters;

            double latOffset = northMeters / METERS_PER_DEGREE_LATITUDE;
            double lngScale = Math.cos(Math.toRadians(centerLat));
            double lngOffset = eastMeters / (METERS_PER_DEGREE_LATITUDE * Math.max(lngScale, 0.000001));

            return new double[] {
                    centerLat + latOffset,
                    centerLng + lngOffset
            };
        }

        @Override
        public IBinder onBind(Intent intent) {
            return null;
        }

        private Notification buildNotification(String text) {
            NotificationManager nm = (NotificationManager) getSystemService(NOTIFICATION_SERVICE);

            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
                // Android 8 以上的 foreground service 通知必須建立 notification channel。
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
}
