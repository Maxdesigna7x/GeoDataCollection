using LeTai.TrueShadow;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class pushBottomEffect : MonoBehaviour
{
    [SerializeField]TrueShadow TS1;
    [SerializeField]TrueShadow TS2;

    [SerializeField]Image icon;
    [SerializeField]Color pushOnColor;
    [SerializeField]Color pushOffColor;

    [SerializeField]UnityEvent triggerOn;
    [SerializeField]UnityEvent triggerOff;
    [SerializeField]UnityEvent noGPS;

    [SerializeField]GPSRecorder gpsRecorder;

    bool isPush = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void PushON()
    {        
        TS1.Inset = true; // Push effect
        TS2.Inset = true; // Push effect
        icon.color = pushOnColor;   
        triggerOn.Invoke();     
    }
    void PushOFF()
    {
        TS1.Inset = false; // Push effect
        TS2.Inset = false; // Push effect
        icon.color = pushOffColor;
        triggerOff.Invoke();  
    }
    public void OnPush()
    {
        // Verifica si el usuario tiene habilitada la ubicación.
        if (!Input.location.isEnabledByUser)
        {
            Debug.Log("La ubicación no está habilitada en el dispositivo."); 
            noGPS.Invoke();
            return;
        }
        else
        {
            if (isPush)
            {
                PushOFF();
                isPush = false;
            }
            else
            {
                PushON();
                isPush = true;
            }
        }
        
    }    
}
