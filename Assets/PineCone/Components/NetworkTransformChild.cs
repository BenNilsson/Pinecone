using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkTransformChild : NetworkTransform
{
    [SerializeField]
    protected Transform target;

    public override void OnStart()
    {
        internalTransform = target;

        base.OnStart();
    }
}
