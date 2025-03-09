using System;
using GaussianSplatting.Runtime;
using StartScene;
using UnityEngine;

/// <summary>
/// 用于GaussianSplatAsset存储为二进制文件的数据集
/// </summary>
[Serializable]
public class GaussianSplatAssetData
{
    /// <summary>
    /// 由于Vector3类无法序列化，因此用该类代替
    /// </summary>
    [Serializable]
    private struct Vector3Internal
    {
        private readonly float _x;
        private readonly float _y;
        private readonly float _z;

        public Vector3Internal(Vector3 vector3)
        {
            _x = vector3.x;
            _y = vector3.y;
            _z = vector3.z;
        }
        public Vector3 Get()
        {
            return new Vector3(_x, _y, _z);
        }
    }
    //基本信息
    private string _name;
    private int _formatVersion;
    private int _splatCount;
    private GaussianSplatAsset.VectorFormat _posFormat = GaussianSplatAsset.VectorFormat.Norm11;
    private GaussianSplatAsset.VectorFormat _scaleFormat = GaussianSplatAsset.VectorFormat.Norm11;
    private GaussianSplatAsset.SHFormat _shFormat = GaussianSplatAsset.SHFormat.Norm11;
    private GaussianSplatAsset.ColorFormat _colorFormat;
    private Vector3Internal _boundsMin;
    private Vector3Internal _boundsMax;
    private Hash128 _dataHash;
    private GaussianSplatAsset.CameraInfo[] cameras;

    private string _chunkPath, _positionPath, _scalePath, _colorPath, _shPath;
    private bool _useChunks;

    public GaussianSplatAssetData(GaussianSplatAsset asset,bool useChunks,string chunkPath,string positionPath,string scalePath,string colorPath,string shPath)
    {
        _name = asset.name;
        _formatVersion = asset.formatVersion;
        _splatCount = asset.splatCount;
        _posFormat = asset.posFormat;
        _scaleFormat = asset.scaleFormat;
        _shFormat = asset.shFormat;
        _colorFormat = asset.colorFormat;
        _boundsMin = new Vector3Internal(asset.boundsMin);
        _boundsMax = new Vector3Internal(asset.boundsMax);
        _dataHash = asset.dataHash;
        cameras = asset.cameras;
        
        // 外部数据路径
        _useChunks = useChunks;
        _chunkPath = chunkPath;
        _positionPath = positionPath;
        _scalePath = scalePath;
        _colorPath = colorPath;
        _shPath = shPath;
    }
    

    /// <summary>
    /// 把当前的可序列化数据转换为GaussianSplatAsset
    /// </summary>
    /// <returns>转换后的GaussianSplatAsset对象</returns>
    public GaussianSplatAsset GetAsset()
    {
        GaussianSplatAsset asset = ScriptableObject.CreateInstance<GaussianSplatAsset>();
        asset.name = _name;
        // 初始化
        asset.Initialize(_splatCount,_posFormat,_scaleFormat,_colorFormat,_shFormat,_boundsMin.Get(),_boundsMax.Get(),cameras);
        // 导入外部数据
        asset.SetAssetFiles(_useChunks ? CreateAssetManager.CreateByteAssetFromFile(_chunkPath) : null,
            CreateAssetManager.CreateByteAssetFromFile(_positionPath),
            CreateAssetManager.CreateByteAssetFromFile(_scalePath),
            CreateAssetManager.CreateByteAssetFromFile(_colorPath),
            CreateAssetManager.CreateByteAssetFromFile(_shPath));
        return asset;
    }
}

