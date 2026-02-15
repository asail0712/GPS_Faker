package com.example.gpsmock;

import android.content.Context;
import android.location.Location;
import android.location.LocationManager;

public class GPSMockPlugin {

    public void setMockLocation(Context context, double lat, double lng) {
        LocationManager lm = (LocationManager) context.getSystemService(Context.LOCATION_SERVICE);
        String provider = LocationManager.GPS_PROVIDER;

        try {
            // powerUsage、accuracy 都必須在 1~3
            lm.addTestProvider(
                    provider,
                    false, false, false, false,
                    true, true, true,
                    1, 1
            );
        } catch (IllegalArgumentException ignored) {
            // provider 可能已存在，忽略即可
        }

        try {
            lm.setTestProviderEnabled(provider, true);
        } catch (SecurityException se) {
            // 沒有被選為「模擬位置應用程式」或 appops 不允許
            throw se;
        }

        Location location = new Location(provider);
        location.setLatitude(lat);
        location.setLongitude(lng);
        location.setAccuracy(1.0f);
        location.setTime(System.currentTimeMillis());

        // Android 17+ 建議也加 elapsedRealtimeNanos（有些版本更吃這個）
        location.setElapsedRealtimeNanos(System.nanoTime());

        lm.setTestProviderLocation(provider, location);
    }
}
