using System;
using UnityEngine;
using UnityEngine.UI;

public class UIPresenter3D : MonoBehaviour
{
    [SerializeField] private float scrollSensitivity;
    [SerializeField] private Button placePointZone;
    [SerializeField] private Button switchTo2d;

    [Header("GameObjects")] 
    [SerializeField] private Transform pointPreview;
    [SerializeField] private TetraedraPresenter tetraedra;

    [Header("Navigation")] 
    [SerializeField] private GameObject section2d;

    private float zOffset;

    private Vector3 GetAimedPosition()
    {
        // Solve for intersection with Z plane
        var cameraLine = Camera.main.ScreenPointToRay(Input.mousePosition);

        var p0 = new Vector3(0, 0, zOffset);
        var l0 = cameraLine.origin;
        var l = cameraLine.direction.normalized;
        var n = Vector3.back;

        var d = Vector3.Dot(p0 - l0, n) / Vector3.Dot(l, n);
        return l0 + l * d;
    }

    private void PlacePoint()
    {
        var p = GetAimedPosition();
        tetraedra.AddPoint(p);
    }
    
    private void SwitchTo2D()
    {
        gameObject.SetActive(false);
        section2d.SetActive(true);
    }
    
    void Start()
    {
        placePointZone.onClick.AddListener(PlacePoint);
        switchTo2d.onClick.AddListener(SwitchTo2D);
    }

    private void Update()
    {
        pointPreview.position = GetAimedPosition();

        zOffset += Input.mouseScrollDelta.y * scrollSensitivity;
    }
}
