using FFmpeg;
using GeoGlobetrotterProtoRocktree;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using UnityEngine;

public class main : MonoBehaviour
{
	[SerializeField] public Camera mainCamera;
	[SerializeField] public Material tileMaterial;
	[SerializeField] rocktree_gl gl;

	static rocktree_t _planetoid = null;

	TripleBuffer potential_nodes_triple_buffer = new TripleBuffer();


	private void Awake()
    {
		ServicePointManager.DefaultConnectionLimit = 20000;

		

		StartCoroutine(Load());
	}
	
	IEnumerator Load()
    {
		yield return new WaitForSecondsRealtime(3.0f);

		loadPlanet();
	}


    private void Update()
    {
		drawPlanet();
	}

    void loadPlanet()
    {
        var planetoid = new rocktree_t();
        planetoid.downloaded.Value = false;
        _planetoid = planetoid;

		rocktree_http.simultaneousRequests++;
        rocktree_util.getPlanetoid((PlanetoidMetadata _metadata) =>
        {
			rocktree_http.simultaneousRequests--;
			if (_metadata == null)
            {
                Debug.LogError("NO PLANETOID");
                return;
            }
            rocktree_ex.populatePlanetoid(planetoid, _metadata);

            var bulk = planetoid.root_bulk;
            if (bulk.dl_state.Value != dl_state.dl_state_stub)
                throw new System.Exception("INTERNAL ERROR");

            bulk.setStartedDownloading();

			rocktree_http.simultaneousRequests++;
			rocktree_util.getBulk(bulk.request, bulk, (cb) => { rocktree_http.simultaneousRequests--; });
        });
    }
	Thread t = null;
    private void Start()
    {
		/*Vector3 eye = new Vector3();
		eye.mat[0, 0] = 1329866.230289;
		eye.mat[1, 0] = -4643494.267515;
		eye.mat[2, 0] = 4154677.131562;

		Vector3 direction = new Vector3();
		direction.mat[0, 0] = 0.219862;
		direction.mat[1, 0] = 0.419329;
		direction.mat[2, 0] = 0.312226;

		UnityEngine.Vector3 position =
	new UnityEngine.Vector3((float)eye.mat[0, 0], (float)eye.mat[1, 0], (float)eye.mat[2, 0]);

		UnityEngine.Vector3 up = position.normalized;

		UnityEngine.Vector3 lookAt =
			new UnityEngine.Vector3((float)(direction.mat[0, 0]), (float)(direction.mat[1, 0]), (float)(direction.mat[2, 0]));

		mainCamera.transform.position = UnityEngine.Vector3.zero;
		mainCamera.transform.LookAt(-lookAt, up);
		mainCamera.transform.position = position;*/


	}

	string[] octs = new string[] { "0", "1", "2", "3", "4", "5", "6", "7" };


	public float speed_amp;
	Matrix viewprojection;
	UnityEngine.Vector3 mainCameraPosition;
	UnityEngine.Vector3 mainCameraForward;

	Tuple<Dictionary<string, rocktree_t.bulk_t>, SortedDictionary<string, rocktree_t.node_t>> read;

