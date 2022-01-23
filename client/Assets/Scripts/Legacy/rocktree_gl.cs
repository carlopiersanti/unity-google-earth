using Inking;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using static rocktree_t.node_t.mesh_t;

public class rocktree_gl : MonoBehaviour
{

	public void unbufferMesh(rocktree_t.node_t.mesh_t mesh)
	{
		if (meshToBuffer.Contains(mesh))
			meshToBuffer.Remove(mesh);
		if (mesh.computeBufferIndices!=null) mesh.computeBufferIndices.Dispose();
		if (mesh.computeBufferVertex!=null) mesh.computeBufferVertex.Dispose();
		if (mesh.texture!=null) Destroy(mesh.texture);
	}

	private int nbSimultaneous = 0;
	private int maxNbSimultaneous = 5;

	public List<rocktree_t.node_t.mesh_t> meshToBuffer = new List<rocktree_t.node_t.mesh_t>();



	public void bufferMesh(string path, rocktree_t.node_t.mesh_t mesh)
	{
		lock (meshToBuffer)
        {
			if (!meshToBuffer.Contains(mesh))
            {
				mesh.path = path;
				meshToBuffer.Insert(0, mesh);
			}
		}
	}

    private void Update()
    {
		//while (nbSimultaneous < maxNbSimultaneous)
		while (true)
		{
			rocktree_t.node_t.mesh_t mesh;

			lock (meshToBuffer)
			{
				if (meshToBuffer.Count == 0)
					return;

				mesh = meshToBuffer[0];

				for (int i = 1; i < meshToBuffer.Count; i++)
					mesh = mesh.path.Length <= meshToBuffer[i].path.Length ? mesh : meshToBuffer[i];

				meshToBuffer.Remove(mesh);
			}

			mesh.buffering = true;
			nbSimultaneous++;

			mesh.computeBufferIndices = new ComputeBuffer(mesh.indices.Length, sizeof(int));
			mesh.computeBufferVertex = new ComputeBuffer(mesh.vertices.Length, vertex_t.SizeOf());

			mesh.computeBufferIndices.SetData(mesh.indices);
			mesh.computeBufferVertex.SetData(mesh.vertices);

			if (mesh.texture_format == rocktree_t.texture_format.texture_format_rgb)
			{
				mesh.texture = new UnityEngine.Texture2D(mesh.texture_width, mesh.texture_height, TextureFormat.RGBA32, false);
				mesh.texture.LoadRawTextureData(mesh.texture_Data);
				mesh.texture.Apply();
				mesh.buffered = true;
				nbSimultaneous--;
			}
			else if (mesh.texture_format == rocktree_t.texture_format.texture_format_dxt1)
			{
				Debug.LogError("NOT OPTIMIZED TEXTURE FORMAT, THIS COULD CAUSE LAGS");
				mesh.texture = new UnityEngine.Texture2D(mesh.texture_width, mesh.texture_height, TextureFormat.DXT1Crunched, false);
				mesh.texture.LoadRawTextureData(mesh.texture_Data);
				mesh.texture.Apply();
				mesh.buffered = true;
				nbSimultaneous--;
			}
		}

	}


	public void bindAndDrawMesh(Camera camera, Material material, rocktree_t.node_t.mesh_t mesh, UnityEngine.Matrix4x4 transform_float, byte mask_map)
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
