using System.IO;
using UnityEngine;
using Vuforia;

// muss auf dem Model-Target-Objekt sitzen, gleiche Position wie die Route
[RequireComponent(typeof(ObserverBehaviour))]
public class TrackingLogger : MonoBehaviour
{
    private ObserverBehaviour observerBehaviour;
    private StreamWriter writer;
    private string filePath;

    void Start()
    {
        observerBehaviour = GetComponent<ObserverBehaviour>();

        filePath = Path.Combine(Application.persistentDataPath, "tracking_log.csv");
        writer = new StreamWriter(filePath, false);
        writer.WriteLine("frame,zeit_s,status,status_info,pos_x,pos_y,pos_z,rot_x,rot_y,rot_z,rot_w");
        writer.Flush();

        Debug.Log("TrackingLogger schreibt nach: " + filePath);
    }

    void Update()
    {
        if (observerBehaviour == null || writer == null) return;

        var status = observerBehaviour.TargetStatus.Status;
        var statusInfo = observerBehaviour.TargetStatus.StatusInfo;

        Vector3 pos = transform.position;
        Quaternion rot = transform.rotation;

        writer.WriteLine(string.Format(
            "{0},{1:F4},{2},{3},{4:F6},{5:F6},{6:F6},{7:F6},{8:F6},{9:F6},{10:F6}",
            Time.frameCount,
            Time.time,
            status,
            statusInfo,
            pos.x, pos.y, pos.z,
            rot.x, rot.y, rot.z, rot.w
        ));

        // Sofort flushen, damit bei einem Absturz/Stop mitten in der Session
        // moeglichst wenig verloren geht.
        writer.Flush();
    }

    void OnApplicationQuit()
    {
        CloseWriter();
    }

    void OnDestroy()
    {
        CloseWriter();
    }

    private void CloseWriter()
    {
        if (writer != null)
        {
            writer.Close();
            writer = null;
        }
    }
}