using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tools
{
    public static Int16 UnpackBytes(byte left, byte right)
    {
        return (Int16)(left | (right << 8));
    }
}
