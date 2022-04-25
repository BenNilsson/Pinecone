using UnityEngine;
using Pinecone;

public partial class NetworkTransform : NetworkBehaviour
{
    [Header("Synchronization")]
    [Range(0, 1f)]
    [SerializeField] private float syncInterval = 0.2f;
    [SerializeField] private bool syncPosition = true;
    [SerializeField] private bool syncRotation = true;
    [SerializeField] private bool syncScale = false;

    [Header("Interpolation")]
    [SerializeField] private bool interpolatePosition = true;
    [SerializeField] private bool interpolateRotation = true;
    [SerializeField] private bool interpolateScale = false;

    [SerializeField] private float minimumMovementRequiredForPacket = 0.1f;
    [Tooltip("If greater than value set, do not interpolate but move directly instead. Allows for teleportation")]
    [SerializeField] private float teleportRadius = 5.0f;

    protected Transform internalTransform;

    private double _timeElapsedClient;
    private Vector3 positionLastFrame;
    private Vector3 rotationLastFrame;

    protected Vector3 _lastPosition;
    protected Vector3 _toPosition;

    protected Quaternion _toRotation;
    protected Quaternion _lastRotation;

    protected Vector3 _lastScale;
    protected Vector3 _toScale;

    private float _interpolationTime;

    [NetworkCommand]
    public void CmdClientToServerSync(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        Generated.RpcServerToClientSync(this, position, rotation, scale);

        _interpolationTime = 0.0f;

        if (syncPosition)
        {
            if (interpolatePosition)
            {
                _lastPosition = _toPosition;
                _toPosition = position;
            }
            else internalTransform.position = position;
        }
        if (syncRotation)
        {
            if (interpolateRotation)
            {
                _lastRotation = _toRotation;
                _toRotation = rotation;
            }
            else internalTransform.rotation = rotation;
        }
        if (syncScale)
        {
            if (interpolateScale)
            {
                _lastScale = _toScale;
                _toScale = scale;
            }
            else internalTransform.localScale = scale;
        }
    }

    [NetworkRPC]
    public void RpcServerToClientSync(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        if (HasAuthority || NetworkClient.IsHost)
            return;

        _interpolationTime = 0.0f;

        if (syncPosition)
        {
            if (interpolatePosition)
            {
                _lastPosition = _toPosition;
                _toPosition = position;
            }
            else internalTransform.position = position;
        }
        if (syncRotation)
        {
            if (interpolateRotation)
            {
                _lastRotation = _toRotation;
                _toRotation = rotation;
            }
            else internalTransform.rotation = rotation;
        }
        if (syncScale)
        {
            if (interpolateScale)
            {
                _lastScale = _toScale;
                _toScale = scale;
            }
            else internalTransform.localScale = scale;
        }
    }

    public override void OnStart()
    {
        internalTransform ??= transform;

        _lastPosition = internalTransform.position;
        _toPosition = _lastPosition;

        _lastRotation = internalTransform.rotation;
        _toRotation = _lastRotation;

        _lastScale = internalTransform.localScale;
        _toScale = _lastScale;
    }

    private void Update()
    {
        if (!HasAuthority)
        {
            if (internalTransform == null)
                return;

            // Linear Interpolate Movement
            if (interpolatePosition)
            {
                // If the position change is too great, simply move the object. This allows for teleportation (transform.position = Vector3)
                // without seeing an interpolation happening extremely fast.
                if (Vector3.Distance(_lastPosition, _toPosition) > teleportRadius)
                {
                    internalTransform.position = _toPosition;
                }
                else
                {
                    internalTransform.position = Vector3.Lerp(_lastPosition, _toPosition, _interpolationTime / syncInterval);
                }
            }
            if (interpolateRotation && _toRotation != Quaternion.Euler(Vector3.zero))
            {
                internalTransform.rotation = Quaternion.Lerp(_lastRotation, _toRotation, _interpolationTime / syncInterval);
            }
            if (interpolateScale)
            {
                internalTransform.localScale = Vector3.Lerp(_lastScale, _toScale, _interpolationTime / syncInterval);
            }
            _interpolationTime += Time.deltaTime;
            return;
        }

        if (_timeElapsedClient >= syncInterval)
        {
            _timeElapsedClient = 0;
            if (NetworkClient.IsConnected && (Vector3.Distance(positionLastFrame, internalTransform.position) > minimumMovementRequiredForPacket || Vector3.Distance(rotationLastFrame, internalTransform.rotation.eulerAngles) > 5f))
            {
                Generated.CmdClientToServerSync(this, internalTransform.position, internalTransform.rotation, internalTransform.localScale);
                positionLastFrame = internalTransform.position;
                rotationLastFrame = internalTransform.rotation.eulerAngles;
            }
            else if (NetworkServer.IsActive && (Vector3.Distance(positionLastFrame, internalTransform.position) > minimumMovementRequiredForPacket || Vector3.Distance(rotationLastFrame, internalTransform.rotation.eulerAngles) > 5f))
            {
                Generated.RpcServerToClientSync(this, internalTransform.position, internalTransform.rotation, internalTransform.localScale);
                positionLastFrame = internalTransform.position;
                rotationLastFrame = internalTransform.rotation.eulerAngles;
            }

        }
        _timeElapsedClient += Time.deltaTime;
    }
}
