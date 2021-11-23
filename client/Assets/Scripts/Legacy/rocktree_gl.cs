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
		mesh.computeBufferIndices.Dispose();
		mesh.computeBufferVertex.Dispose();
		Destroy(mesh.texture);
	}

	private int nbSimultaneous = 0;
	private int maxNbSimultaneous = 20;


	public void bufferMesh(rocktree_t.node_t.mesh_t mesh)
	{
		if (nbSimultaneous >= maxNbSimultaneous)
			return;

		mesh.buffering = true;
		nbSimultaneous++;

		mesh.computeBufferIndices = new ComputeBuffer(mesh.indices.Length, sizeof(int));
		mesh.computeBufferVertex = new ComputeBuffer(mesh.vertices.Length, vertex_t.SizeOf());

		/*mesh.computeBufferIndices.SetData(mesh.indices);
		mesh.computeBufferVertex.SetData(mesh.vertices);*/

		GCHandle pinnedData = GCHandle.Alloc(mesh.texture_Data, GCHandleType.Pinned);
		GCHandle pinnedIndices = GCHandle.Alloc(mesh.indices, GCHandleType.Pinned);
		GCHandle pinnedVertices = GCHandle.Alloc(mesh.vertices, GCHandleType.Pinned);



		if (mesh.texture_format == rocktree_t.texture_format.texture_format_rgb)
		{
			TextureLoader.Instance.LoadAsync(
				pinnedData.AddrOfPinnedObject(), mesh.texture_width, mesh.texture_height,
				mesh.computeBufferIndices.GetNativeBufferPtr(), pinnedIndices.AddrOfPinnedObject(), mesh.indices.Length * sizeof(int),
				mesh.computeBufferVertex.GetNativeBufferPtr(), pinnedVertices.AddrOfPinnedObject(), mesh.vertices.Length * vertex_t.SizeOf(),
				(texture) =>
				{
					Debug.LogError("OK");
					mesh.texture = texture.ToUnityTexture2D();
					pinnedData.Free();
					pinnedIndices.Free();
					pinnedVertices.Free();
					nbSimultaneous--;
					mesh.buffered = true;
				}, () =>
				{
					Debug.LogError("KO");

					pinnedData.Free();
					pinnedIndices.Free();
					pinnedVertices.Free();
					nbSimultaneous--;
					Debug.LogError("Load Failed : ");
				});
		}
		else if (mesh.texture_format == rocktree_t.texture_format.texture_format_dxt1)
		{
			Debug.LogError("NOT OPTIMIZED TEXTURE FORMAT, THIS COULD CAUSE LAGS");
			mesh.texture = new UnityEngine.Texture2D(mesh.texture_width, mesh.texture_height, TextureFormat.DXT1Crunched, false);
			mesh.buffered = true;
			nbSimultaneous--;
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
