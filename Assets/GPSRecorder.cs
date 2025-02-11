using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System.IO;
using System;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class GPSRecorder : MonoBehaviour
{
    // Bandera para indicar si se está grabando.
    private bool isRecording = false;

    // Lista para almacenar las líneas del CSV (incluye la cabecera).
    private List<string> csvData = new List<string>();

    // Coroutine para grabar la posición periódicamente.
    private Coroutine recordingCoroutine;

    // Intervalo de grabación en segundos.
    [SerializeField] private float recordInterval = 1f;

    // Nombre del archivo CSV.
    [SerializeField] private string fileName = "GPSData.csv";

    private void Start()
    {
        // Agrega la cabecera al CSV.
        csvData.Add("Timestamp,Latitude,Longitude,Altitude");

        // Solicita los permisos necesarios en Android.
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            Permission.RequestUserPermission(Permission.FineLocation);
        }
        // Permiso para escribir en el almacenamiento externo (para exportar el CSV).
        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
        {
            Permission.RequestUserPermission(Permission.ExternalStorageWrite);
        }
#endif
    }

    /// <summary>
    /// Llamar a este método para iniciar la grabación.
    /// </summary>
    public void StartRecording()
    {
        if (!isRecording)
        {
            StartCoroutine(StartLocationService());
        }
    }

    /// <summary>
    /// Inicia el servicio de ubicación y, de forma exitosa, comienza la grabación.
    /// </summary>
    IEnumerator StartLocationService()
    {
        // Verifica si el usuario tiene habilitada la ubicación.
        if (!Input.location.isEnabledByUser)
        {
            Debug.Log("La ubicación no está habilitada en el dispositivo.");
            yield break;
        }

        // Inicia el servicio de ubicación.
        Input.location.Start();

        // Espera a que el servicio se inicialice (máximo 20 segundos).
        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        // Si se excede el tiempo de espera.
        if (maxWait < 1)
        {
            Debug.Log("Tiempo de espera agotado al inicializar el servicio de ubicación.");
            yield break;
        }

        // Si la conexión falló.
        if (Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.Log("No se pudo determinar la ubicación.");
            yield break;
        }
        else
        {
            // Servicio iniciado correctamente; inicia la grabación.
            isRecording = true;
            recordingCoroutine = StartCoroutine(RecordLocation());
        }
    }

    /// <summary>
    /// Coroutine que registra la posición y el tiempo cada cierto intervalo.
    /// </summary>
    IEnumerator RecordLocation()
    {
        while (isRecording)
        {
            if (Input.location.status == LocationServiceStatus.Running)
            {
                // Obtiene la marca de tiempo actual.
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                // Obtiene los datos de ubicación.
                float latitude = Input.location.lastData.latitude;
                float longitude = Input.location.lastData.longitude;
                float altitude = Input.location.lastData.altitude;

                // Agrega una línea al CSV.
                string line = string.Format("{0},{1},{2},{3}", timestamp, latitude, longitude, altitude);
                csvData.Add(line);

                Debug.Log("Registrado: " + line);
            }
            else
            {
                Debug.Log("El servicio de ubicación no se encuentra activo.");
            }
            yield return new WaitForSeconds(recordInterval);
        }
    }

    /// <summary>
    /// Llamar a este método para detener la grabación.
    /// </summary>
    public void StopRecording()
    {
        if (isRecording)
        {
            isRecording = false;
            if (recordingCoroutine != null)
            {
                StopCoroutine(recordingCoroutine);
            }
            Input.location.Stop();
            Debug.Log("Grabación detenida.");
        }
    }

    // Variables para almacenar la última posición y el último tiempo registrado
    private Vector2 lastCoordinates;
    private DateTime lastTimestamp;
    private bool hasPrevious = false;

    /// <summary>
    /// Calcula la distancia en metros entre dos coordenadas geográficas usando la fórmula de Haversine.
    /// </summary>
    private double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000; // Radio de la Tierra en metros
        double dLat = (lat2 - lat1) * Mathf.Deg2Rad;
        double dLon = (lon2 - lon1) * Mathf.Deg2Rad;
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Mathf.Deg2Rad) * Math.Cos(lat2 * Mathf.Deg2Rad) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    /// <summary>
    /// Llama a este método pasándole la latitud y longitud actuales para calcular la velocidad (m/s)
    /// basándose en la distancia recorrida y el tiempo transcurrido.
    /// </summary>
    public float CalculateSpeed(float currentLatitude, float currentLongitude)
    {
        DateTime currentTimestamp = DateTime.Now;
        
        // Si es la primera lectura, se inicializan las variables y se retorna 0 m/s.
        if (!hasPrevious)
        {
            lastCoordinates = new Vector2(currentLatitude, currentLongitude);
            lastTimestamp = currentTimestamp;
            hasPrevious = true;
            return 0f;
        }
        
        // Calcula la distancia entre la última posición y la actual.
        double distance = HaversineDistance(lastCoordinates.x, lastCoordinates.y, currentLatitude, currentLongitude);
        
        // Calcula la diferencia de tiempo en segundos.
        double timeDiff = (currentTimestamp - lastTimestamp).TotalSeconds;
        
        // Actualiza la última posición y el tiempo.
        lastCoordinates = new Vector2(currentLatitude, currentLongitude);
        lastTimestamp = currentTimestamp;
        
        // Retorna la velocidad en metros por segundo.
        return timeDiff > 0 ? (float)(distance / timeDiff) : 0f;
    }


    /// <summary>
    /// Exporta el CSV guardándolo en la carpeta local y copiándolo a la carpeta de descargas en Android.
    /// </summary>
    public void ExportCSV()
    {
        // Define la ruta de guardado local.
        string filePath = Path.Combine(Application.persistentDataPath, fileName);

        // Escribe todas las líneas en el archivo CSV.
        File.WriteAllLines(filePath, csvData.ToArray());
        Debug.Log("CSV guardado localmente en: " + filePath);

#if UNITY_ANDROID && !UNITY_EDITOR
        // Ruta de la carpeta de descargas en dispositivos Android.
        string downloadsPath = Path.Combine("/storage/emulated/0/Download", fileName);

        try
        {
            File.Copy(filePath, downloadsPath, true);
            Debug.Log("CSV exportado a: " + downloadsPath);
        }
        catch (Exception e)
        {
            Debug.Log("Error exportando el CSV: " + e.Message);
        }
#else
        Debug.Log("La exportación a carpeta de descargas solo funciona en dispositivos Android.");
#endif
    }
}
