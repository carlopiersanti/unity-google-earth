using GeoGlobetrotterProtoRocktree;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static rocktree_t;

public class rocktree_ex
{
	public static void populateBulk(bulk_t bulk, BulkMetadata bulk_metadata)
	{
		bulk._metadata = bulk_metadata;

		bulk.head_node_center = new Vector3();
		bulk.head_node_center.mat[0, 0] = bulk._metadata.HeadNodeCenter[0];
		bulk.head_node_center.mat[1, 0] = bulk._metadata.HeadNodeCenter[1];
		bulk.head_node_center.mat[2, 0] = bulk._metadata.HeadNodeCenter[2];

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

			if ((has_data || !((aux.flags & (int)NodeMetadata.Types.Flags.Leaf) == 0)) && node_meta.HasOrientedBoundingBox)
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
		if (!(node.can_have_data))
			throw new System.Exception("INTERNAL ERROR");

		for (int i = 0; i < 16; i++) node.matrix_globe_from_mesh.mat[i/4,i%4] = node_data.MatrixGlobeFromMesh[i];

		foreach (var mesh in node_data.Meshes)
		{
			rocktree_t.node_t.mesh_t m = new rocktree_t.node_t.mesh_t();

			m.indices = rocktree_decoder.unpackIndices(mesh.Indices.ToByteArray());
			m.vertices = rocktree_decoder.unpackVertices(mesh.Vertices.ToByteArray());

			rocktree_decoder.unpackTexCoords(mesh.TextureCoordinates.ToByteArray(), m.vertices, ref m.uv_offset, ref m.uv_scale);
			if (mesh.UvOffsetAndScale.Count == 4)
			{
				m.uv_offset[0] = mesh.UvOffsetAndScale[0];
				m.uv_offset[1] = mesh.UvOffsetAndScale[1];
				m.uv_scale[0] = mesh.UvOffsetAndScale[2];
				m.uv_scale[1] = mesh.UvOffsetAndScale[3];
			}
			else
			{
				m.uv_offset[1] -= 1 / m.uv_scale[1];
				m.uv_scale[1] *= -1;
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
			if (texture.Data.Count != 1);
				throw new Exception("INTERNAL ERROR");
			
			var tex = texture.Data[0];

			// maybe: keep compressed in memory?
			/*if (texture.format() == Texture_Format_JPG)
			{
				auto data = (uint8_t*)tex.data();
				int width, height, comp;
				unsigned char* pixels = stbi_load_from_memory(&data[0], tex.size(), &width, &height, &comp, 0);
				assert(pixels != NULL);
				assert(width == texture.width() && height == texture.height() && comp == 3);
				m.texture = std::vector<uint8_t>(pixels, pixels + width * height * comp);
				stbi_image_free(pixels);
				m.texture_format = rocktree_t::texture_format_rgb;
			}
			else if (texture.format() == Texture_Format_CRN_DXT1)
			{
				auto src_size = tex.size();
				auto src = (uint8_t*)tex.data();
				auto dst_size = crn_get_decompressed_size(src, src_size, 0);
				assert(dst_size == ((texture.width() + 3) / 4) * ((texture.height() + 3) / 4) * 8);
				m.texture = std::vector<uint8_t>(dst_size);
				crn_decompress(src, src_size, m.texture.data(), dst_size, 0);
				m.texture_format = rocktree_t::texture_format_dxt1;
			}
			else
			{
				fprintf(stderr, "unsupported texture format: %d\n", texture.format());
				abort();
			}*/

			m.texture_width = (int)texture.Width;
			m.texture_height = (int)texture.Height;

			m.buffered = false;
			node.meshes.Add(m);
		}
		node.setFinishedDownloading();
	}
}
