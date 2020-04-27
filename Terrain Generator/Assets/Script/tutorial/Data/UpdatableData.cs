﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdatableData : ScriptableObject
{
    public event System.Action OnValueUpdated;
    public bool autoUpdate;
    /*to avoid only the onValidate method being called in NoiseData class add protected virtual in front,
      and add protected override for the onValidate method in NoiseData class so that the method is called correctly*/
    protected virtual void OnValidate()
    {
        if (autoUpdate)
        {
            NotifiedOfUpdateValue();
        }
    }

    public void NotifiedOfUpdateValue()
    {
        if(OnValueUpdated != null)
        {
            OnValueUpdated();
        }
    }
}
