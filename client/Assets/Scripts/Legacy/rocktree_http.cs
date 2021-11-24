/*struct x
{
	int i;
	void (* thunk) (int i, int error, uint8_t* d, size_t l);
};

// if you want to retain the data you need to copy it
void downloadSucceeded(emscripten_fetch_t* fetch)
{
	auto xx = (x*)(fetch->userData);
	xx->thunk(xx->i, 0, (uint8_t*)fetch->data, (size_t)fetch->numBytes);
	delete xx;
	emscripten_fetch_close(fetch);
}

void downloadFailed(emscripten_fetch_t* fetch)
{
	//fprintf(stderr, "Downloading %s failed, HTTP failure status code: %d.\n", fetch->url, fetch->status);  
	auto xx = (x*)(fetch->userData);
	xx->thunk(xx->i, 1, NULL, 0);
	delete xx;
	emscripten_fetch_close(fetch);
}
*/


using GeoGlobetrotterProtoRocktree;
using System;
using System.IO;
using System.Net.Http;

public class rocktree_http
{
	static readonly HttpClient client = new HttpClient();
	static readonly string cache_pfx = "cache/";
	static bool folderCreated = false;

	public static int simultaneousRequests = 0;
	public static int maxRequests = 15;
	static SuperQueue _queue = null;
	public static SuperQueue queue
	{
		get
		{
			if (_queue == null)
				_queue = new SuperQueue(maxRequests + 1);
			return _queue;
		}
	}

	public class FetchResult
	{
		public Tuple<Action<BulkMetadata>, rocktree_t.bulk_t> i;
		public Action<PlanetoidMetadata> i2;
		public Tuple<Action<NodeData>, rocktree_t.node_t> i3;
		public Exception error;
		public byte[] data;
	}

	public static void fetchData(string path,  Tuple<Action<BulkMetadata>, rocktree_t.bulk_t> i, Action<PlanetoidMetadata> i2, Tuple<Action<NodeData>, rocktree_t.node_t> i3, Action<FetchResult> action)
	{
		if (!folderCreated)
        {
			if (!Directory.Exists(cache_pfx))
				Directory.CreateDirectory(cache_pfx);
			if (!Directory.Exists(cache_pfx + "BulkMetadata"))
				Directory.CreateDirectory(cache_pfx + "BulkMetadata");
			if (!Directory.Exists(cache_pfx + "NodeData"))
				Directory.CreateDirectory(cache_pfx + "NodeData");
			folderCreated = true;
		}

		FetchResult result = new FetchResult();
		result.i = i;
		result.i2 = i2;
		result.i3 = i3;

		bool use_cache = true;//path[0] != 'P'; // don't cache planetoid
		string cache_path = cache_pfx + path;
		if (use_cache && File.Exists(cache_path))
		{
			result.error = null;
			result.data = File.ReadAllBytes(cache_path);
			action(result);
			return;
		}

		try
		{
			const string base_url = "https://kh.google.com/rt/earth/";
			string url = base_url + path;

			var response = client.GetAsync(url).Result;
			response.EnsureSuccessStatusCode();
			result.error = null;
			result.data = response.Content.ReadAsByteArrayAsync().Result;
		}
		catch (Exception e)
		{
			result.error = e;
			result.data = null;
			action(result);
			return;
		}

		if (use_cache)
		{
			File.WriteAllBytes(cache_path, result.data);
		}

		action(result);
	}
}