using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EnvelopeAlgorithm = EnvelopeScript.EnvelopeAlgorithm;

public class UIPresenter2D : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropdown;
    [SerializeField] private Toggle delauneyToggle;
    [SerializeField] private Toggle voronoiToggle;
    [SerializeField] private Button switchTo3d;
    [SerializeField] private Button placePointZone;

    [Header("GameObjects")] 
    [SerializeField] private GameObject pointPrefab;
    [SerializeField] private Transform pointsParent;
    [SerializeField] private EnvelopeScript envelopeScript;
    [SerializeField] private TriangulationScript triangulationScript;

    [Header("Navigation")] 
    [SerializeField] private GameObject section3d;

    private void OnEnvelopeAlgorithmSelected(int algorithm)
    {
        EnvelopeAlgorithm envelopeAlgorithm = (EnvelopeAlgorithm)algorithm;
        envelopeScript.SetAlgorithm(envelopeAlgorithm);
    }

    private void SwitchTo3DControls()
    {
        gameObject.SetActive(false);
        section3d.SetActive(true);
    }

    private void Add2DPoint()
    {
        // Solve for intersection with Z plane
        var cameraLine = Camera.main.ScreenPointToRay(Input.mousePosition);

        var p0 = Vector3.zero;
        var l0 = cameraLine.origin;
        var l = cameraLine.direction.normalized;
        var n = Vector3.back;

        var d = Vector3.Dot(p0 - l0, n) / Vector3.Dot(l, n);
        var p = l0 + l * d;
        
        // Place point at given coordinates
        var point = Instantiate(pointPrefab, pointsParent);
        point.transform.position = p;
    }

    void Start()
    {
        dropdown.onValueChanged.AddListener(OnEnvelopeAlgorithmSelected);
        delauneyToggle.onValueChanged.AddListener(t => triangulationScript.useDelauney = t);
        voronoiToggle.onValueChanged.AddListener(t => triangulationScript.voronoi = t);
        switchTo3d.onClick.AddListener(SwitchTo3DControls);
        placePointZone.onClick.AddListener(Add2DPoint);
    }
}
