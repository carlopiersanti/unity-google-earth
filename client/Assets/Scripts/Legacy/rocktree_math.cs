using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class rocktree_math
{

	// based on @fabioarnold's impl of "https://fgiesen.wordpress.com/2012/08/31/frustum-planes-from-the-projection-matrix/"
	public static Vector4[] getFrustumPlanes(Matrix projection)
	{
		Vector4[] planes = new Vector4[6]
		{
			new Vector4(),
			new Vector4(),
			new Vector4(),
			new Vector4(),
			new Vector4(),
			new Vector4()
		};

		for (int i = 0; i < 3; ++i)
		{
			planes[i + 0] = projection.GetRow(3)+ projection.GetRow(i);
			planes[i + 3] = projection.GetRow(3)-projection.GetRow(i);
		}
		return planes;
	}

	public enum obb_frustum : int
	{
		obb_frustum_inside = -1,
		obb_frustum_intersect = 0,
		obb_frustum_outside = 1,
	};



	// based on @fabioarnold's impl of "Real-Time Collision Detection 5.2.3 Testing Box Against Plane"
	public static obb_frustum classifyObbFrustum(rocktree_decoder.OrientedBoundingBox obb, Vector4[] planes)
	{
		obb_frustum result = obb_frustum.obb_frustum_inside;
		var obb_orientation_t = Matrix.Transpose(obb.orientation);
		for (int i = 0; i < 6; i++)
		{
			var plane4 = planes[i];
			var plane3 = new Vector3();
			plane3.mat[0, 0] = plane4.mat[0, 0];
			plane3.mat[1, 0] = plane4.mat[1, 0];
			plane3.mat[2, 0] = plane4.mat[2, 0];
			var mul = (obb_orientation_t * plane3);
			Matrix abs_plane = Matrix.Abs(mul);

			double r = Matrix.dot( obb.extents, abs_plane);
			double d = Matrix.dot(obb.center, plane3) + plane4.mat[3, 0];

			if (Math.Abs(d) < r) result = obb_frustum.obb_frustum_intersect;
			if (d + r < 0.0f) return obb_frustum.obb_frustum_outside;
		}
		return result;
	}

	/*
	Matrix4d perspective(double fov_rad, double aspect_ratio, double near, double far)
	{
		assert(aspect_ratio > 0);
		assert(far > near);

		auto tan_half_fovy = tan(fov_rad / 2.0);

		Matrix4d res = Matrix4d::Zero();
		res(0, 0) = 1.0 / (aspect_ratio * tan_half_fovy);
		res(1, 1) = 1.0 / (tan_half_fovy);
		res(2, 2) = -(far + near) / (far - near);
		res(3, 2) = -1.0;
		res(2, 3) = -(2.0 * far * near) / (far - near);
		return res;
	}

	Matrix4d lookAt(Vector3d eye, Vector3d center, Vector3d up)
	{

		auto f = (center - eye).normalized();
		auto u = up.normalized();
		auto s = f.cross(u).normalized();
		u = s.cross(f);

		Matrix4d res;
		res << s.x(),s.y(),s.z(),-s.dot(eye),
           u.x(),u.y(),u.z(),-u.dot(eye),
           -f.x(),-f.y(),-f.z(),f.dot(eye),
           0,0,0,1;

		return res;
	}*/


}