	void drawPlanet()
	{
		var planetoid = _planetoid;
		if (planetoid == null) return;
		if (!planetoid.downloaded.Value) return;
		if (planetoid.root_bulk.dl_state.Value != dl_state.dl_state_downloaded) return;
		var current_bulk = planetoid.root_bulk;
		var planet_radius = planetoid.radius;

		int width, height;

		bool key_up_pressed = Input.GetKey(KeyCode.Z);
		bool key_left_pressed = Input.GetKey(KeyCode.Q);
		bool key_down_pressed = Input.GetKey(KeyCode.S);
		bool key_right_pressed = Input.GetKey(KeyCode.D);
		bool key_boost_pressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
		bool mouse_pressed = Input.GetMouseButton(1);


		// from lat/lon
		//static Vector3d ecef = { ...https://www.oc.nps.edu/oc2902w/coord/llhxyz.htm };
		//static auto ecef_norm = ecef.normalized();
		//static Vector3d eye = (ecef_norm * (planet_radius + 10000));

		// up is the vec from the planetoid's center towards the sky

		// projection
		//float aspect_ratio = (float)width / (float)height;
		//float fov = 0.25f * (float)M_PI;
		double altitude = mainCamera.transform.position.magnitude - planet_radius;
		var horizon = Math.Sqrt(altitude * (2 * planet_radius + altitude));
		var near = horizon > 370000 ? altitude / 2 : 5;
		var far = horizon;
		if (near >= far) near = far - 1;
		if (double.IsNaN(far) || far < near) far = near + 1;
		mainCamera.fieldOfView = 45;
		mainCamera.nearClipPlane = (float)near;
		mainCamera.farClipPlane = (float)far;
		var projectionUnity = UnityEngine.Matrix4x4.Perspective(mainCamera.fieldOfView, mainCamera.aspect, mainCamera.nearClipPlane, mainCamera.farClipPlane);

	    speed_amp = 100* Mathf.Min(2600, Mathf.Pow(Mathf.Max(0, ((float)altitude - 500) / 10000) + 1, 1.337f), 2) / 6;

		// rotation
		/*int mouse_x, mouse_y;
		SDL_GetRelativeMouseState(&mouse_x, &mouse_y);
		double yaw = mouse_x * 0.001;
		double pitch = -mouse_y * 0.001;
		auto overhead = direction.dot(-up);
		if ((overhead > 0.99 && pitch < 0) || (overhead < -0.99 && pitch > 0))
			pitch = 0;
		auto pitch_axis = direction.cross(up);
		auto yaw_axis = direction.cross(pitch_axis);
		pitch_axis.normalize();
		AngleAxisd roll_angle(0, Vector3d::UnitZ());
		AngleAxisd yaw_angle(yaw, yaw_axis);
		AngleAxisd pitch_angle(pitch, pitch_axis);
		auto quat = roll_angle * yaw_angle * pitch_angle;
		auto rotation = quat.matrix();
		direction = (rotation * direction).normalized();

		// movement
		auto speed_amp = fmin(2600, powf(fmax(0, (altitude - 500) / 10000) + 1, 1.337)) / 6;
		auto mag = 100 * (deltaTime / 17.0) * (1 + key_boost_pressed * 4) * speed_amp;
		auto sideways = direction.cross(up).normalized();
		auto forwards = direction * mag;
		auto backwards = -direction * mag;
		auto left = -sideways * mag;
		auto right = sideways * mag;
		auto new_eye = eye + key_up_pressed * forwards
							+ key_down_pressed * backwards
							+ key_left_pressed * left
							+ key_right_pressed * right;
		auto pot_altitude = new_eye.norm() - planet_radius;
		if (pot_altitude < 1000 * 1000 * 10)
		{
			eye = new_eye;
		}

		auto view = lookAt(eye, eye + direction, up);*/


		Matrix4x4 view = new Matrix4x4(mainCamera.transform.worldToLocalMatrix);
		Matrix4x4 projection = new Matrix4x4(projectionUnity);

		viewprojection = (Matrix)projection * (Matrix)view;
		mainCameraPosition = mainCamera.transform.position;
		mainCameraForward = mainCamera.transform.forward;

		if (t==null)
        {
			t = new Thread(() => { while (true) { ThreadTilesCalculation(); } });
			t.Start();
		}

		var read2 = potential_nodes_triple_buffer.GetReadBuffer();
		if (read2 != null)
			read = read2;

		if (read != null)
			DisplayTiles(read, viewprojection);
	}

