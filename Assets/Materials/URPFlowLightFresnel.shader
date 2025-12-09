Shader "Custom/URPFlowLightFresnel"
{
    Properties
    {
        [Header(Base Settings)]
        _MainColor ("Color Tone", Color) = (1, 1, 1, 1)
        _Scale ("Net Scale", Float) = 3.0
        _Speed ("Animation Speed", Float) = 1.0
        
        [Header(Detail Settings)]
        // --- 新增：控制线条粗细 ---
        _LineWidth ("Line Width", Range(0.001, 0.05)) = 0.005 
        // --- 新增：控制点的大小和亮度 ---
        _PointBrightness ("Point Brightness", Range(0.001, 0.1)) = 0.02 
        
        [Header(Glow Settings)]
        [HDR] _EmissionColor ("Emission Color", Color) = (0, 1, 1, 1)
        _GlowPower ("Glow Intensity", Range(0, 10)) = 2.0

        [Header(Fresnel Settings)]
        [HDR] _FresnelColor ("Fresnel Color", Color) = (0, 1, 1, 1)
        _FresnelPower ("Fresnel Power", Range(0.1, 10.0)) = 3.0
        _FresnelBias ("Fresnel Bias", Range(0.0, 1.0)) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 viewDirWS  : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainColor;
                float4 _EmissionColor;
                float _Scale;
                float _Speed;
                float _GlowPower;
                
                float _LineWidth;
                float _PointBrightness;

                float4 _FresnelColor;
                float _FresnelPower;
                float _FresnelBias;
            CBUFFER_END

            #define S(a, b, t) smoothstep(a, b, t)
            #define NUM_LAYERS 2.0

            float N21(float2 p) {
                float3 a = frac(float3(p.xyx) * float3(213.897, 653.453, 253.098));
                a += dot(a, a.yzx + 79.76);
                return frac((a.x + a.y) * a.z);
            }

            float2 GetPos(float2 id, float2 offs, float t) {
                float n = N21(id + offs);
                float n1 = frac(n * 10.0);
                float n2 = frac(n * 100.0);
                float a = t + n;
                return offs + float2(sin(a * n1), cos(a * n2)) * 0.4;
            }

            float df_line(float2 a, float2 b, float2 p) {
                float2 pa = p - a, ba = b - a;
                float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
                return length(pa - ba * h);
            }

            // --- 修改了画线函数 ---
            float line_func(float2 a, float2 b, float2 uv) {
                // 使用 _LineWidth 变量替代原来的 0.04
                float r1 = _LineWidth; 
                float r2 = _LineWidth * 0.25; // 核心比边缘更细
                
                float d = df_line(a, b, uv);
                float d2 = length(a - b);
                float fade = S(1.5, 0.5, d2);
                fade += S(0.05, 0.02, abs(d2 - 0.75));
                return S(r1, r2, d) * fade;
            }

            // --- 修改了网格层函数 ---
            float NetLayer(float2 st, float n, float t) {
                float2 id = floor(st) + n;
                st = frac(st) - 0.5;
                float2 p[9];
                int i = 0;
                for(float y = -1.0; y <= 1.0; y++) {
                    for(float x = -1.0; x <= 1.0; x++) {
                        p[i++] = GetPos(id, float2(x, y), t);
                    }
                }
                float m = 0.0;
                float sparkle = 0.0;
                for(int j = 0; j < 9; j++) {
                    m += line_func(p[4], p[j], st);
                    float d = length(st - p[j]);
                    
                    // --- 修改了光点亮度 ---
                    // 使用 _PointBrightness 替代原来的 0.005
                    float s = (_PointBrightness / (d * d)); 
                    s *= S(1.0, 0.7, d);
                    float pulse = sin((frac(p[j].x) + frac(p[j].y) + t) * 5.0) * 0.4 + 0.6;
                    pulse = pow(pulse, 20.0);
                    s *= pulse;
                    sparkle += s;
                }
                m += line_func(p[1], p[3], st);
                m += line_func(p[1], p[5], st);
                m += line_func(p[7], p[5], st);
                m += line_func(p[7], p[3], st);
                float sPhase = (sin(t + n) + sin(t * 0.1)) * 0.25 + 0.5;
                sPhase += pow(sin(t * 0.1) * 0.5 + 0.5, 50.0) * 5.0;
                m += sparkle * sPhase;
                return m;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                output.viewDirWS = GetWorldSpaceViewDir(worldPos);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = (input.uv - 0.5) * _Scale;
                float t = _Time.y * _Speed * 0.1;
                float s = sin(t);
                float c = cos(t);
                float2x2 rot = float2x2(c, -s, s, c);
                float2 st = mul(rot, uv); 
                
                float m = 0.0;
                for(float i = 0.0; i < 1.0; i += 1.0 / NUM_LAYERS) {
                    float z = frac(t + i);
                    float size = lerp(15.0, 1.0, z);
                    float fade = S(0.0, 0.6, z) * S(1.0, 0.8, z);
                    m += fade * NetLayer(st * size, i, _Time.y * _Speed);
                }
                
                float3 baseCol = float3(s, cos(t * 0.4), -sin(t * 0.24)) * 0.4 + 0.6;
                baseCol *= _MainColor.rgb;
                float3 netColor = baseCol * m * _EmissionColor.rgb * _GlowPower;
                
                float3 N = normalize(input.normalWS);
                float3 V = normalize(input.viewDirWS);
                float fresnelFactor = pow(1.0 - saturate(dot(N, V)), _FresnelPower);
                fresnelFactor = saturate(_FresnelBias + (1.0 - _FresnelBias) * fresnelFactor);
                float3 fresnelColor = fresnelFactor * _FresnelColor.rgb;

                float3 finalColor = netColor + fresnelColor;
                float dist = length(input.uv - 0.5);
                finalColor *= 1.0 - dot(dist, dist);

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}