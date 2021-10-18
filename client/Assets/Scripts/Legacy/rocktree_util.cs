using GeoGlobetrotterProtoRocktree;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using static rocktree_decoder;
using static rocktree_http;

public class rocktree_util
{
	
	public static BulkMetadataRequest createBulkMetadataRequest(string base_path, string path, int epoch)
	{
		BulkMetadataRequest req = new BulkMetadataRequest();
		var key = new NodeKey();
		key.Path = base_path + path;
		key.Epoch = (uint)epoch;
		req.NodeKey = key;
		return req;
	}

	static global::GeoGlobetrotterProtoRocktree.Texture.Types.Format getSupportedTextureFormat(BulkMetadata bulk, NodeMetadata node_meta)
    {
		global::GeoGlobetrotterProtoRocktree.Texture.Types.Format[] supported = { global::GeoGlobetrotterProtoRocktree.Texture.Types.Format.CrnDxt1, global::GeoGlobetrotterProtoRocktree.Texture.Types.Format.Jpg };
		int available = node_meta.HasAvailableTextureFormats
			? (int)node_meta.AvailableTextureFormats
			: (int)bulk.DefaultAvailableTextureFormats;

		foreach ( var s in supported)
        {
			if ( (available & (1 << ((int)s - 1))) != 0 ) return s;
		}

		return supported[0];
	}

	static NodeKey getAllocatedNodeKey(string base_path, BulkMetadata bulk, NodeMetadata node_meta, node_data_path_and_flags_t aux)
	{
		NodeKey key = new NodeKey();
		key.Path = base_path + aux.path;
		if (!((bulk.HeadNodeKey != null && bulk.HeadNodeKey.HasEpoch)))
			throw new Exception("INTERNAL ERROR");
		key.Epoch = node_meta.HasEpoch ? node_meta.Epoch : bulk.HeadNodeKey.Epoch;
		return key;
	}

	// createNodeDataRequest creates a object for requesting node data. can be re-used
	public static NodeDataRequest createNodeDataRequest(string base_path, BulkMetadata bulk, NodeMetadata node_meta)
	{
		var aux = rocktree_decoder.unpackPathAndFlags(node_meta);
		if ( (aux.flags & (int)NodeMetadata.Types.Flags.Nodata) == 0)
			throw new Exception("INTERNAL ERROR");

		//assert(node_meta.has_epoch());
		NodeDataRequest req = new NodeDataRequest();

		// set texture format based on supported formats
		req.TextureFormat = getSupportedTextureFormat(bulk, node_meta);

		// set imagery epoch if flags say it should be used
		if ( ((int)aux.flags & (int)NodeMetadata.Types.Flags.UseImageryEpoch) != 0)
		{
			var imagery_epoch = node_meta.HasImageryEpoch ?
				node_meta.ImageryEpoch :
				bulk.DefaultImageryEpoch;
			req.ImageryEpoch = imagery_epoch;
		}

		// set path and epoch
		req.NodeKey = getAllocatedNodeKey(base_path, bulk, node_meta, aux);

		return req;
	}

	static ConcurrentDictionary<int, Action<PlanetoidMetadata>> mapPlanetoid = new ConcurrentDictionary<int, Action<PlanetoidMetadata>>();
	static int indexPlanetoid = 0;

	// getPlanetoid fetches planetoid from web and calls cb when it's done.
	public static void getPlanetoid(Action<PlanetoidMetadata> cb)
	{

		Action<FetchResult> thunk = (FetchResult result) =>
		{
			var cb = mapPlanetoid[result.i];		
  			if (result.error != 0)
			{
				Debug.LogError("could not load planetoid");
				cb(null);
			}
			else
			{
				PlanetoidMetadata planetoid = PlanetoidMetadata.Parser.ParseFrom(result.data);
				cb(planetoid);
			}
			mapPlanetoid.TryRemove(indexPlanetoid, out var val);
		};

		mapPlanetoid[++indexPlanetoid] = cb;
		rocktree_http.fetchData("PlanetoidMetadata", indexPlanetoid, thunk);
	}

	static ConcurrentDictionary<int, Tuple<Action<BulkMetadata>, rocktree_t.bulk_t>> mapBulk =
		new ConcurrentDictionary<int, Tuple<Action<BulkMetadata>, rocktree_t.bulk_t>>();

	static int indexBulk = 0;

