using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class rocktree_gl : MonoBehaviour
{

	public static void unbufferMesh(rocktree_t.node_t.mesh_t mesh)
	{
		Destroy(mesh.mesh);
		Destroy(mesh.texture);
	}

	public static void bufferMesh(rocktree_t.node_t.mesh_t mesh)
	{
		if (mesh.texture_format==rocktree_t.texture_format.texture_format_rgb)
			mesh.texture = new Texture2D(mesh.texture_width, mesh.texture_height, TextureFormat.RGB24, false);
		else if (mesh.texture_format == rocktree_t.texture_format.texture_format_dxt1)
			mesh.texture = new Texture2D(mesh.texture_width, mesh.texture_height, TextureFormat.DXT1, false);

		mesh.texture.LoadRawTextureData(mesh.texture_Data);
		mesh.texture.Apply();

		mesh.mesh = new Mesh();
		mesh.mesh.vertices = mesh.mesh_positions;
		mesh.mesh.triangles = mesh.indices;
		mesh.mesh.uv = mesh.octants;
		mesh.mesh.uv2 = mesh.mesh_texCoords;
		mesh.mesh.bounds = new Bounds(UnityEngine.Vector3.zero, new UnityEngine.Vector3(float.MaxValue, float.MaxValue, float.MaxValue) );

		mesh.buffered = true;
	}

	public static void bindAndDrawMesh(Camera camera, Material material, rocktree_t.node_t.mesh_t mesh, UnityEngine.Matrix4x4 transform_float, byte mask_map)
    {
		MaterialPropertyBlock block = new MaterialPropertyBlock();
		block.SetMatrix("transform", transform_float);
		block.SetFloatArray("octant_mask", new List<float> { mask_map & 0b00000001, mask_map & 0b00000010, mask_map & 0b00000100, mask_map & 0b00001000, mask_map & 0b00010000, mask_map & 0b00100000, mask_map & 0b01000000, mask_map & 0b10000000 });
		block.SetFloat("uv_offset_x", mesh.uv_offset.x);
		block.SetFloat("uv_offset_y", mesh.uv_offset.y);
		block.SetFloat("uv_scale_x", mesh.uv_scale.x);
		block.SetFloat("uv_scale_y", mesh.uv_scale.y);
		block.SetTexture("maptexture", mesh.texture);
		Graphics.DrawMesh(mesh.mesh, camera.cameraToWorldMatrix, material, 0, null, 0, block, false, false, false);
    }

}