	void ThreadTilesCalculation()
    {
		var planetoid = _planetoid;
		if (planetoid == null) return;
		if (!planetoid.downloaded.Value) return;
		if (planetoid.root_bulk.dl_state.Value != dl_state.dl_state_downloaded) return;
		var current_bulk = planetoid.root_bulk;
		var planet_radius = planetoid.radius;

		var potential_nodes = new SortedDictionary<string, rocktree_t.node_t>();

		 //DEBUG FROM THIS POINT
		 var frustum_planes = rocktree_math.getFrustumPlanes(viewprojection); // for obb culling
		List<Tuple<string, rocktree_t.bulk_t>> valid = new List<Tuple<string, rocktree_t.bulk_t>> { new Tuple<string, rocktree_t.bulk_t>("", current_bulk) };

		List<Tuple<string, rocktree_t.bulk_t>> next_valid = new List<Tuple<string, rocktree_t.bulk_t>>();
		//std::multimap<double, rocktree_t::node_t *> dist_nodes;

		// todo: improve download order
		// todo: abort emscripten_fetch_close() https://emscripten.org/docs/api_reference/fetch.html
		//       and/or emscripten coroutine fetch semaphore	
		// todo: purge branches less aggressively	
		// todo: workers instead of shared mem https://emscripten.org/docs/api_reference/emscripten.h.html#worker-api	

		Dictionary<string, rocktree_t.bulk_t> potential_bulks = new Dictionary<string, rocktree_t.bulk_t>();

		// node culling and level of detail using breadth-first search
		for (; ; )
		{
			foreach (var cur2 in valid)
			{
				var cur = cur2.Item1;
				var bulk = cur2.Item2;

				if (cur.Length > 0 && cur.Length % 4 == 0)
				{
					var rel = cur.Substring((int)Math.Floor((decimal)(cur.Length - 1) / 4) * 4, 4);
					var has_bulk = bulk.bulks.TryGetValue(rel, out var b);
					if (!has_bulk)
						continue;
					potential_bulks[cur] = b;
					if (b.dl_state.Value == dl_state.dl_state_stub)
					{
						if (rocktree_http.simultaneousRequests < rocktree_http.maxRequests)
						{
							rocktree_http.simultaneousRequests++;
							b.setStartedDownloading();
							rocktree_util.getBulk(b.request, b, (cb) => { rocktree_http.simultaneousRequests--; });
						}
					}
					if (b.dl_state.Value != dl_state.dl_state_downloaded) continue;
					bulk = b;
				}
				potential_bulks[cur] = bulk;

				foreach (var o in octs)
				{
					var nxt = cur + o;
					int index = (int)Math.Floor((decimal)(nxt.Length - 1) / 4) * 4;
					var nxt_rel = nxt.Substring(index, Math.Min(4, nxt.Length - index));
					if (!bulk.nodes.TryGetValue(nxt_rel, out var node))
						continue;

					// cull outside frustum using obb
					// todo: check if it could cull more
					if (rocktree_math.obb_frustum.obb_frustum_outside == rocktree_math.classifyObbFrustum(node.obb, frustum_planes))
					{
						continue;
					}

					// level of detail
					/*{
						auto obb_center = node->obb.center;
						auto obb_max_diameter = fmax(fmax(node->obb.extents[0], node->obb.extents[1]), node->obb.extents[2]);			

						auto t = Affine3d().Identity();
						t.translate(Vector3d(obb_center.x(), obb_center.y(), obb_center.z()));
						t.scale(obb_max_diameter);
						Matrix4d viewprojection_d;
						for(auto i = 0; i < 16; i++) viewprojection_d.data()[i] = viewprojection.data()[i];
						auto m = viewprojection_d * t;
						auto s = m(3, 3);
						if (s < 0) s = -s; // ?
						auto diameter_in_clipspace = 2 * (obb_max_diameter / s);  // *2 because clip space is -1 to +1
						auto amplify = 4; // todo: meters per texel
						if (diameter_in_clipspace < 0.5 / amplify) {
							continue;
						}
					}*/

					{
						var t = new Matrix4x4();
						var sub = new Vector3();
						sub.mat[0, 0] = mainCameraPosition.x - node.obb.center[0, 0];
						sub.mat[1, 0] = mainCameraPosition.y - node.obb.center[1, 0];
						sub.mat[2, 0] = mainCameraPosition.z - node.obb.center[2, 0];
						var eye = new Vector3();
						eye.mat[0, 0] = mainCameraPosition.x;
						eye.mat[1, 0] = mainCameraPosition.y;
						eye.mat[2, 0] = mainCameraPosition.z;
						var fwd = new Vector3();
						fwd.mat[0, 0] = -mainCameraForward.x;
						fwd.mat[1, 0] = -mainCameraForward.y;
						fwd.mat[2, 0] = -mainCameraForward.z;
						Matrix translation = eye + Math.Sqrt(Math.Pow(sub.mat[0, 0], 2) + Math.Pow(sub.mat[1, 0], 2) + Math.Pow(sub.mat[2, 0], 2)) * fwd;
						t.mat[0, 0] = 1;
						t.mat[1, 1] = 1;
						t.mat[2, 2] = 1;
						t.mat[3, 3] = 1;

						t.mat[0, 3] = translation.mat[0, 0];
						t.mat[1, 3] = translation.mat[1, 0];
						t.mat[2, 3] = translation.mat[2, 0];

						var m = viewprojection * t;
						var s = m.mat[3, 3];
						var texels_per_meter = 1.0f / node.meters_per_texel;
						var wh = 768; // width < height ? width : height;
						var r = (2.0 * (1.0 / s)) * wh;
						if (texels_per_meter > r)
						{
							continue;
						}
					}

					next_valid.Add(new Tuple<string, rocktree_t.bulk_t>(nxt, bulk));

					if (node.can_have_data)
					{
						potential_nodes[nxt] = node;
						//auto d = (node->obb.center - eye).squaredNorm();
						//dist_nodes[d] = node;
						//dist_nodes.insert(std::make_pair (d, node));
					}
				}
			}
			if (next_valid.Count == 0)
			{
				break;
			}
			valid = next_valid;
			next_valid = new List<Tuple<string, rocktree_t.bulk_t>>();
		}

		foreach (var kv in potential_nodes)
		{   // normal order
			//for (auto kv = potential_nodes.rbegin(); kv != potential_nodes.rend(); ++kv) { // reverse order
			//for (auto kv = dist_nodes.rbegin(); kv != dist_nodes.rend(); ++kv) { // reverse order
			//for (auto kv = dist_nodes.begin(); kv != dist_nodes.end(); ++kv) { // normal order
			var node = kv.Value;
			if (node.dl_state.Value == dl_state.dl_state_stub)
			{
				if (rocktree_http.simultaneousRequests < rocktree_http.maxRequests)
				{
					rocktree_http.simultaneousRequests++;
					node.setStartedDownloading();
					rocktree_util.getNode(node.request, node, node => { rocktree_http.simultaneousRequests--; });
				}
			}
		}

		potential_nodes_triple_buffer.SwapWriteBuffer(new Tuple<Dictionary<string, rocktree_t.bulk_t>, SortedDictionary<string, rocktree_t.node_t>>(potential_bulks, potential_nodes) );
	}



