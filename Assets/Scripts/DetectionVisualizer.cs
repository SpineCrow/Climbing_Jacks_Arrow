using UnityEngine;
using System.Collections.Generic;


// Renders the enemy's field-of-view cone as a dynamic mesh.
// Attach to the same GameObject as FieldOfView, and assign a MeshFilter
// with an appropriate transparent material for the cone overlay.
public class DetectionVisualizer : MonoBehaviour
{
    [Tooltip("Reference to the FieldOfView component providing detection settings")]
    public FieldOfView fov;

    [Tooltip("MeshFilter that will display the generated view cone mesh")]
    public MeshFilter viewMeshFilter;

    [Tooltip("Z-axis rotation speed for the cone (degrees per second)")]
    public float zRotationSpeed = 50f;

    private Mesh viewMesh;
    private Quaternion currentRotation;

    // Reusable list to reduce GC allocations each frame
    private readonly List<Vector3> viewPoints = new List<Vector3>();

    #region Unity Lifecycle

    private void Start()
    {
        viewMesh = new Mesh { name = "View Mesh" };
        viewMeshFilter.mesh = viewMesh;
    }

    private void LateUpdate()
    {
        DrawFieldOfView();
    }

    #endregion

    #region Mesh Generation

    
    // Rebuilds the fan-shaped view cone mesh every frame.
    // Raycasts along the cone edges to clip against obstacles.
    private void DrawFieldOfView()
    {
        float viewAngle = fov.settings.viewAngle;
        int stepCount = Mathf.RoundToInt(viewAngle * 0.1f);
        float stepAngleSize = viewAngle / stepCount;

        // Apply continuous Z rotation
        Quaternion zRotation = Quaternion.Euler(0f, 0f, zRotationSpeed * Time.deltaTime);
        currentRotation *= zRotation;
        transform.rotation = currentRotation;

        float currentZAngle = currentRotation.eulerAngles.z;

        // Collect raycast hit points along the cone
        viewPoints.Clear();
        for (int i = 0; i <= stepCount; i++)
        {
            float angle = currentZAngle - viewAngle * 0.5f + stepAngleSize * i;
            ViewCastInfo cast = ViewCast(angle);
            viewPoints.Add(cast.point);
        }

        // Build a triangle-fan mesh: origin at center, fan out to hit points
        int vertexCount = viewPoints.Count + 1;
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[(vertexCount - 2) * 3];

        vertices[0] = Vector3.zero; // Local-space origin (enemy position)

        for (int i = 0; i < viewPoints.Count; i++)
        {
            vertices[i + 1] = transform.InverseTransformPoint(viewPoints[i]);

            if (i < viewPoints.Count - 1)
            {
                int triIndex = i * 3;
                triangles[triIndex] = 0;
                triangles[triIndex + 1] = i + 1;
                triangles[triIndex + 2] = i + 2;
            }
        }

        viewMesh.Clear();
        viewMesh.vertices = vertices;
        viewMesh.triangles = triangles;
        viewMesh.RecalculateNormals();
    }

    
    // Casts a single ray at the given global angle, returning hit info or the max-range endpoint.
    private ViewCastInfo ViewCast(float globalAngle)
    {
        Vector2 direction = fov.DirFromAngle(globalAngle, true);
        float radius = fov.settings.viewRadius;

        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            direction,
            radius,
            fov.settings.obstacleMask
        );

        if (hit.collider != null)
        {
            return new ViewCastInfo(true, hit.point, hit.distance, globalAngle);
        }

        Vector3 endpoint = transform.position + (Vector3)direction * radius;
        return new ViewCastInfo(false, endpoint, radius, globalAngle);
    }

    #endregion

    #region Data Structures

    
    // Result of a single visibility raycast used for mesh construction.
    public struct ViewCastInfo
    {
        public readonly bool hit;
        public readonly Vector3 point;
        public readonly float distance;
        public readonly float angle;

        public ViewCastInfo(bool hit, Vector3 point, float distance, float angle)
        {
            this.hit = hit;
            this.point = point;
            this.distance = distance;
            this.angle = angle;
        }
    }

    #endregion
}
