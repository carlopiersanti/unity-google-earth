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

            struct v2f {
                float4 gl_Position  : SV_POSITION;
                float2 v_texcoords : TEXCOORD0;
            };

            #pragma vertex vert
            #pragma fragment frag

            v2f vert(appdata_t i){
                v2f o;
                float mask = octant_mask[int(i.octant.x)] ? 0.0f : 1.0f;
                o.v_texcoords = (i.texcoords + float2(uv_offset_x, uv_offset_y)) * float2(uv_scale_x, uv_scale_y) * mask;
                o.gl_Position = mul(transform , float4(i.position.xyz, 1.0) )*mask;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                return float4(tex2D(maptexture, float2(i.v_texcoords.x,1- i.v_texcoords.y) ).rgb, 1.0);
            }

            ENDCG
        }
    }
}