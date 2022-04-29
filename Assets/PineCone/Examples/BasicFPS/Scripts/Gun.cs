using System;
using System.Linq;
using UnityEngine;

namespace Pinecone.Examples.BasicFPS
{
    public partial class Gun : NetworkBehaviour
    {
        [SerializeField] private int damage = 100;
        [SerializeField] private PlayerController controller;
        [SerializeField] private Player player;
        [SerializeField] private float shootingDistance = 30.0f;
        [SerializeField] private LayerMask shootingMask;
        [SerializeField] private Transform shootLocation;
        [SerializeField] private Vector3 localGunPosition = new Vector3(0, -0.5f, 0);
        [SerializeField] private Transform gun;
        [SerializeField] private GameObject laserRay;
        [SerializeField] private Renderer[] renderers;

        [SerializeField] private float cooldown = 1.0f;
        [SerializeField] private Renderer gunRenderer;
        public Color32 gunReadyColor;
        public Color32 cooldownColor;
        [SerializeField] private bool IsServerAuthoritative = false;
        private Material gunMat;

        [NetworkSync]
        private double timeSinceLastShotServer;
        private float timeSinceLastShot;

        public override void OnStart()
        {
            gunMat = gunRenderer.materials[1];
            gunMat.color = gunReadyColor;
            if (HasAuthority)
                gun.transform.localPosition = localGunPosition;
        }

        private double GetUnixTime()
        {
            var t = DateTime.UtcNow - new DateTime(1970, 1, 1);
            return t.TotalSeconds;
        }

        private void Update()
        {
            if (!HasAuthority)
            {
                gunMat.color = timeSinceLastShotServer + cooldown < GetUnixTime() ? gunReadyColor : cooldownColor;
                return;
            }

            timeSinceLastShot -= Time.deltaTime;

            if (timeSinceLastShot > 0)
                return;
            else
                gunMat.color = gunReadyColor;

            if (Input.GetMouseButtonDown(0))
            {
                timeSinceLastShot = cooldown;
                gunMat.color = cooldownColor;
                if (IsServerAuthoritative)
                    Generated.PlayerShotBullet(this, shootLocation.position, controller.PlayerCamera.transform.forward);
                else
                    PlayerShotBulletInternal(shootLocation.position, controller.PlayerCamera.transform.forward);
            }
        }

        public void SetRendererEnabled(bool enable)
        {
            foreach (var renderer in renderers)
            {
                renderer.enabled = enable;
            }
        }

        [NetworkCommand]
        public void PlayerShotBullet(Vector3 cameraPos, Vector3 forwardVector)
        {
            Vector3 endPosition = cameraPos + forwardVector * shootingDistance;
            if (Physics.Raycast(cameraPos, forwardVector, out var hit, shootingDistance, shootingMask))
            {

#if UNITY_EDITOR
                Debug.DrawLine(cameraPos, hit.point, Color.red, 0.5f);
#endif
                // Check name to avoid creating tags for examples.

                var hitCollider = hit.collider.gameObject;
                var networkObject = GetComponent<NetworkObject>();
                var objectId = networkObject.NetworkObjectID;
                if (hitCollider.name.Contains("FpsPlayer") &&
                    hitCollider.GetComponent<NetworkObject>().NetworkObjectID !=
                    networkObject.NetworkObjectID)
                {
                    endPosition = hit.point;
                    Generated.TargetRPCSetupLineRenderer(this, endPosition);
                    hitCollider.GetComponent<Player>().ServerPlayerHit(damage, networkObject);
                    return;
                }
            }

            Generated.TargetRPCSetupLineRenderer(this, endPosition);
        }

        public void PlayerShotBulletInternal(Vector3 cameraPos, Vector3 forwardVector)
        {
            timeSinceLastShotServerGenerated = GetUnixTime();
            var ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
            Vector3 endPosition = ray.origin + (ray.direction * shootingDistance);

            if (Physics.Raycast(ray, out var hit, shootingDistance, shootingMask))
            {
#if UNITY_EDITOR
                Debug.DrawLine(ray.origin, hit.point, Color.red, 0.5f);
#endif
                var hitCollider = hit.collider.gameObject;
                if (hitCollider.name.Contains("FpsPlayer") &&
                    hitCollider.GetComponent<NetworkObject>().NetworkObjectID !=
                    GetComponent<NetworkObject>().NetworkObjectID)
                {
                    Generated.HitPlayerClient(this, hit.point, hitCollider.GetComponent<NetworkObject>().NetworkObjectID);
                    return;
                }

                endPosition = hit.point;
            }

            Generated.HitPlayerClient(this, endPosition, string.Empty);
        }

        [NetworkCommand]
        private void HitPlayerClient(Vector3 endPosition, string networkObjectId = "")
        {
            Generated.TargetRPCSetupLineRenderer(this, endPosition);

            if (!string.IsNullOrWhiteSpace(networkObjectId))
            {
                NetworkObject networkObject = FindObjectsOfType<NetworkObject>().FirstOrDefault(x => x.NetworkObjectID == networkObjectId);
                if (networkObject != null)
                {
                    networkObject.GetComponent<Player>().ServerPlayerHit(damage, GetComponent<NetworkObject>());
                }
            }

            timeSinceLastShotServerGenerated = GetUnixTime();
        }

        [NetworkRPC]
        private void TargetRPCSetupLineRenderer(Vector3 endPos)
        {
            Vector3 startPos = gun.transform.position;
            GameObject go = Instantiate(laserRay, startPos, Quaternion.identity);
            LineRenderer lineRenderer = go.GetComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.startColor = player.playerColor.color;
            lineRenderer.endColor = player.playerColor.color;
            lineRenderer.SetPosition(0, startPos);
            lineRenderer.SetPosition(1, endPos);
            lineRenderer.enabled = true;
            Destroy(go, 0.2f);
        }
    }
}
