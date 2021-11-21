Shader "Custom/TileShader" {
    SubShader{

       Pass {
            CGPROGRAM


            uniform float4x4 transform;
            uniform float uv_offset_x;
            uniform float uv_offset_y;
            uniform float uv_scale_x;
            uniform float uv_scale_y;
            uniform float octant_mask[8];
            uniform sampler2D maptexture;

            struct appdata_t {
                float3 position   : POSITION;
                float2 octant : TEXCOORD0;
                float2 texcoords : TEXCOORD1;
            };

            StructuredBuffer<appdata_t> vertices;
            StructuredBuffer<int> indexes;

            struct v2f {
                float4 gl_Position  : SV_POSITION;
                float2 v_texcoords : TEXCOORD0;
            };

            #pragma vertex vert
            #pragma fragment frag

            v2f vert(uint id : SV_VertexID){
                v2f o;

                int arrayid = 0;
                if (id % 6 == 0) arrayid = id / 3;
                else if (id % 6 == 1) arrayid = id / 3+2;
                else if (id % 6 == 2) arrayid = id / 3+1;
                else if (id % 6 == 3) arrayid = id / 3;
                else if (id % 6 == 4) arrayid = id / 3+1;
                else arrayid = id / 3+2;

                float mask = octant_mask[int(vertices[indexes[arrayid]].octant.x)] ? 0.0f : 1.0f;
                o.v_texcoords = (vertices[indexes[arrayid]].texcoords + float2(uv_offset_x, uv_offset_y)) * float2(uv_scale_x, uv_scale_y) * mask;
                o.gl_Position = mul(transform , float4(vertices[indexes[arrayid]].position.xyz, 1.0) )*mask;
                o.gl_Position.y *= -1;
                o.gl_Position.z = (-o.gl_Position.z / o.gl_Position.w + 1.0) / 2.0 * o.gl_Position.w;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                return float4(tex2D(maptexture, float2(i.v_texcoords.x,1-i.v_texcoords.y) ).rgb, 1.0);
            }

            ENDCG
        }
    }
}