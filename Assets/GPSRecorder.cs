using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class GPSRecorder : MonoBehaviour
{
    // Bandera para indicar si se está grabando.
    public bool isRecording = false;

    // Lista para almacenar las líneas del CSV (incluye la cabecera).
    private List<string> csvData = new List<string>();

    // Coroutine para grabar la posición periódicamente.
    private Coroutine recordingCoroutine;

    // Intervalo de grabación en segundos.
    [SerializeField] private float recordInterval = 1f;

    // Nombre del archivo CSV.
    [SerializeField] private string fileName = "GPSData.csv";

    [SerializeField] private TextMeshProUGUI speedText;

    [SerializeField]TMP_InputField intervalInput;
    [SerializeField]TMP_InputField dataLimitImput;

    private void Start()
    {
        // Agrega la cabecera al CSV.
        csvData.Add("Timestamp,Latitude,Longitude,Velocity");
        LoadRecordCount();
        dataLimitImput.text = maxChildren.ToString();
        intervalInput.text = recordInterval.ToString();

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
        Input.location.Start(1f, 0.5f);

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

    public void SetInterval()
    {
        int Input = int.Parse(intervalInput.text);
        
        if(Input > 1)
        {
            recordInterval = Input;
            intervalInput.text = recordInterval.ToString();
        }else
        {
            intervalInput.text = "";
        }
        
    }
    public void SetDataLitit()
    {
        int Input = int.Parse(dataLimitImput.text);
        if(Input > 1)
        {
            DestroyAllChildren();
            maxChildren = Input;            
            dataLimitImput.text = maxChildren.ToString(); 
        }else
        {
            dataLimitImput.text = maxChildren.ToString(); 
        }      
    }

    public void OnPush()
    {
        if (isRecording)
        {
            StopRecording();
        }
        else
        {
            StartRecording();
        }
    }

    /// <summary>
    /// Coroutine que registra la posición y el tiempo cada cierto intervalo.
    /// </summary>
    IEnumerator RecordLocation()
    {
        // Primero, carga los datos existentes.
        LoadCSVData();
        
        while (isRecording)
        {
            if (Input.location.status == LocationServiceStatus.Running)
            {
                // Obtiene la marca de tiempo actual.
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                // Obtiene los datos de ubicación.
                float latitude = Input.location.lastData.latitude;
                float longitude = Input.location.lastData.longitude;                
                float velocity = CalculateSpeed(latitude, longitude);

                // Agrega una línea al CSV.
                string line = string.Format("{0},{1},{2},{3}", timestamp, latitude, longitude, velocity);
                csvData.Add(line);
                IncrementRecordCount();
                InstantiateRecordUI(timestamp, latitude, longitude, velocity);
                speedText.text = velocity.ToString("F2") + " m/s";

                Debug.Log("Registrado: " + line);
            }
            else
            {
                Debug.Log("El servicio de ubicación no se encuentra activo.");
            }
            yield return new WaitForSeconds(recordInterval);
        }
    }

    // Variable para contar el número de registros tomados.
    private int recordCount = 0;

    /// <summary>
    /// Incrementa el contador de registros y devuelve la cantidad total de registros tomados hasta ahora.
    /// </summary>
    public int GetRecordCount()
    {
        return recordCount;
    }

    /// <summary>
    /// Llama a este método cada vez que se registra un nuevo dato del GPS.
    /// Incrementa el contador de registros, lo guarda en PlayerPrefs y devuelve la cantidad total de registros tomados hasta ahora.
    /// </summary>
    private void IncrementRecordCount()
    {
        recordCount++;
        SaveRecordCount();
    }
    /// <summary>
    /// Guarda el número de registros en PlayerPrefs.
    /// </summary>
    private void SaveRecordCount()
    {
        PlayerPrefs.SetInt("RecordCount", recordCount);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Carga el número de registros almacenado en PlayerPrefs al iniciar.
    /// </summary>
    private void LoadRecordCount()
    {
        recordCount = PlayerPrefs.GetInt("RecordCount", 0);
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
    /// Este método carga los datos existentes del CSV en la lista 'csvData'.
    /// Si el archivo ya existe, se leen sus líneas; de lo contrario, se crea la cabecera.
    /// </summary>
    private void LoadCSVData()
    {
        string filePath = Path.Combine(Application.persistentDataPath, fileName);
        if (File.Exists(filePath))
        {
            // Carga todas las líneas existentes en csvData.
            csvData = new List<string>(File.ReadAllLines(filePath));
        }
        else
        {
            // Si no existe, inicializa la lista y agrega la cabecera.
            csvData = new List<string>();
            csvData.Add("Timestamp,Latitude,Longitude,Velocity");
        }
    }

    public void DeleteLocalCSV()
    {
        // Ruta completa del archivo CSV.
        string filePath = Path.Combine(Application.persistentDataPath, fileName);
        
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            Debug.Log("Archivo CSV eliminado de: " + filePath);
            
            // Reinicia la lista de datos con la cabecera.
            csvData = new List<string> { "Timestamp,Latitude,Longitude,Velocity" };
        }
        else
        {
            Debug.Log("No se encontró archivo CSV en: " + filePath);
        }
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
            Debug.Log("Error exportan doel CSV: " + e.Message);
        }
#else
        Debug.Log("La exportación a carpeta de descargas solo funciona en dispositivos Android.");
#endif
    }

    // Asegúrate de tener asignado en el Inspector el prefab y el contenedor donde se instanciarán los elementos.
    public GameObject recordPrefab;   // Prefab que contiene los TMP para cada valor.
    public Transform recordsParent;   // Contenedor (por ejemplo, un panel) donde se agregarán los elementos instanciados.

    /// <summary>
    /// Instancia un objeto (a partir de un prefab) y asigna los valores de la información del registro  
   
    public void InstantiateRecordUI(string timestamp, float latitude, float longitude, float velocity)
    {
        // Instanciar el prefab como hijo del contenedor asignado.
        GameObject recordInstance = Instantiate(recordPrefab, recordsParent);
        recordInstance.transform.SetAsFirstSibling(); // Para que se muestren en orden descendente.
        LimitChildren();

        // Obtener el componente RecordUI del prefab (debe tener las referencias a los TMP).
        RecordUI recordUI = recordInstance.GetComponent<RecordUI>();

        if (recordUI != null)
        {
            // Asignar los valores a cada TMP.
            recordUI.timestampTMP.text = timestamp;
            recordUI.latitudeTMP.text = latitude.ToString("F6");    // 6 decimales para mayor precisión.
            recordUI.longitudeTMP.text = longitude.ToString("F6");
            recordUI.velocityTMP.text = velocity.ToString("F2");      // 2 decimales para la altitud.
        }
        else
        {
            Debug.LogError("El prefab no contiene el componente RecordUI con las referencias a los TMP.");
        }
    }

    [SerializeField] private int maxChildren = 10;

    public void LimitChildren()
    {
        if (recordsParent == null)
        {
            Debug.LogWarning("Parent transform is not assigned!");
            return;
        }

        int childCount = recordsParent.childCount;

        if (childCount > maxChildren)
        {
            int childrenToRemove = childCount - maxChildren;

            for (int i = 0; i < childrenToRemove; i++)
            {
                Transform childToRemove = recordsParent.GetChild(recordsParent.childCount - 1);
                Destroy(childToRemove.gameObject);
            }
        }
    }
    public void DestroyAllChildren()
    {
        if (recordsParent == null)
        {
            Debug.LogWarning("Parent transform is not assigned!");
            return;
        }

        // Copia de todos los hijos en una lista temporal
        List<GameObject> children = new List<GameObject>();
        foreach (Transform child in recordsParent)
        {
            children.Add(child.gameObject);
        }

        // Itera por la lista y destruye cada objeto
        foreach (GameObject child in children)
        {
            Destroy(child);
        }
    }


    // Optional: Button to trigger the destruction in the inspector
    [ContextMenu("Destroy All Children")]
    public void DestroyAllChildrenButton()
    {
        DestroyAllChildren();
    }

}
