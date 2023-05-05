using System.Collections;
using System.Collections.Generic;
using Niantic.ARDK.Utilities.Input.Legacy;
using UnityEngine;
using UnityEngine.Events;

public class LoakTapPlace : MonoBehaviour
{
    public static LoakTapPlace Instance;

    public GameObject objectToPlace;
    public Transform objectParent;
    public bool allowMultiple = true;

    public UnityEvent<GameObject> OnObjectPlaced;

    private Camera cam;
    private GameObject singleObject;

    void Awake()
    {
        Instance = this;
        enabled = false;
    }

    public void StartPlacement()
    {
        cam = Camera.main;
        enabled = true;

        if (objectToPlace.scene.name != null)
            objectToPlace.SetActive(false);

        if (!allowMultiple)
            singleObject = Instantiate(objectToPlace, objectParent);
    }

    public void EndPlacement()
    {
        enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (PlatformAgnosticInput.touchCount <= 0)
            return;

        Touch touch = PlatformAgnosticInput.GetTouch(0);

        if (touch.phase != TouchPhase.Began)
            return;

        RaycastHit[] hits = Physics.RaycastAll(cam.ScreenPointToRay(touch.position), Mathf.Infinity, 1);

        if (hits.Length <= 0)
            return;

        if (allowMultiple)
        {
            singleObject = Instantiate(objectToPlace, hits[hits.Length / 2].point, Quaternion.identity, objectParent);
            singleObject.SetActive(true);
            OnObjectPlaced.Invoke(singleObject);
        }
        else
        {
            singleObject.transform.position = hits[hits.Length / 2].point;
            singleObject.SetActive(true);
            OnObjectPlaced.Invoke(singleObject);
        }
    }
}
