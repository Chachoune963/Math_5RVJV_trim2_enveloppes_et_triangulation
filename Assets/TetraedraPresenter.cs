using UnityEngine;

public class TetraedraPresenter : MonoBehaviour
{
    [SerializeField] private MeshFilter meshFilter;
    
    private TetraedraGraph graph = TetraedraGraph.BaseCube();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        graph.AddPoint(new Point { position = Vector3.up * 3.0f });
        graph.AddPoint(new Point { position = Vector3.right * 2.0f });
        graph.AddPoint(new Point { position = (Vector3.up + Vector3.right + Vector3.back).normalized * 2.0f });
        
        meshFilter.mesh = graph.AsMesh();
    }
}
