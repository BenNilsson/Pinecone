using System.Collections.Generic;
using UnityEngine;

namespace Pinecone
{
    public partial class NetworkAnimatorBasic : NetworkBehaviour
    {
        public Animator animator;

        [Header("Synchronization")]
        [Range(0.2f, 1f)]
        [SerializeField] private float syncInterval = 0.2f;

        private double _timeElapsedClient;
        private AnimationInfo _lastSentData = new (new Dictionary<int, bool>(), new Dictionary<int, float>());

        // Send all parameters in lateupdate to ensure that other changes have been made.
        // Ideally, the animator should send the state of the actual animator and not assume that
        // users can transition through everything with basic values. This could cause some animator
        // controllers to get stuck.

        private void LateUpdate()
        {
            if (!HasAuthority)
                return;

            if (_timeElapsedClient >= syncInterval)
            {
                _timeElapsedClient = 0;
                AnimationInfo animationInfo = new AnimationInfo(new Dictionary<int, bool>(), new Dictionary<int, float>());
                
                for (int i = 0; i < animator.parameterCount; i++)
                {
                    var parameter = animator.parameters[i];
                    
                    switch(parameter.type)
                    {
                        case AnimatorControllerParameterType.Bool:
                        {
                            var value = animator.GetBool(parameter.nameHash);

                            if (!_lastSentData.bools.ContainsKey(parameter.nameHash) || _lastSentData.bools[parameter.nameHash] != value)
                            {
                                animationInfo.bools.Add(parameter.nameHash, value);
                            }
                            break;
                        }
                        case AnimatorControllerParameterType.Float:
                        {
                            var value = animator.GetFloat(parameter.nameHash);

                            if (!_lastSentData.floats.ContainsKey(parameter.nameHash) || !Mathf.Approximately(_lastSentData.floats[parameter.nameHash], value))
                            {
                                animationInfo.floats.Add(parameter.nameHash, value);
                            }
                            break;
                        }
                    }
                }

                if ((animationInfo.bools.Count > 0 || animationInfo.floats.Count > 0))
                {
                    _lastSentData = animationInfo;
                    Generated.CmdSyncAnimToServer(this, animationInfo);
                }
            }
            _timeElapsedClient += Time.deltaTime;
        }

        [NetworkCommand]
        public void CmdSyncAnimToServer(AnimationInfo animationInfo)
        {
            Generated.RpcSyncAnimToClient(this, animationInfo);

            foreach(var (key, value) in animationInfo.bools)
            {
                animator.SetBool(key, value);
            }

            foreach (var (key, value) in animationInfo.floats)
            {
                animator.SetFloat(key, value);
            }
        }

        [NetworkRPC]
        public void RpcSyncAnimToClient(AnimationInfo animationInfo)
        {
            foreach (var (key, value) in animationInfo.bools)
            {
                animator.SetBool(key, value);
            }

            foreach (var (key, value) in animationInfo.floats)
            {
                animator.SetFloat(key, value);
            }
        }
    }


    public struct AnimationInfo
    {
        public Dictionary<int, bool> bools;
        public Dictionary<int, float> floats;

        public AnimationInfo(Dictionary<int, bool> bools, Dictionary<int, float> floats)
        {
            this.bools = bools;
            this.floats = floats;
        }
    }
}