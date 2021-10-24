using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class CrunchPlugin : MonoBehaviour
{
	[DllImport("crn-unity-plugin")]
	public static extern uint crn_get_decompressed_size(IntPtr src, uint src_size, uint level_index);

	[DllImport("crn-unity-plugin")]
	public static extern void crn_decompress(IntPtr src, uint src_size, IntPtr dst, uint dst_size, uint level_index);
}
