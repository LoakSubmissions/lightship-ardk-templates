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
    public int scanThreshold = 20;
    [SerializeField] private bool autoStart = true;

    [Header("Events")]
    public UnityEvent OnScanStart = new UnityEvent();
    public UnityEvent OnScanEnd = new UnityEvent();

    private GameObject scanCanvas;
    private TMP_Text scanText;
    private Image fillBar;
    private ARMeshManager meshMan;
    private string scanningString = "Building mesh...";
    private string completeString = "Mesh complete!";
    private float scanProgress = 0f;
    private bool scanning = false;

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

        if (autoStart)
            StartScan();
    }

    public void StartScan()
    {
        fillBar.fillAmount = 0f;
        scanText.text = scanningString;
        scanCanvas.SetActive(true);
        scanning = true;
        OnScanStart.Invoke();
    }

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

    private IEnumerator EndScan()
    {
        fillBar.fillAmount = 1f;
        scanText.text = completeString;

        yield return new WaitForSeconds(2f);

        scanCanvas.SetActive(false);
        OnScanEnd.Invoke();
    }
}
