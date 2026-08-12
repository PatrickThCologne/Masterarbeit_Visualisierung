using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.InputSystem;
using Vuforia;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class BohrhakenRaycastTool : MonoBehaviour
{
    [Header("Im Inspector zuweisen")]
    public Camera arCamera;
    public LayerMask wandLayer;
    public SplineContainer splineBohrhaken12;
    public SplineContainer splineBohrhaken3;
    public VuforiaBehaviour vuforiaBehaviour;
    public string dateiName = "bohrhaken_3d_messungen.csv";

    private Dictionary<string, string> messungen = new Dictionary<string, string>();
    private int aktuellerBohrhaken = 1;
    private int sekundenZaehler = 0;
    private bool springtGerade = false;

    void Start()
    {
        vuforiaBehaviour.enabled = false; // startet pausiert
        Debug.Log("Bereit. [Leertaste]=1 Sekunde weiter  [1][2][3]=Bohrhaken  [Linksklick]=Messen  [K]=CSV speichern");
    }

    void Update()
    {
        var kb = Keyboard.current;
        var maus = Mouse.current;
        if (kb == null || maus == null) return;

        if (kb.spaceKey.wasPressedThisFrame && !springtGerade)
            StartCoroutine(SpringeEineSekunde());

        if (kb.digit1Key.wasPressedThisFrame) aktuellerBohrhaken = 1;
        if (kb.digit2Key.wasPressedThisFrame) aktuellerBohrhaken = 2;
        if (kb.digit3Key.wasPressedThisFrame) aktuellerBohrhaken = 3;

        if (maus.leftButton.wasPressedThisFrame)
            Messen(maus.position.ReadValue());

        if (kb.kKey.wasPressedThisFrame)
            SchreibeCSV();
    }

    IEnumerator SpringeEineSekunde()
    {
        springtGerade = true;
        vuforiaBehaviour.enabled = true;
        yield return new WaitForSecondsRealtime(1f);
        vuforiaBehaviour.enabled = false;
        sekundenZaehler++;
        springtGerade = false;
        Debug.Log($"--- Sekunde {sekundenZaehler} erreicht, pausiert ---");
    }

    void Messen(Vector2 mausPosition)
    {
        Ray ray = arCamera.ScreenPointToRay(new Vector3(mausPosition.x, mausPosition.y, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, wandLayer, QueryTriggerInteraction.Collide))
        {
            Vector3 p = hit.point;
            SplineContainer relevanterSpline = (aktuellerBohrhaken == 3) ? splineBohrhaken3 : splineBohrhaken12;
            float abstand = AbstandZumNaechstenKnoten(p, relevanterSpline);

            string key = $"{sekundenZaehler}_{aktuellerBohrhaken}";
            string zeile = $"{sekundenZaehler};{aktuellerBohrhaken};{p.x:F4};{p.y:F4};{p.z:F4};{abstand:F4}";

            if (messungen.ContainsKey(key))
                Debug.Log($"[Sekunde {sekundenZaehler}] Bohrhaken {aktuellerBohrhaken}: vorherige Messung ersetzt, neuer Abstand = {abstand:F4}");
            else
                Debug.Log($"[Sekunde {sekundenZaehler}] Bohrhaken {aktuellerBohrhaken}: Abstand = {abstand:F4}");

            messungen[key] = zeile;
        }
        else
        {
            Debug.LogWarning("Kein Treffer - daneben geklickt, oder MeshCollider fehlt.");
        }
    }

    float AbstandZumNaechstenKnoten(Vector3 bohrhakenPos, SplineContainer splineContainer)
    {
        float minAbstand = float.MaxValue;
        Spline spline = splineContainer.Spline;
        for (int i = 0; i < spline.Count; i++)
        {
            Vector3 knotenWelt = splineContainer.transform.TransformPoint((Vector3)spline[i].Position);
            float abstand = Vector3.Distance(bohrhakenPos, knotenWelt);
            if (abstand < minAbstand) minAbstand = abstand;
        }
        return minAbstand;
    }

    void SchreibeCSV()
    {
        var geordnet = messungen
            .OrderBy(kvp => int.Parse(kvp.Key.Split('_')[0]))
            .ThenBy(kvp => int.Parse(kvp.Key.Split('_')[1]))
            .Select(kvp => kvp.Value);

        List<string> zeilen = new List<string> { "Sekunde;Bohrhaken;PosX;PosY;PosZ;AbstandZumNaechstenKnoten" };
        zeilen.AddRange(geordnet);

        string pfad = Path.Combine(Application.persistentDataPath, dateiName);
        File.WriteAllLines(pfad, zeilen);
        Debug.Log($"Gespeichert unter: {pfad}");
    }

#if UNITY_EDITOR
    [ContextMenu("CSV neu berechnen (Abstand zur Kurve statt zum Knoten)")]
    void CsvNeuBerechnenAbstandZurKurve()
    {
        string inputPfad = EditorUtility.OpenFilePanel("CSV mit Rohdaten wählen", Application.persistentDataPath, "csv");
        if (string.IsNullOrEmpty(inputPfad)) return;

        var zeilenEin = File.ReadAllLines(inputPfad);
        var zeilenAus = new List<string> { "Sekunde;Bohrhaken;PosX;PosY;PosZ;AbstandZurKurve" };

        for (int i = 1; i < zeilenEin.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(zeilenEin[i])) continue;
            var teile = zeilenEin[i].Split(';');

            int sekunde = int.Parse(teile[0]);
            int bohrhaken = int.Parse(teile[1]);
            float x = float.Parse(teile[2].Replace(',', '.'), CultureInfo.InvariantCulture);
            float y = float.Parse(teile[3].Replace(',', '.'), CultureInfo.InvariantCulture);
            float z = float.Parse(teile[4].Replace(',', '.'), CultureInfo.InvariantCulture);
            Vector3 pos = new Vector3(x, y, z);

            SplineContainer relevanterSpline = (bohrhaken == 3) ? splineBohrhaken3 : splineBohrhaken12;

            if (i == 1)
            {
                Debug.Log($"[DEBUG] Query Weltposition: {pos}");
                Spline s = relevanterSpline.Spline;
                for (int k = 0; k < s.Count; k++)
                {
                    Vector3 knotWelt = relevanterSpline.transform.TransformPoint((Vector3)s[k].Position);
                    Debug.Log($"[DEBUG] Knoten {k} Weltposition: {knotWelt}");
                }
                for (int sIdx = 0; sIdx <= 4; sIdx++)
                {
                    float tDbg = sIdx / 4f;
                    Vector3 kurvenPunktWelt = relevanterSpline.EvaluatePosition(tDbg);
                    Debug.Log($"[DEBUG] Kurve t={tDbg:F2} Weltposition: {kurvenPunktWelt}");
                }
            }

            float abstand = AbstandZurKurve(pos, relevanterSpline);

            if (teile.Length > 5 && float.TryParse(teile[5].Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out float abstandKnotenAlt))
            {
                if (abstand > abstandKnotenAlt + 0.001f)
                    Debug.LogWarning($"Unplausibel: Sekunde {sekunde}, Bohrhaken {bohrhaken}: Kurve ({abstand:F4}) > Knoten ({abstandKnotenAlt:F4}) - das darf rechnerisch nicht sein, Ergebnis pruefen!");
            }

            zeilenAus.Add($"{sekunde};{bohrhaken};{x:F4};{y:F4};{z:F4};{abstand:F4}");
        }

        string ausgabePfad = inputPfad.Substring(0, inputPfad.Length - 4) + "_kurve.csv";
        File.WriteAllLines(ausgabePfad, zeilenAus);
        Debug.Log($"Neu berechnet ({zeilenAus.Count - 1} Zeilen). Gespeichert unter: {ausgabePfad}");
    }

    float AbstandZurKurve(Vector3 weltPos, SplineContainer splineContainer)
    {
        float minAbstand = float.MaxValue;
        const int samples = 2000;

        for (int i = 0; i <= samples; i++)
        {
            float t = (float)i / samples;
            Vector3 punktWelt = splineContainer.EvaluatePosition(t);
            float abstand = Vector3.Distance(weltPos, punktWelt);
            if (abstand < minAbstand) minAbstand = abstand;
        }

        return minAbstand;
    }
#endif
}