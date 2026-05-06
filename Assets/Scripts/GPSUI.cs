using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif
using UnityEngine.UI;

public class GPSUI : MonoBehaviour
{
    private const string PostNotificationsPermission = "android.permission.POST_NOTIFICATIONS";
    private const string DefaultLatitude = "25.111821";
    private const string DefaultLongitude = "121.53331";
    private const string DefaultRadiusMeters = "50";
    private const string PrefLatitude = "gps_mock_latitude";
    private const string PrefLongitude = "gps_mock_longitude";
    private const string PrefRadiusMeters = "gps_mock_radius_meters";

    public InputField latInput;
    public InputField lngInput;
    public InputField radiusInput;
    public Button inputBtn;
    public Button saveBtn;

    private void Awake()
    {
        // 盡量讓 Unity 在平台允許時維持執行。
        // 真正的背景假定位刷新仍交給 Android service。
        Application.runInBackground = true;

#if UNITY_ANDROID && !UNITY_EDITOR
        RequestAndroidPermissions();
#endif

        EnsureRadiusInput();
        EnsureSaveButton();
        LoadSavedInputValues();

        inputBtn.onClick.AddListener(StartMock);
        saveBtn.onClick.AddListener(SaveInputValues);
    }

    public void StartMock()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!HasRequiredAndroidPermissions())
        {
            RequestAndroidPermissions();
            Debug.LogWarning("Location permission is required before starting mock location.");
            return;
        }
#endif

        Debug.Log("Start Mock");

        double lat = double.Parse(latInput.text);
        double lng = double.Parse(lngInput.text);
        double radiusMeters = double.Parse(radiusInput.text);

        // 把最新座標交給 foreground service，讓 App 切背景後仍可持續刷新。
        KeepAlive.StartService(lat, lng, radiusMeters);

        // 立即套用一次，避免要等到 service 下一次 tick 才變更位置。
        if (!GPSMock.SetLocation(lat, lng))
        {
            Debug.LogWarning("Mock location was not applied. Check Android developer options.");
        }
    }

    public void SaveInputValues()
    {
        PlayerPrefs.SetString(PrefLatitude, latInput.text);
        PlayerPrefs.SetString(PrefLongitude, lngInput.text);
        PlayerPrefs.SetString(PrefRadiusMeters, radiusInput.text);
        PlayerPrefs.Save();

        Debug.Log("Mock location input saved.");
    }

    private void LoadSavedInputValues()
    {
        latInput.text = PlayerPrefs.GetString(PrefLatitude, DefaultLatitude);
        lngInput.text = PlayerPrefs.GetString(PrefLongitude, DefaultLongitude);
        radiusInput.text = PlayerPrefs.GetString(PrefRadiusMeters, DefaultRadiusMeters);
    }

    private void EnsureRadiusInput()
    {
        if (radiusInput != null)
        {
            return;
        }

        // 場景尚未手動放半徑欄位時，先複製經度欄位產生一個可輸入半徑的 UI。
        radiusInput = Instantiate(lngInput, lngInput.transform.parent);
        radiusInput.name = "RadiusInput";
        radiusInput.text = DefaultRadiusMeters;

        RectTransform radiusRect = radiusInput.GetComponent<RectTransform>();
        RectTransform lngRect = lngInput.GetComponent<RectTransform>();
        if (radiusRect != null && lngRect != null)
        {
            radiusRect.anchoredPosition = lngRect.anchoredPosition + new Vector2(0f, -70f);
        }

        Text placeholder = radiusInput.placeholder as Text;
        if (placeholder != null)
        {
            placeholder.text = "Radius(m)";
        }

        Text text = radiusInput.textComponent;
        if (text != null)
        {
            text.text = DefaultRadiusMeters;
        }

        RectTransform buttonRect = inputBtn.GetComponent<RectTransform>();
        if (buttonRect != null)
        {
            buttonRect.anchoredPosition += new Vector2(0f, -70f);
        }
    }

    private void EnsureSaveButton()
    {
        if (saveBtn != null)
        {
            return;
        }

        // 場景尚未手動放 Save 按鈕時，先複製開始按鈕產生一個儲存按鈕。
        saveBtn = Instantiate(inputBtn, inputBtn.transform.parent);
        saveBtn.name = "SaveButton";

        RectTransform saveRect = saveBtn.GetComponent<RectTransform>();
        RectTransform inputRect = inputBtn.GetComponent<RectTransform>();
        if (saveRect != null && inputRect != null)
        {
            saveRect.anchoredPosition = inputRect.anchoredPosition + new Vector2(0f, -70f);
        }

        Text label = saveBtn.GetComponentInChildren<Text>();
        if (label != null)
        {
            label.text = "Save";
        }
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static bool HasRequiredAndroidPermissions()
    {
        return Permission.HasUserAuthorizedPermission(Permission.FineLocation);
    }

    private static void RequestAndroidPermissions()
    {
        // 注意：一般定位權限不等於 MOCK_LOCATION 權限。
        // MOCK_LOCATION 必須由使用者在 Android 開發人員選項中指定。
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            Permission.RequestUserPermission(Permission.FineLocation);
        }

        if (!Permission.HasUserAuthorizedPermission(PostNotificationsPermission))
        {
            Permission.RequestUserPermission(PostNotificationsPermission);
        }
    }
#endif
}
