using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单圆柱水带的优化版本。
/// 使用两个 Mesh 组合出一条水带：
/// 1. BakedHoseMesh：已经固定的长水带，只在玩家走满 _bendPointInterval 时重建。
/// 2. ActiveHoseMesh：最后一小段实时水带，每帧只重建这一小段，让末端跟随玩家。
/// 这样避免路径变长后每帧重建整条 Mesh。
/// </summary>
public class SingleCylinderHoseFromFireTruck : MonoBehaviour
{
    [Header("Control Points")]
    // 消防车出水口，作为整条水带的起点。
    [SerializeField] private Transform _startPoint;

    [Header("Hose Mesh")]
    // 水带材质。
    [SerializeField] private Material _hoseMaterial;

    // 水带半径。
    [SerializeField] private float _pipeRadius = 0.05f;

    // 水带贴地高度。
    [SerializeField] private float _groundY = 0f;

    // 玩家每走出该距离，就固定一个新的 bendPoint，并把 Active 段合并进 Baked 段。
    [SerializeField] private float _bendPointInterval = 3f;

    // 已经固定下来的路径点：消防车起点 + 已完成的 bendPoint。
    private readonly List<Vector3> _bakedPathPoints = new List<Vector3>();

    // 传给 ActiveHoseMesh 的短路径：最后一个固定点 + 玩家当前位置。
    private readonly List<Vector3> _activePathPoints = new List<Vector3>();

    private Transform _playerEndPoint;
    private MultiPointBentCylinder _bakedHose;
    private MultiPointBentCylinder _activeHose;
    private Vector3 _lastBakedPointPosition;
    private bool _isDrawing;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        FirstPersonController player = other.GetComponent<FirstPersonController>();
        if (_startPoint == null || player == null || player.lineRenderEndPoint == null) return;

        _playerEndPoint = player.lineRenderEndPoint;
        _isDrawing = true;

        CreateOrResetHoses();

        Vector3 start = ToGround(_startPoint.position);
        _bakedPathPoints.Clear();
        _bakedPathPoints.Add(start);
        _lastBakedPointPosition = start;

        UpdateBakedHose();
        UpdateActiveHose();
    }

    public void StopDrawing()
    {
        _isDrawing = false;
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // 不停止生成。玩家离开消防车触发器后，水带继续跟随玩家延伸。
    }

    private void Update()
    {
        if (!_isDrawing || _playerEndPoint == null || _activeHose == null) return;

        Vector3 playerPos = ToGround(_playerEndPoint.position);

        // 玩家走满一个间隔时，把当前位置固定为新的 bendPoint。
        // 这一步才重建长的 BakedHoseMesh。
        if (Vector3.Distance(_lastBakedPointPosition, playerPos) >= _bendPointInterval)
        {
            _bakedPathPoints.Add(playerPos);
            _lastBakedPointPosition = playerPos;
            UpdateBakedHose();
        }

        // ActiveHoseMesh 只包含最后一个固定点到玩家当前位置，所以每帧重建也很轻。
        UpdateActiveHose();
    }

    private void CreateOrResetHoses()
    {
        if (_bakedHose == null)
            _bakedHose = CreateHoseObject("Runtime Baked Hose Mesh");

        if (_activeHose == null)
            _activeHose = CreateHoseObject("Runtime Active Hose Mesh");

        ConfigureHose(_bakedHose);
        ConfigureHose(_activeHose);
    }

    private MultiPointBentCylinder CreateHoseObject(string objectName)
    {
        GameObject hoseObject = new GameObject(objectName);
        hoseObject.transform.SetParent(transform, false);
        return hoseObject.AddComponent<MultiPointBentCylinder>();
    }

    private void ConfigureHose(MultiPointBentCylinder hose)
    {
        hose.SetRadius(_pipeRadius);

        MeshRenderer meshRenderer = hose.GetComponent<MeshRenderer>();
        if (_hoseMaterial != null)
            meshRenderer.sharedMaterial = _hoseMaterial;
    }

    private void UpdateBakedHose()
    {
        // 只有至少两个固定点时，长水带 Mesh 才有内容。
        // 刚开始只有消防车起点时，Baked Mesh 为空，Active Mesh 负责显示起点到玩家的短段。
        _bakedHose.SetPath(_bakedPathPoints);
    }

    private void UpdateActiveHose()
    {
        _activePathPoints.Clear();

        Vector3 activeStart = _bakedPathPoints[_bakedPathPoints.Count - 1];
        Vector3 activeEnd = ToGround(_playerEndPoint.position);

        _activePathPoints.Add(activeStart);

        if (Vector3.Distance(activeStart, activeEnd) > 0.001f)
            _activePathPoints.Add(activeEnd);

        _activeHose.SetPath(_activePathPoints);
    }

    private Vector3 ToGround(Vector3 pos)
    {
        return new Vector3(pos.x, _groundY, pos.z);
    }
}
