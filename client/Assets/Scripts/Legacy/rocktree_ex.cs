using GeoGlobetrotterProtoRocktree;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using static rocktree_t;

public class rocktree_ex
{
	public static void populateBulk(bulk_t bulk, BulkMetadata bulk_metadata)
	{
		bulk._metadata = bulk_metadata;

		bulk.head_node_center = new UnityEngine.Vector3();
		bulk.head_node_center.x = (float)bulk._metadata.HeadNodeCenter[0];
		bulk.head_node_center.y = (float)bulk._metadata.HeadNodeCenter[1];
		bulk.head_node_center.z = (float)bulk._metadata.HeadNodeCenter[2];

		foreach (var node_meta in bulk._metadata.NodeMetadata )
		{
			var aux = rocktree_decoder.unpackPathAndFlags(node_meta);
			var has_data = (aux.flags & (int)NodeMetadata.Types.Flags.Nodata) == 0;
			var has_bulk = aux.path.Length == 4 && ((aux.flags & (int)NodeMetadata.Types.Flags.Leaf) == 0);

			if (has_bulk)
			{
				var epoch = node_meta.HasBulkMetadataEpoch
					? node_meta.BulkMetadataEpoch
					: bulk._metadata.HeadNodeKey.Epoch;

				var b = new bulk_t();
				b.setNotDownloadedYet();
				b.parent = bulk;
				b.request = rocktree_util.createBulkMetadataRequest(bulk.request.NodeKey.Path, aux.path, (int)epoch);
				b.busy_ctr.Value = 0;
				bulk.bulks.TryAdd(aux.path, b);
			}

			if ((has_data || (aux.flags & (int)NodeMetadata.Types.Flags.Leaf)==0 ) && !node_meta.HasOrientedBoundingBox)
			{
				Debug.LogError("skip unknown node\n");
			}

			if ((has_data || (aux.flags & (int)NodeMetadata.Types.Flags.Leaf) == 0) && node_meta.HasOrientedBoundingBox)
			{
				var meters_per_texel = node_meta.HasMetersPerTexel
					? node_meta.MetersPerTexel
					: bulk._metadata.MetersPerTexel[aux.level - 1];

				var n = new rocktree_t.node_t();
				n.setNotDownloadedYet();
				n.parent = bulk;
				n.can_have_data = has_data;
				if (has_data)
				{
					n.request = rocktree_util.createNodeDataRequest(bulk.request.NodeKey.Path, bulk._metadata, node_meta);
				}
				n.meters_per_texel = meters_per_texel;
				n.obb = rocktree_decoder.unpackObb(node_meta.OrientedBoundingBox.ToByteArray(), bulk.head_node_center, meters_per_texel);
				
				bulk.nodes.TryAdd(aux.path, n);
			}
		}
		bulk._metadata = null;
		bulk.setFinishedDownloading();
	}

	public static void populatePlanetoid(rocktree_t planetoid, PlanetoidMetadata planetoid_metadata)
	{
		var bulk = new bulk_t();
		bulk.parent = null;
		bulk.request = rocktree_util.createBulkMetadataRequest("", "", (int)planetoid_metadata.RootNodeMetadata.Epoch);
		bulk.busy_ctr.Value = 0;
		bulk.setNotDownloadedYet();

		planetoid.radius = planetoid_metadata.Radius;
		planetoid.root_bulk = bulk;
		planetoid.downloaded.Value = true;
	}

