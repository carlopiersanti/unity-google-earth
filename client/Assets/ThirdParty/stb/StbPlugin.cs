using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class StbPlugin : MonoBehaviour
{
    [DllImport("stb-unity-plugin")]
    public static extern IntPtr export_stbi_load_from_memory(IntPtr buffer, int len, IntPtr x, IntPtr y, IntPtr comp, int req_comp);

    [DllImport("stb-unity-plugin")]
    public static extern void export_stbi_image_free(IntPtr retval_from_stbi_load);

}
