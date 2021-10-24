Shader "Custom/TileShader" {
    SubShader{
        Pass {
            CGPROGRAM

            uniform float4x4 transform;
            uniform float2 uv_offset;
            uniform float2 uv_scale;
            uniform bool octant_mask[8];
            uniform sampler2D maptexture;

            struct appdata_t {
                float3 position   : POSITION;
                float octant : TEXCOORD0;
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
                float mask = octant_mask[int(i.octant)] ? 0.0f : 1.0f;
                o.v_texcoords = (i.texcoords + uv_offset) * uv_scale * mask;
                o.gl_Position = mul(transform , float4(i.position.xyz, 1.0) )*mask;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                return float4(tex2D(maptexture, i.v_texcoords).rgb, 1.0);
            }

            ENDCG
        }
    }
}