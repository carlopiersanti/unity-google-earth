using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tools
{
    public static UInt16 UnpackBytes(byte left, byte right)
    {
        return (UInt16)(left | right << 8);
    }
}
