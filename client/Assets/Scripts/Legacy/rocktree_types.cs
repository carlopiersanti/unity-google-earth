using GeoGlobetrotterProtoRocktree;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using static rocktree_decoder;

public enum dl_state : int
{
	dl_state_stub = 1,
	dl_state_downloading = 2,
	dl_state_downloaded = 4,
};




public class rocktree_t
{
	public enum texture_format : int
	{
		texture_format_rgb = 1,
		texture_format_dxt1 = 2,
	};  

	public class node_t
	{
		public NodeDataRequest request;
		public bool can_have_data;
		public Atomic<dl_state> dl_state = new Atomic<dl_state>(global::dl_state.dl_state_stub);
		public bulk_t parent;

		public void setNotDownloadedYet()
		{
			dl_state.Value = global::dl_state.dl_state_stub;
		}

		public void setStartedDownloading()
		{
			if (parent != null) parent.busy_ctr.Operation ( t => t++);
			dl_state.Value = global::dl_state.dl_state_downloading;
		}

		public void setFinishedDownloading()
		{
			dl_state.Value = global::dl_state.dl_state_downloaded;
		}

		public void setFailedDownloading()
		{
			dl_state.Value = global::dl_state.dl_state_stub;
			if (parent != null) parent.busy_ctr.Operation(t => t--);
		}

		public void setDeleted()
		{
			dl_state.Value = global::dl_state.dl_state_stub;
			if (parent != null) parent.busy_ctr.Operation(t => t--);
		}

		public float meters_per_texel;
		public rocktree_decoder.OrientedBoundingBox obb;

		public NodeData _data;

		public Matrix4x4 matrix_globe_from_mesh = new Matrix4x4();
		public class mesh_t
		{
			//public vertex_t[] vertices;
			public int[] indices;
			public Vector2 uv_offset;
			public Vector2 uv_scale;

			public byte[] texture_Data;
			public texture_format texture_format;
			public int texture_width;
			public int texture_height;

			public UnityEngine.Vector3[] mesh_positions;
			public UnityEngine.Vector2[] mesh_texCoords;
			public UnityEngine.Vector2[] octants;

			public ComputeBuffer computeBufferIndices;
			public ComputeBuffer computeBufferVertex;

			public bool buffered;

			public Texture2D texture;
		};
		public List<mesh_t> meshes = new List<mesh_t>();
	};

	public class bulk_t
	{
		public BulkMetadataRequest request;
		public Atomic<dl_state> dl_state = new Atomic<dl_state>(global::dl_state.dl_state_stub);
		public bulk_t parent;

		public void setNotDownloadedYet()
		{
			dl_state.Value = global::dl_state.dl_state_stub;
		}

		public void setStartedDownloading()
		{
			if (parent!=null) parent.busy_ctr.Operation(t => t++);
			dl_state.Value = global::dl_state.dl_state_downloading;
		}

		public void setFinishedDownloading()
		{
			dl_state.Value = global::dl_state.dl_state_downloaded;
		}

		public void setFailedDownloading()
		{
			dl_state.Value = global::dl_state.dl_state_stub;
			if (parent!=null) parent.busy_ctr.Operation(t => t--);
		}

		public void setDeleted()
		{
			dl_state.Value = global::dl_state.dl_state_stub;
			if (parent != null) parent.busy_ctr.Operation(t => t--);
		}

		public UnityEngine.Vector3 head_node_center;

		public BulkMetadata _metadata;
		public Atomic<int> busy_ctr = new Atomic<int>(0);

		public ConcurrentDictionary<string, node_t> nodes = new ConcurrentDictionary<string, node_t>();
		public ConcurrentDictionary<string, bulk_t> bulks = new ConcurrentDictionary<string, bulk_t>();
	};

	public float radius;
	public bulk_t root_bulk;
	public PlanetoidMetadata _metadata;
	public Atomic<bool> downloaded = new Atomic<bool>(false);
}
