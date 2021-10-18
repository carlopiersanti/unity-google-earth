using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Matrix4x4 : Matrix
{
    public Matrix4x4() : base(4, 4) { }

    public Vector4 Row(int i)
    {
        Vector4 returnValue = new Vector4();
        returnValue.mat[0, 0] = mat[i, 0];
        returnValue.mat[1, 0] = mat[i, 1];
        returnValue.mat[2, 0] = mat[i, 2];
        returnValue.mat[3, 0] = mat[i, 3];
        return returnValue;
    }


}
