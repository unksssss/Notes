using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 多点控制的单个弯曲圆柱体网格。
/// 整条管道始终只有一个 Mesh，不会按段创建多个圆柱体。
/// 路径点顺序为：起点、若干 bendPoint、终点。
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MultiPointBentCylinder : MonoBehaviour
{
    [Header("Mesh Settings")]
    // 圆柱半径。
    [SerializeField, Min(0.001f)] private float _radius = 0.05f;

    // 圆形截面的分段数量。
    [SerializeField, Min(3)] private int _radialSegments = 16;

    // 每两个路径点之间插值生成多少段，数值越大越平滑。
    [SerializeField, Min(1)] private int _samplesPerPathSegment = 8;

    // 是否封住管道两端。
    [SerializeField] private bool _capEnds = true;

    // 是否生成双面网格。
    [SerializeField] private bool _doubleSided = true;

    private readonly List<Vector3> _worldPathPoints = new List<Vector3>();
    private readonly List<Vector3> _sampledLocalPoints = new List<Vector3>();
    private Mesh _mesh;

    private void Awake()
    {
        CreateMesh();
    }

    /// <summary>
    /// 设置管道路径点。传入的是世界坐标，内部会转换为当前物体的本地坐标生成 Mesh。
    /// </summary>
    public void SetPath(IList<Vector3> worldPathPoints)
    {
        _worldPathPoints.Clear();

        if (worldPathPoints != null)
        {
            for (int i = 0; i < worldPathPoints.Count; i++)
                _worldPathPoints.Add(worldPathPoints[i]);
        }

        RebuildMesh();
    }

    /// <summary>
    /// 外部设置管道半径。
    /// </summary>
    public void SetRadius(float radius)
    {
        _radius = Mathf.Max(0.001f, radius);
        RebuildMesh();
    }

    /// <summary>
    /// 重新生成单个弯曲圆柱体网格。
    /// </summary>
    public void RebuildMesh()
    {
        CreateMesh();
        _mesh.Clear();

        if (_worldPathPoints.Count < 2)
            return;

        SamplePath();

        if (_sampledLocalPoints.Count < 2)
            return;

        int ringCount = _sampledLocalPoints.Count;
        int sideVertexCount = ringCount * _radialSegments;
        int capVertexCount = _capEnds ? 2 : 0;

        Vector3[] vertices = new Vector3[sideVertexCount + capVertexCount];
        Vector3[] normals = new Vector3[vertices.Length];
        Vector2[] uvs = new Vector2[vertices.Length];

        for (int i = 0; i < ringCount; i++)
        {
            Vector3 center = _sampledLocalPoints[i];
            Vector3 tangent = GetTangent(i);

            // 用世界上方向作为参考，生成当前截面的两个轴。
            Vector3 xAxis = Vector3.Cross(Vector3.up, tangent);
            if (xAxis.sqrMagnitude < 0.000001f)
                xAxis = Vector3.Cross(Vector3.right, tangent);
            xAxis.Normalize();

            Vector3 yAxis = Vector3.Cross(tangent, xAxis).normalized;

            for (int j = 0; j < _radialSegments; j++)
            {
                float angle = Mathf.PI * 2f * j / _radialSegments;
                Vector3 radial = Mathf.Cos(angle) * xAxis + Mathf.Sin(angle) * yAxis;
                int index = i * _radialSegments + j;

                vertices[index] = center + radial * _radius;
                normals[index] = radial;
                uvs[index] = new Vector2((float)j / _radialSegments, (float)i / (ringCount - 1));
            }
        }

        if (_capEnds)
        {
            int startCenterIndex = sideVertexCount;
            int endCenterIndex = sideVertexCount + 1;

            vertices[startCenterIndex] = _sampledLocalPoints[0];
            vertices[endCenterIndex] = _sampledLocalPoints[ringCount - 1];
            normals[startCenterIndex] = -GetTangent(0);
            normals[endCenterIndex] = GetTangent(ringCount - 1);
            uvs[startCenterIndex] = new Vector2(0.5f, 0.5f);
            uvs[endCenterIndex] = new Vector2(0.5f, 0.5f);
        }

        int frontTriangleIndexCount = (ringCount - 1) * _radialSegments * 6 + (_capEnds ? _radialSegments * 6 : 0);
        int[] triangles = new int[_doubleSided ? frontTriangleIndexCount * 2 : frontTriangleIndexCount];
        int tri = 0;

        for (int i = 0; i < ringCount - 1; i++)
        {
            for (int j = 0; j < _radialSegments; j++)
            {
                int nextJ = (j + 1) % _radialSegments;
                int a = i * _radialSegments + j;
                int b = i * _radialSegments + nextJ;
                int c = (i + 1) * _radialSegments + j;
                int d = (i + 1) * _radialSegments + nextJ;

                triangles[tri++] = a;
                triangles[tri++] = b;
                triangles[tri++] = c;
                triangles[tri++] = b;
                triangles[tri++] = d;
                triangles[tri++] = c;
            }
        }

        if (_capEnds)
        {
            int startCenterIndex = sideVertexCount;
            int endCenterIndex = sideVertexCount + 1;
            int lastRingStart = (ringCount - 1) * _radialSegments;

            for (int j = 0; j < _radialSegments; j++)
            {
                int nextJ = (j + 1) % _radialSegments;

                triangles[tri++] = startCenterIndex;
                triangles[tri++] = j;
                triangles[tri++] = nextJ;

                triangles[tri++] = endCenterIndex;
                triangles[tri++] = lastRingStart + nextJ;
                triangles[tri++] = lastRingStart + j;
            }
        }

        if (_doubleSided)
        {
            for (int i = 0; i < frontTriangleIndexCount; i += 3)
            {
                triangles[tri++] = triangles[i];
                triangles[tri++] = triangles[i + 2];
                triangles[tri++] = triangles[i + 1];
            }
        }

        _mesh.vertices = vertices;
        _mesh.normals = normals;
        _mesh.uv = uvs;
        _mesh.triangles = triangles;
        _mesh.RecalculateBounds();
        _mesh.RecalculateTangents();
    }

    private void SamplePath()
    {
        _sampledLocalPoints.Clear();

        for (int i = 0; i < _worldPathPoints.Count - 1; i++)
        {
            for (int sample = 0; sample < _samplesPerPathSegment; sample++)
            {
                float t = (float)sample / _samplesPerPathSegment;
                Vector3 worldPoint = GetCatmullRomPoint(i, t);
                _sampledLocalPoints.Add(transform.InverseTransformPoint(worldPoint));
            }
        }

        _sampledLocalPoints.Add(transform.InverseTransformPoint(_worldPathPoints[_worldPathPoints.Count - 1]));
    }

    private Vector3 GetCatmullRomPoint(int segmentIndex, float t)
    {
        Vector3 p0 = GetWorldPathPoint(segmentIndex - 1);
        Vector3 p1 = GetWorldPathPoint(segmentIndex);
        Vector3 p2 = GetWorldPathPoint(segmentIndex + 1);
        Vector3 p3 = GetWorldPathPoint(segmentIndex + 2);

        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            2f * p1 +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }

    private Vector3 GetWorldPathPoint(int index)
    {
        return _worldPathPoints[Mathf.Clamp(index, 0, _worldPathPoints.Count - 1)];
    }

    private Vector3 GetTangent(int sampleIndex)
    {
        Vector3 tangent;
        if (sampleIndex == 0)
            tangent = _sampledLocalPoints[1] - _sampledLocalPoints[0];
        else if (sampleIndex == _sampledLocalPoints.Count - 1)
            tangent = _sampledLocalPoints[sampleIndex] - _sampledLocalPoints[sampleIndex - 1];
        else
            tangent = _sampledLocalPoints[sampleIndex + 1] - _sampledLocalPoints[sampleIndex - 1];

        if (tangent.sqrMagnitude < 0.000001f)
            tangent = Vector3.forward;

        return tangent.normalized;
    }

    private void CreateMesh()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (_mesh == null)
        {
            _mesh = new Mesh
            {
                name = "Multi Point Bent Cylinder"
            };
        }

        meshFilter.sharedMesh = _mesh;
    }
}
