Shader "Custom/PointCloud" {
    Properties {
        _PointSize("Point Size", Range(0.1, 10)) = 2.0
        _Color("Color", Color) = (1,1,1,1)
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.0

            struct appdata {
                float4 vertex : POSITION;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float size : PSIZE;
            };

            float _PointSize;
            fixed4 _Color;

            v2f vert (appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.size = _PointSize;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                return _Color;
            }
            ENDCG
        }
    }
}