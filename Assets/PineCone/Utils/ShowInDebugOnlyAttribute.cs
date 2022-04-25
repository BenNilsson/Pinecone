using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field)]
public class ShowInDebugOnlyAttribute : PropertyAttribute
{
    public ShowInDebugOnlyAttribute()
    {

    }
}