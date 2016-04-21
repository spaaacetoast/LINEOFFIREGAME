﻿using UnityEngine;
using UnityEngine.UI;
public class WorldRaycaster : GraphicRaycaster
{

    [SerializeField]
    private int SortOrder = 0;

    public override int sortOrderPriority
    {
        get
        {
            return SortOrder;
        }
    }
}