using UnityEngine;

public class TetraedraPresenter : MonoBehaviour
{
    [SerializeField] private MeshFilter meshFilter;
    
    private TetraedraGraph graph = TetraedraGraph.BaseCube();

    public void AddPoint(Vector3 point)
    {
        graph.AddPoint(new Point { position = point });
        meshFilter.mesh = graph.AsMesh();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        meshFilter.mesh = graph.AsMesh();
    }
}
