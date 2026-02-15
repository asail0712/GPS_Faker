using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GPSUI : MonoBehaviour
{
    public InputField latInput;
    public InputField lngInput;
    public Button inputBtn;

    private float interval = 0.8f;
    private Coroutine _co;

    private void Awake()
    {
        Application.runInBackground = true;

        inputBtn.onClick.AddListener(StartMock);
    }

    public void StartMock()
    {
        Debug.Log("Start Mock");

        if (_co != null) StopCoroutine(_co);

        double lat = double.Parse(latInput.text);
        double lng = double.Parse(lngInput.text);

        _co = StartCoroutine(Loop(lat, lng));
    }

    //public void OnSetLocation()
    //{
    //    double lat = double.Parse(latInput.text);
    //    double lng = double.Parse(lngInput.text);

    //    GPSMock.SetLocation(lat, lng);
    //}

    IEnumerator Loop(double lat, double lng)
    {
        while (true)
        {
            Debug.Log($"Set Loc {lat}, {lng}");

            GPSMock.SetLocation(lat, lng);
            yield return new WaitForSeconds(interval);
        }
    }

}
