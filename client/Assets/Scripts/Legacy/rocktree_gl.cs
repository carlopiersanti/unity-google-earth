using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct vertex
{
	public UnityEngine.Vector3 position;
	public Vector2 octant;
	public Vector2 texCoord;

	public static int size()
    {
		return 3*sizeof(float) + 2*sizeof(float) + 2*sizeof(float);
	}
}

public class rocktree_gl : MonoBehaviour
{

	public static void unbufferMesh(rocktree_t.node_t.mesh_t mesh)
	{
		mesh.computeBufferIndices.Dispose();
		mesh.computeBufferVertex.Dispose();
		Destroy(mesh.texture);
	}



	public static void bufferMesh(rocktree_t.node_t.mesh_t mesh)
	{
		if (mesh.texture_format==rocktree_t.texture_format.texture_format_rgb)
			mesh.texture = new Texture2D(mesh.texture_width, mesh.texture_height, TextureFormat.RGB24, false);
		else if (mesh.texture_format == rocktree_t.texture_format.texture_format_dxt1)
			mesh.texture = new Texture2D(mesh.texture_width, mesh.texture_height, TextureFormat.DXT1Crunched, false);

		mesh.texture.LoadRawTextureData(mesh.texture_Data);
		mesh.texture.Apply();

		mesh.computeBufferIndices = new ComputeBuffer(mesh.indices.Length, sizeof(int));
		mesh.computeBufferVertex = new ComputeBuffer ( mesh.mesh_positions.Length, vertex.size() );

		int[] computeBufferIndices = new int[mesh.indices.Length];
		vertex[] computeBufferVertex = new vertex[mesh.mesh_positions.Length];

		for (int i = 0; i < mesh.indices.Length; i++)
			computeBufferIndices[i] = mesh.indices[i];

		for (int i = 0; i < mesh.mesh_positions.Length; i++)
        {
			computeBufferVertex[i].position = mesh.mesh_positions[i];
			computeBufferVertex[i].octant = mesh.octants[i];
			computeBufferVertex[i].texCoord = mesh.mesh_texCoords[i];
		}

		mesh.computeBufferIndices.SetData(computeBufferIndices);
		mesh.computeBufferVertex.SetData(computeBufferVertex);

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
		block.SetBuffer("indexes", mesh.computeBufferIndices);
		block.SetBuffer("vertices", mesh.computeBufferVertex);

		Graphics.DrawProcedural(material, new Bounds(UnityEngine.Vector3.zero, new UnityEngine.Vector3(float.MaxValue, float.MaxValue, float.MaxValue)), MeshTopology.Triangles, (mesh.computeBufferIndices.count - 2) * 3, 1, null, block, UnityEngine.Rendering.ShadowCastingMode.Off, false, 0);
    }

}
