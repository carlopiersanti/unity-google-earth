using GeoGlobetrotterProtoRocktree;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class rocktree_decoder
{

	public const int MAX_LEVEL = 20;


	// unpackVarInt unpacks variable length integer from proto (like coded_stream.h)
	public static int unpackVarInt(byte[] packed, ref int index)
	{
		//auto data = (uint8_t*)packed.data();
		int size = packed.Length;
		int c = 0, d = 1, e;
		do
		{
			if (index >= size)
				throw new System.Exception("INTERNAL ERROR");
			e = packed[index];
			index++;
			c += (e & 0x7F) * d;
			d <<= 7;
		} while ((e & 0x80) != 0);
		return c;
	}


	public class vertex_t
	{
		public byte x, y, z; // position
		public byte w;       // octant mask
		public byte u, v;   // texture coordinates
	};



	// unpackVertices unpacks vertices XYZ to new 8-byte-per-vertex array
	public static vertex_t[] unpackVertices(byte[] packed)
	{
		int count = packed.Length / 3;
		vertex_t[] vertices = new vertex_t[count];
		byte x = 0, y = 0, z = 0; // 8 bit for % 0x100
		for (int i = 0; i < count; i++)
		{
			vertices[i].x = x += packed[count * 0 + i];
			vertices[i].y = y += packed[count * 1 + i];
			vertices[i].z = z += packed[count * 2 + i];
		}
		return vertices;
	}

	// unpackTexCoords unpacks texture coordinates UV to 8-byte-per-vertex-array
	public static void unpackTexCoords(byte[] packed, vertex_t[] vertices, ref Vector2 uv_offset, ref Vector2 uv_scale)
	{
		int dataIndex = 0;
		int count = vertices.Length;
		UInt16 u_mod = (UInt16)((UInt16)1 + Tools.UnpackBytes(packed[0], packed[1]));
		UInt16 v_mod = (UInt16)((UInt16)1 + Tools.UnpackBytes(packed[2], packed[3]));
		dataIndex += 4;
		int vtxIndex = 0;
		byte u = 0, v = 0;
		for (int i = 0; i < count; i++)
		{
			vertices[i].u = u = (byte)(u + Tools.UnpackBytes(packed[dataIndex + count * 0 + i],packed[dataIndex + count * 2 + i]) % u_mod);
			vertices[i].v = v = (byte)(v + Tools.UnpackBytes(packed[dataIndex + count * 1 + i],packed[dataIndex + count * 3 + i]) % v_mod);
		}

		uv_offset.x = 0.5f;
		uv_offset.y = 0.5f;
		uv_scale[0] = 1.0f / u_mod;
		uv_scale[1] = 1.0f / v_mod;
	}

	// unpackIndices unpacks indices to triangle strip
	public static UInt16[] unpackIndices(byte[] packed)
	{
		int offset = 0;

		int triangle_strip_len = unpackVarInt(packed, ref offset);
		UInt16[] triangle_strip = new UInt16[triangle_strip_len];
		int num_non_degenerate_triangles = 0;
		for (int zeros = 0, a, b = 0, c = 0, i = 0; i < triangle_strip_len; i++)
		{
			int val = unpackVarInt(packed, ref offset);
			triangle_strip[i] = (UInt16)(zeros - val);
			if (0 == val) zeros++;
		}

		return triangle_strip;
	}

	// unpackOctantMaskAndOctantCountsAndLayerBounds unpacks the octant mask for vertices (W) and layer bounds and octant counts
	public static void unpackOctantMaskAndOctantCountsAndLayerBounds(byte[] packed, UInt16[] indices, vertex_t[] vertices, int[] layer_bounds)
	{
		// todo: octant counts
		int offset = 0;
		int len = unpackVarInt(packed, ref offset);
		int idx_i = 0;
		int k = 0;
		int m = 0;

		for (int i = 0; i < len; i++)
		{
			if (0 == i % 8)
			{
				if (m >= 10)
					throw new Exception("INTERNAL ERROR");
				layer_bounds[m++] = k;
			}
			int v = unpackVarInt(packed, ref offset);
			for (int j = 0; j < v; j++)
			{
				UInt16 idx = indices[idx_i++];
				if (!(0 <= idx && idx < indices.Length))
					throw new Exception("INTERNAL ERROR");
				int vtx_i = idx;
				if (!(0 <= vtx_i && vtx_i < vertices.Length))
					throw new Exception("INTERNAL ERROR");
				(vertices)[vtx_i].w = (byte)(i & 7);
			}
			k += v;
		}

		for (; 10 > m; m++) layer_bounds[m] = k;
	}

	// unpackForNormals unpacks normals info for later mesh normals usage
	int unpackForNormals(NodeData nodeData, ref byte[] unpacked_for_normals)
	{
		Func<int, int, int> f1 = (int v, int l) =>
		{
			if (4 >= l)
				return (v << l) + (v & (1 << l) - 1);
			if (6 >= l)
			{
				int r = 8 - l;
				return (v << l) + (v << l >> r) + (v << l >> r >> r) + (v << l >> r >> r >> r);
			}
			return -(v & 1);
		};

		Func<double, int> f2 = (double c) =>
		{
			int cr = (int)Math.Round(c);
			if (cr < 0) return 0;
			if (cr > 255) return 255;
			return cr;
		};

		if (!nodeData.HasForNormals)
			throw new Exception("INTERNAL ERROR");

		var input = nodeData.ForNormals;
		var data = input.ToByteArray();
		var size = input.Length;
		if (!(size > 2))
			throw new Exception("INTERNAL ERROR");

		UInt16 count = (UInt16)((((UInt16)data[1]) << 8) | (UInt16)data[0]);
		if (!(count * 2 == size - 3))
			throw new Exception("INTERNAL ERROR");

		int s = data[2];

		int dataIndex = 3;

		byte[] output = new byte[3 * count];

		for (int i = 0; i < count; i++)
		{
			double a = f1(data[dataIndex + 0 + i], s) / 255.0;
			double f = f1(data[dataIndex + count + i], s) / 255.0;

			double b = a, c = f, g = b + c, h = b - c;
			int sign = 1;

			if (!(.5 <= g && 1.5 >= g && -.5 <= h && .5 >= h))
			{
				sign = -1;
				if (.5 >= g)
				{
					b = .5 - f;
					c = .5 - a;
				}
				else
				{
					if (1.5 <= g)
					{
						b = 1.5 - f;
						c = 1.5 - a;
					}
					else
					{
						if (-.5 >= h)
						{
							b = f - .5;
							c = a + .5;
						}
						else
						{
							b = f + .5;
							c = a - .5;
						}
					}
				}
				g = b + c;
				h = b - c;
			}

			a = Math.Min(Math.Min(2 * g - 1, 3 - 2 * g), Math.Min(2 * h + 1, 1 - 2 * h)) * sign;
			b = 2 * b - 1;
			c = 2 * c - 1;
			double m = 127 / Math.Sqrt(a * a + b * b + c * c);

			output[3 * i + 0] = (byte)f2(m * a + 127);
			output[3 * i + 1] = (byte)f2(m * b + 127);
			output[3 * i + 2] = (byte)f2(m * c + 127);
		}

		unpacked_for_normals = output;
		return 3 * count;
	}

	// unpackNormals unpacks normals indices in mesh using normal data from NodeData
	int unpackNormals(GeoGlobetrotterProtoRocktree.Mesh mesh, byte[] unpacked_for_normals, ref byte[] unpacked_normals)
	{
		var normals = mesh.Normals;
		byte[] new_normals = null;
		int count = 0;
		if (mesh.HasNormals && unpacked_for_normals != null)
		{
			count = normals.Length / 2;
			new_normals = new byte[4 * count];
			var input = normals.ToByteArray();
			for (int i = 0; i < count; ++i)
			{
				int j = input[i] + (input[count + i] << 8);
				if (!(3 * j + 2 < unpacked_for_normals.Length))
					throw new Exception("INTERNAL ERROR");
				new_normals[4 * i + 0] = unpacked_for_normals[3 * j + 0];
				new_normals[4 * i + 1] = unpacked_for_normals[3 * j + 1];
				new_normals[4 * i + 2] = unpacked_for_normals[3 * j + 2];
				new_normals[4 * i + 3] = 0;
			}
		}
		else
		{
			count = (mesh.Vertices.Length / 3) * 8;
			new_normals = new byte[4 * count];
			for (int i = 0; i < count; ++i)
			{
				new_normals[4 * i + 0] = 127;
				new_normals[4 * i + 1] = 127;
				new_normals[4 * i + 2] = 127;
				new_normals[4 * i + 3] = 0; ;
			}
		}
		unpacked_normals = new_normals;
		return 4 * count;
	}

	public class node_data_path_and_flags_t
	{
		public string path;
		public int flags;
		public int level;
	};

	static void getPathAndFlags(int path_id, ref string path, ref int level, ref int flags)
	{
		level = 1 + (path_id & 3);
		path_id >>= 2;
		StringBuilder stringBuilder = new StringBuilder(level);
		for (int i = 0; i < level; i++)
		{
			stringBuilder[i] = (char)('0' + (path_id & 7));
			path_id >>= 3;
		}
		path = stringBuilder.ToString();
		flags = path_id;
	}

	// unpackPathAndFlags unpacks path, flags and level (strlen(path)) from node metadata
	public static node_data_path_and_flags_t unpackPathAndFlags(NodeMetadata node_meta)
	{
		node_data_path_and_flags_t result = new node_data_path_and_flags_t();
		getPathAndFlags((int)node_meta.PathAndFlags, ref result.path, ref result.level, ref result.flags);
		return result;
	}

	public class OrientedBoundingBox
	{
		public Vector3 center = new Vector3();
		public Vector3 extents = new Vector3();
		public Matrix3x3 orientation = new Matrix3x3();
	};

	public static OrientedBoundingBox unpackObb(byte[] packed, Vector3 head_node_center, float meters_per_texel)
	{
		if (!(packed.Length == 15))
			throw new Exception("INTERNAL ERROR");

		byte[] data = packed;

		OrientedBoundingBox obb = new OrientedBoundingBox();
		obb.center.mat[0,0] = Tools.UnpackBytes(data[0], data[1]) * meters_per_texel + head_node_center.mat[0,0];
		obb.center.mat[1,0] = Tools.UnpackBytes(data[2], data[3]) * meters_per_texel + head_node_center.mat[1,0];
		obb.center.mat[2,0] = Tools.UnpackBytes(data[4], data[5]) * meters_per_texel + head_node_center.mat[2,0];
		obb.extents.mat[0,0] = data[6] * meters_per_texel;
		obb.extents.mat[1,0] = data[7] * meters_per_texel;
		obb.extents.mat[2,0] = data[8] * meters_per_texel;
		UnityEngine.Vector3 euler;
		euler.x = (float)(Tools.UnpackBytes(data[9], data[10]) * Math.PI / 32768.0f);
		euler.y = (float)(Tools.UnpackBytes(data[11], data[12]) * Math.PI / 65536.0f);
		euler.z = (float)(Tools.UnpackBytes(data[13], data[14]) * Math.PI / 32768.0f);
		double c0 = Mathf.Cos(euler.x);
		double s0 = Mathf.Sin(euler.x);
		double c1 = Mathf.Cos(euler.y);
		double s1 = Mathf.Sin(euler.y);
		double c2 = Mathf.Cos(euler.z);
		double s2 = Mathf.Sin(euler.z);
		var orientation = obb.orientation.mat;
		orientation[0,0] = c0 * c2 - c1 * s0 * s2;
		orientation[0,1] = c1 * c0 * s2 + c2 * s0;
		orientation[0,2] = s2 * s1;
		orientation[1,0] = -c0 * s2 - c2 * c1 * s0;
		orientation[1,1] = c0 * c1 * c2 - s0 * s2;
		orientation[1,2] = c2 * s1;
		orientation[2,0] = s1 * s0;
		orientation[2,1] = -c0 * s1;
		orientation[2,2] = c1;

		return obb;
	}
}