	public static void populateNode(rocktree_t.node_t node, NodeData node_data)
	{
		if (node.request.NodeKey.Path == "216")
        {
			int dfdqq = 0;
        }

		if (!(node.can_have_data))
			throw new System.Exception("INTERNAL ERROR");

		for (int i = 0; i < 16; i++) node.matrix_globe_from_mesh.mat[i%4,i/4] = node_data.MatrixGlobeFromMesh[i];

		foreach (var mesh in node_data.Meshes)
		{
			rocktree_t.node_t.mesh_t m = new rocktree_t.node_t.mesh_t();

			m.indices = rocktree_decoder.unpackIndices(mesh.Indices.ToByteArray());
			m.vertices = rocktree_decoder.unpackVertices(mesh.Vertices.ToByteArray());

			rocktree_decoder.unpackTexCoords(mesh.TextureCoordinates.ToByteArray(), m.vertices, ref m.uv_offset, ref m.uv_scale);
			if (mesh.UvOffsetAndScale.Count == 4)
			{
				m.uv_offset.x = mesh.UvOffsetAndScale[0];
				m.uv_offset.y = mesh.UvOffsetAndScale[1];
				m.uv_scale.x = mesh.UvOffsetAndScale[2];
				m.uv_scale.y = -mesh.UvOffsetAndScale[3];
			}
			else
			{
				m.uv_offset.y -= 1 / m.uv_scale.y;
				m.uv_scale.y *= -1;
			}

			int[] layer_bounds = new int[10];
			rocktree_decoder.unpackOctantMaskAndOctantCountsAndLayerBounds(mesh.LayerAndOctantCounts.ToByteArray(), m.indices, m.vertices, layer_bounds);
			if (!(0 <= layer_bounds[3] && layer_bounds[3] <= m.indices.Length))
				throw new System.Exception("INTERNAL ERROR");

			Array.Resize(ref m.indices, layer_bounds[3]);

			var textures = mesh.Texture;
			if (textures.Count != 1)
				throw new Exception("INTERNAL ERROR");
			
			var texture = textures[0];
			if (texture.Data.Count != 1)
				throw new Exception("INTERNAL ERROR");
			
			var tex = texture.Data[0];

			// maybe: keep compressed in memory?
			if (texture.Format == GeoGlobetrotterProtoRocktree.Texture.Types.Format.Jpg)
			{
				var data = tex.ToByteArray();
				int[] width = new int[1];
				int[] height = new int[1];
				int[] comp = new int[1];
				GCHandle pinnedData = GCHandle.Alloc(data, GCHandleType.Pinned);
				GCHandle pinnedWidth = GCHandle.Alloc(width, GCHandleType.Pinned);
				GCHandle pinnedHeight = GCHandle.Alloc(height, GCHandleType.Pinned);
				GCHandle pinnedComp = GCHandle.Alloc(comp, GCHandleType.Pinned);

				IntPtr pinnedPixels = StbPlugin.export_stbi_load_from_memory(pinnedData.AddrOfPinnedObject(), tex.Length, pinnedWidth.AddrOfPinnedObject(), pinnedHeight.AddrOfPinnedObject(), pinnedComp.AddrOfPinnedObject(), 4);
				if (pinnedPixels == null)
					throw new Exception("INTERNAL ERROR");

				if (!(width[0] == texture.Width && height[0] == texture.Height && comp[0] == 3))
					throw new Exception("INTERNAL ERROR");

				byte[] managedArray = new byte[width[0] * height[0] * 4];
				Marshal.Copy(pinnedPixels, managedArray, 0, width[0] * height[0] * 4);
				
				m.texture_Data = managedArray;
				StbPlugin.export_stbi_image_free(pinnedPixels);
				m.texture_format = rocktree_t.texture_format.texture_format_rgb;

				pinnedData.Free();
				pinnedWidth.Free();
				pinnedHeight.Free();
				pinnedComp.Free();
			}
			else if (texture.Format == GeoGlobetrotterProtoRocktree.Texture.Types.Format.CrnDxt1)
			{
				var src_size = tex.Length;
				var src = tex.ToByteArray();
				GCHandle pinnedSrc = GCHandle.Alloc(src, GCHandleType.Pinned);
				var dst_size = CrunchPlugin.crn_get_decompressed_size(pinnedSrc.AddrOfPinnedObject(), (uint)src_size, 0);
				if ( ! (dst_size == ((texture.Width + 3) / 4) * ((texture.Height + 3) / 4) * 8))
					throw new Exception("INTERNAL ERROR");
				m.texture_Data = new byte[dst_size];
				GCHandle pinnedTexture = GCHandle.Alloc(m.texture_Data[0], GCHandleType.Pinned);
				CrunchPlugin.crn_decompress(pinnedSrc.AddrOfPinnedObject(), (uint)src_size, pinnedTexture.AddrOfPinnedObject(), dst_size, 0);
				m.texture_format = rocktree_t.texture_format.texture_format_dxt1;
				pinnedTexture.Free();
				pinnedSrc.Free();
			}
			else
			{
				Debug.LogError("unsupported texture format: " + texture.Format );
				throw new Exception("INTERNAL ERROR");
			}
			
			m.texture_width = (int)texture.Width;
			m.texture_height = (int)texture.Height;

			m.buffered = false;
			node.meshes.Add(m);
		}
		node.setFinishedDownloading();
	}
}
