using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Niantic.ARDK.Extensions.Meshing;

public class LoakScanner : MonoBehaviour
{
    [Header("Config")]
    [Tooltip("Number of mesh blocks required before scanning is complete.")]
    public int scanThreshold = 20;
    [Tooltip("True if the scan should begin immediately on Start. Set to false if you wish to start it manually.")]
    [SerializeField] private bool autoStart = true;

    [Header("Events")]
    [Tooltip("Invoked when the scan initially starts and the UI turns on.")]
    public UnityEvent OnScanStart = new UnityEvent();
    [Tooltip("Invoked when the scan finishes and the UI turns off.")]
    public UnityEvent OnScanEnd = new UnityEvent();

    private GameObject scanCanvas;
    private TMP_Text scanText;
    private Image fillBar;
    private ARMeshManager meshMan;
    private string scanningString = "Building mesh...";
    private string completeString = "Mesh complete!";
    private float scanProgress = 0f;
    private bool scanning = false;

    // Sets up all private reference variables and begins scan if auto start enabled.
    void Start()
    {
        scanCanvas = transform.GetComponentInChildren<Canvas>(true).gameObject;
        scanText = scanCanvas.GetComponentInChildren<TMP_Text>(true);
        fillBar = scanCanvas.GetComponentsInChildren<Image>(true)[1];
        meshMan = FindObjectOfType<ARMeshManager>(true);

        if (meshMan == null)
        {
            Debug.LogError("Loak Scanner requires an ARMeshManager in the scene. Please add one or remove the Loak Scanner.");
            enabled = false;
            return;
        }

        meshMan.GenerateUnityMeshes = true;

        if (autoStart)
            StartScan();
    }

    // Resets UI and begins the scan.
    public void StartScan()
    {
        fillBar.fillAmount = 0f;
        scanText.text = scanningString;
        scanCanvas.SetActive(true);
        scanning = true;
        OnScanStart.Invoke();
    }

    // Updates the UI based on scan progress. Ends the scan if progress bar full.
    void Update()
    {
        if (!scanning)
            return;
        
        scanProgress = Mathf.Min((float)meshMan.MeshRoot.transform.childCount / scanThreshold, 1f);
        fillBar.fillAmount = Mathf.Max(scanProgress, fillBar.fillAmount);

        if (scanProgress >= 1f)
        {
            StartCoroutine(EndScan());
        }
    }

    // Call if you wish to end the scan early. Set argument to true if you want to skip the complete delay.
    public void ForceEndScan(bool immediate)
    {
        if (immediate)
        {
            scanCanvas.SetActive(false);
            OnScanEnd.Invoke();
        }
        else
        {
            StartCoroutine(EndScan());
        }
    }

    // Sets UI to complete values, delays, then turns off the UI.
    private IEnumerator EndScan()
    {
        fillBar.fillAmount = 1f;
        scanText.text = completeString;

        yield return new WaitForSeconds(2f);

        scanCanvas.SetActive(false);
        OnScanEnd.Invoke();
    }
}
