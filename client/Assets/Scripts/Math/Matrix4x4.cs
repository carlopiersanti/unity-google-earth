using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Matrix4x4 : Matrix
{
    public Matrix4x4() : base(4, 4) { }

    public Matrix4x4(UnityEngine.Matrix4x4 input) : base(4, 4)
    {
        mat[0, 0] = input.m00;
        mat[0, 1] = input.m01;
        mat[0, 2] = input.m02;
        mat[0, 3] = input.m03;

        mat[1, 0] = input.m10;
        mat[1, 1] = input.m11;
        mat[1, 2] = input.m12;
        mat[1, 3] = input.m13;

        mat[2, 0] = input.m20;
        mat[2, 1] = input.m21;
        mat[2, 2] = input.m22;
        mat[2, 3] = input.m23;

        mat[3, 0] = input.m30;
        mat[3, 1] = input.m31;
        mat[3, 2] = input.m32;
        mat[3, 3] = input.m33;
    }


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
