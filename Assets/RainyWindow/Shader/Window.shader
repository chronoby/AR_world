Shader "Unlit/Window"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Size("Size", float) = 1
        _T("Time", float) = 1
        _Distortion("Distortion", range(-5, 5)) = 0
        _Blur("Blur", range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Transparent" }
        LOD 100

        GrabPass { "_GrabTexture" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            #define S(a, b, t) smoothstep(a, b, t)

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 grabuv : TEXCOORD1;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex, _GrabTexture;
            float4 _MainTex_ST;
            float _Size, _T, _Distortion, _Blur;
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.grabuv = UNITY_PROJ_COORD(ComputeGrabScreenPos(o.vertex));
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            float N21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            float3 layer(float2 UV, float t)
            {
                float2 aspect = float2(2, 1);
                float2 uv = UV * _Size * aspect;
                uv.y += t * 0.25;
                float2 gv = frac(uv) - 0.5;
                float2 id = floor(uv);

                float n = N21(id); // rand(0, 1)
                t += n * 6.28;
                
                float w = UV.y * 10;

                float x = (n - 0.5) * 0.8;
                x += (0.4 - abs(x)) * sin(3 * w) * pow(sin(w), 6) * 0.45;
                float y = -0.45 * sin(t + sin(t + sin(t) * 0.5));
                y -= (gv.x - x) * (gv.x - x);

                float2 drop_pos = (gv - float2(x, y)) / aspect;
                float drop = S(0.05, 0.03, length(drop_pos));

                float2 trail_pos = (gv - float2(x, t * 0.25)) / aspect;
                trail_pos.y = (frac(trail_pos.y * 8) - 0.5) / 8;
                float trail = S(0.03, 0.01, length(trail_pos));
                float fog_trail = S(-0.05, 0.05, drop_pos.y);
                fog_trail *= S(0.5, y, gv.y);
                trail *= fog_trail;
                fog_trail *= S(0.05, 0.04, abs(drop_pos.x));

                float2 offs = drop * drop_pos + trail * trail_pos;

                return float3(offs, fog_trail);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float t = fmod(_Time.y + _T, 7200);
                float4 col = 0;

                float3 drops = layer(i.uv, t);
                drops += layer(i.uv * 1.23 + 7.54, t);
                drops += layer(i.uv * 1.35 + 1.54, t);
                drops += layer(i.uv * 1.57 + 7.54, t);

                float fade = 1 - saturate(fwidth(i.uv) * 60);

                float blur = _Blur * 7 * (1 - drops.z * fade);
            
                float2 projuv = i.grabuv.xy / i.grabuv.w;
                projuv += drops.xy * _Distortion * fade;
                blur *= 0.01;
                float a = N21(i.uv) * 6.2831;
                const float num_samples = 4;
                for(float i = 0; i < num_samples; ++i)
                {
                    float2 offs = float2(sin(a), cos(a)) * blur;
                    float d = frac(sin((i + 1)* 524.0) * 5424.0);
                    d = sqrt(d);
                    offs *= d;
                    col += tex2D(_GrabTexture, projuv + offs);
                    a++;
                }
                col /= num_samples;
                
                return col * 0.9;
            }
            ENDCG
        }
    }
}