	void DisplayTiles(Tuple<Dictionary<string, rocktree_t.bulk_t>, SortedDictionary<string, rocktree_t.node_t>> potential, Matrix viewprojection)
	{
		var planetoid = _planetoid;
		if (planetoid == null) return;
		if (!planetoid.downloaded.Value) return;
		if (planetoid.root_bulk.dl_state.Value != dl_state.dl_state_downloaded) return;
		var current_bulk = planetoid.root_bulk;
		var planet_radius = planetoid.radius;

		 // 8-bit octant mask flags of nodes
		 Dictionary<string, byte> mask_map = new Dictionary<string, byte>();

		foreach (var kv in potential.Item2.Reverse())
		{ // reverse order
			var full_path = kv.Key;
			var node = kv.Value;
			var level = full_path.Count();

			if (level <= 0)
				throw new Exception("INTERNAL ERROR");
			if (!node.can_have_data)
				throw new Exception("INTERNAL ERROR");
			if (node.dl_state.Value != dl_state.dl_state_downloaded) continue;
			
			node.meshes.ForEach(m => { if (!m.buffering && !m.buffered) { gl.bufferMesh(full_path, m); } });

			if (node.meshes.Any(m => !m.buffered))
			{
				continue;
			}

			// set octant mask of previous node
			int octant = (int)(full_path[level - 1] - '0');
			var prev = full_path.Substring(0, level - 1);
			if (!mask_map.ContainsKey(prev))
				mask_map[prev] = 0;
			mask_map[prev] |= (byte)(1 << octant);

			// skip if node is masked completely
			if (!mask_map.ContainsKey(full_path))
				mask_map[full_path] = 0;
			if (mask_map[full_path] == 0xff) continue;

			// float transform matrix
			Matrix tiletransform = (Matrix)viewprojection * (Matrix)node.matrix_globe_from_mesh;
			UnityEngine.Matrix4x4 transform_float = new UnityEngine.Matrix4x4();
			{
				transform_float.m00 = (float)tiletransform.mat[0, 0];
				transform_float.m01 = (float)tiletransform.mat[0, 1];
				transform_float.m02 = (float)tiletransform.mat[0, 2];
				transform_float.m03 = (float)tiletransform.mat[0, 3];

				transform_float.m10 = (float)tiletransform.mat[1, 0];
				transform_float.m11 = (float)tiletransform.mat[1, 1];
				transform_float.m12 = (float)tiletransform.mat[1, 2];
				transform_float.m13 = (float)tiletransform.mat[1, 3];

				transform_float.m20 = (float)tiletransform.mat[2, 0];
				transform_float.m21 = (float)tiletransform.mat[2, 1];
				transform_float.m22 = (float)tiletransform.mat[2, 2];
				transform_float.m23 = (float)tiletransform.mat[2, 3];

				transform_float.m30 = (float)tiletransform.mat[3, 0];
				transform_float.m31 = (float)tiletransform.mat[3, 1];
				transform_float.m32 = (float)tiletransform.mat[3, 2];
				transform_float.m33 = (float)tiletransform.mat[3, 3];
			}

			// buffer, bind, draw
			foreach (var mesh in node.meshes)
			{
				//if (!mesh.buffering) GetComponent<rocktree_gl>().bufferMesh(mesh);
				if (mesh.buffered) gl.bindAndDrawMesh(mainCamera, tileMaterial, mesh, transform_float, mask_map[full_path]);
			}
			//bufs[full_path] = node;
		}

	}

}
