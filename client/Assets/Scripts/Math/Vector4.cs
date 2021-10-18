using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vector4 : Matrix
{
    public Vector4() : base(4, 1) { }

    public static Vector4 operator +(Vector4 m1, Vector4 m2)
    {
        Matrix r = Add(m1, m2);
        Vector4 returnValue = new Vector4();
        returnValue.mat = r.mat;
        return returnValue;
    }

    public static Vector4 operator -(Vector4 m1, Vector4 m2)
    {
        return m1 + (-m2);
    }

    public static Vector4 operator -(Vector4 m)
    {
        Matrix r = Matrix.Multiply(-1, m);
        Vector4 returnValue = new Vector4();
        returnValue.mat = r.mat;
        return returnValue;
    }
}