	// getBulk fetches a bulk using path and epoch from web or cache and calls cb when it's done.
	public static void getBulk(BulkMetadataRequest req, rocktree_t.bulk_t b, Action<BulkMetadata> cb)
	{
		var path = req.NodeKey.Path;
		var epoch = req.NodeKey.Epoch;

		Action<FetchResult> thunk = (FetchResult result) =>
		{
			mapBulk.TryRemove(result.i, out var pair);
			var cb = pair.Item1;
			var b = pair.Item2;

			if (result.error != 0)
            {
				Debug.LogError("could not load node");
				cb(null);
			}
			else
            {
				BulkMetadata bulk = BulkMetadata.Parser.ParseFrom(result.data);

				if (bulk==null)
                {
					b.setFailedDownloading();
					cb(null);
					return;
				}

				rocktree_ex.populateBulk(b, bulk);
				cb(null);
			}
		};

		if (path.Length >= 30)
			throw new Exception("INTERNAL ERROR");


		var url_buf = "BulkMetadata/pb=!1m2!1s" + path + "!2u" + epoch;
		++indexBulk;
		mapBulk[indexBulk] = new Tuple<Action<BulkMetadata>, rocktree_t.bulk_t>(cb, b);
		fetchData(url_buf, indexBulk, thunk);
	}

	static ConcurrentDictionary<int, Tuple<Action<NodeData>, rocktree_t.node_t>> mapNode =
		new ConcurrentDictionary<int, Tuple<Action<NodeData>, rocktree_t.node_t>>();

	static int indexNode = 0;

	// getNode fetches a node from path, epoch, texture_format and imagery_epoch (none if -1) and calls cb when it's done.
	public static void getNode(NodeDataRequest req, rocktree_t.node_t n, Action<NodeData> cb)
	{

		Action<FetchResult> thunk = (FetchResult result) =>
		{
			mapNode.TryRemove(result.i, out var pair);

			var cb = pair.Item1;
			var n = pair.Item2;

			if (result.error != 0)
			{
				Debug.LogError("could not load node");
				cb(null);
			}
			else
			{
				NodeData node = NodeData.Parser.ParseFrom(result.data);

				if (node == null)
				{
					Debug.LogError("download failed");
					n.setFailedDownloading();
					cb(null);
					return;
				}

				rocktree_ex.populateNode(n, node);
				cb(null);
			}
		};

		var path = req.NodeKey.Path;

		if (path.Length >= 30)
			throw new Exception("INTERNAL ERROR");
	
		string url_buf;
	
		if (!req.HasImageryEpoch)
		{
			url_buf = "NodeData/pb=!1m2!1s" + path + "!2u" + req.NodeKey.Epoch +"!2e" + req.TextureFormat + "!4b0";
		}
		else
		{
			url_buf = "NodeData/pb=!1m2!1s" + path + "!2u" + req.NodeKey.Epoch + "!2e" + req.TextureFormat + "!3u" + req.ImageryEpoch + "!4b0";
		}

		++indexNode;
		mapNode[indexNode] = new Tuple<Action<NodeData>, rocktree_t.node_t>(cb, n);
		fetchData(url_buf, indexNode, thunk);
	}

	class llbounds_t
	{
		public double n, s, w, e;
	};

	// latLonToOctant converts lat-lon to octant. incorrectly?
	void latLonToOctant(double lat, double lon, ref byte[] octant)
	{
		octant = new byte[rocktree_decoder.MAX_LEVEL + 1];
		octant[0] = 0;
		octant[1] = 0;
		llbounds_t box = new llbounds_t();

		if (lat < 0.0) { octant[1] |= 2; box.n = 0.0; box.s = -90.0; }
		else { octant[0] |= 2; box.n = 90.0; box.s = 0.0; }

		if (lon < -90.0) { box.w = -180.0; box.e = -90.0; }
		else if (lon < 0.0) { octant[1] |= 1; box.w = -90.0; box.e = 0.0; }
		else if (lon < 90.0) { octant[0] |= 1; box.w = 0.0; box.e = 90.0; }
		else { octant[0] |= 1; octant[1] |= 1; box.w = 90.0; box.e = 180.0; }

		int level = MAX_LEVEL;
		for (int i = 2; i < level; i++)
		{
			octant[i] = 0;

			double mid_lat = (box.n + box.s) / 2.0;
			double mid_lon = (box.w + box.e) / 2.0;

			if (lat < mid_lat)
			{
				box.n = mid_lat;
			}
			else
			{
				box.s = mid_lat;
				octant[i] |= 2;
			}

			if (lon < mid_lon)
			{
				box.e = mid_lon;
			}
			else
			{
				box.w = mid_lon;
				octant[i] |= 1;
			}
		}

		// to ascii
		for (int i = 0; i < level; i++) octant[i] += (byte)'0';
		octant[MAX_LEVEL] = 0;
	}



}
