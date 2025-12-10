Shader "Aurora/AuroraSky_URP"
{
    Properties
    {
        [Header(Sky Setting)]
        _Color1 ("Top Color", Color) = (0.0, 0.0, 0.2, 1)
        _Color2 ("Horizon Color", Color) = (0.2, 0.4, 0.6, 1)
        _Color3 ("Bottom Color", Color) = (0.0, 0.1, 0.2, 1)
        _Exponent1 ("Exponent Factor for Top Half", Float) = 1.0
        _Exponent2 ("Exponent Factor for Bottom Half", Float) = 1.0
        _Intensity ("Intensity Amplifier", Float) = 1.0

        [Header(Star Setting)]
        [HDR]_StarColor ("Star Color", Color) = (1,1,1,1)
        _StarIntensity("Star Intensity", Range(0,1)) = 0.5
        _StarSpeed("Star Speed", Range(0,1)) = 0.1

        [Header(Cloud Setting)]
        [HDR]_CloudColor ("Cloud Color", Color) = (1,1,1,1)
        _CloudIntensity("Cloud Intensity", Range(0,1)) = 0.5
        _CloudSpeed("CloudSpeed", Range(0,1)) = 0.1

        [Header(Aurora Setting)]
        [HDR]_AuroraColor ("Aurora Color", Color) = (0,1,0.5,1)
        _AuroraIntensity("Aurora Intensity", Range(0,1)) = 0.5
        _AuroraSpeed("AuroraSpeed", Range(0,1)) = 0.5
        _SurAuroraColFactor("Sur Aurora Color Factor", Range(0,1)) = 0.5
        
        // --- 已移除 Environment Setting (Mountain) ---
    }

    SubShader
    {
        Tags { "RenderType"="Background" "Queue"="Background" "PreviewType"="Skybox" "RenderPipeline" = "UniversalPipeline" }
        
        Pass
        {
            ZWrite Off
            Cull Off
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                // float3 uv : TEXCOORD0; // 不需要了
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 texcoord : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color1;
                half4 _Color2;
                half4 _Color3;
                half _Intensity;
                half _Exponent1;
                half _Exponent2;

                half4 _StarColor;
                half _StarIntensity;
                half _StarSpeed;

                half4 _CloudColor;
                half _CloudIntensity;
                half _CloudSpeed;

                half4 _AuroraColor;
                half _AuroraIntensity;
                half _AuroraSpeed;
                half _SurAuroraColFactor;
                
                // --- 已移除 Mountain 相关的变量声明 ---
                // half4 _MountainColor;
                // float _MountainFactor;
                // half _MountainHeight;
            CBUFFER_END

            // --- 核心算法部分 (保持不变) ---

            // 星空散列哈希
            float StarAuroraHash(float3 x) {
                float3 p = float3(dot(x,float3(214.1 ,127.7,125.4)),
                                  dot(x,float3(260.5,183.3,954.2)),
                                  dot(x,float3(209.5,571.3,961.2)) );
                return -0.001 + _StarIntensity * frac(sin(p) * 43758.5453123);
            }

            // 星空噪声
            float StarNoise(float3 st){
                st += float3(0, _Time.y * _StarSpeed, 0);
                float3 i = floor(st);
                float3 f = frac(st);
                float3 u = f*f*(3.0-1.0*f);
                return lerp(lerp(dot(StarAuroraHash( i + float3(0.0,0.0,0.0)), f - float3(0.0,0.0,0.0) ), 
                                 dot(StarAuroraHash( i + float3(1.0,0.0,0.0)), f - float3(1.0,0.0,0.0) ), u.x),
                            lerp(dot(StarAuroraHash( i + float3(0.0,1.0,0.0)), f - float3(0.0,1.0,0.0) ), 
                                 dot(StarAuroraHash( i + float3(1.0,1.0,0.0)), f - float3(1.0,1.0,0.0) ), u.y), u.z);
            }

            // 云散列哈希
            float CloudHash (float2 st) {
                return frac(sin(dot(st.xy, float2(12.9898,78.233))) * 43758.5453123);
            }

            // 云噪声
            float CloudNoise (float2 st, int flow) {
                st += float2(0, _Time.y * _CloudSpeed * flow);
                float2 i = floor(st);
                float2 f = frac(st);
                float a = CloudHash(i);
                float b = CloudHash(i + float2(1.0, 0.0));
                float c = CloudHash(i + float2(0.0, 1.0));
                float d = CloudHash(i + float2(1.0, 1.0));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(a, b, u.x) + (c - a)* u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
            }

            // 云分型
            float Cloudfbm (float2 st, int flow) {
                float value = 0.0;
                float amplitude = .5;
                for (int i = 0; i < 6; i++) {
                    value += amplitude * CloudNoise(st,flow);
                    st *= 2.;
                    amplitude *= .5;
                }
                return value;
            }

            // 极光噪声
            float AuroraHash(float n) { 
                return frac(sin(n)*758.5453); 
            }

            float AuroraNoise(float3 x) {
                float3 p = floor(x);
                float3 f = frac(x);
                float n = p.x + p.y*57.0 + p.z*800.0;
                float res = lerp(lerp(lerp( AuroraHash(n+  0.0), AuroraHash(n+  1.0),f.x), lerp( AuroraHash(n+ 57.0), AuroraHash(n+ 58.0),f.x),f.y),
                            lerp(lerp( AuroraHash(n+800.0), AuroraHash(n+801.0),f.x), lerp( AuroraHash(n+857.0), AuroraHash(n+858.0),f.x),f.y),f.z);
                return res;
            }

            // 极光分型
            float Aurorafbm(float3 p) {
                float f  = 0.50000*AuroraNoise( p ); p *= 2.02;
                f += 0.25000*AuroraNoise( p ); p *= 2.03;
                f += 0.12500*AuroraNoise( p ); p *= 2.01;
                f += 0.06250*AuroraNoise( p ); p *= 2.04;
                f += 0.03125*AuroraNoise( p );
                return f*1.032258;
            }

            float GetAurora(float3 p) {
                p += Aurorafbm(float3(p.x,p.y,0.0)*0.5)*2.25;
                float a = smoothstep(.0, .9, Aurorafbm(p*2.)*2.2-1.1);
                return a<0.0 ? 0.0 : a;
            }

            // --- 带状极光 ---
            float2x2 RotateMatrix(float a){
                float c = cos(a);
                float s = sin(a);
                return float2x2(c,s,-s,c);
            }

            float tri(float x){
                return clamp(abs(frac(x)-0.5),0.01,0.49);
            }

            float2 tri2(float2 p){
                return float2(tri(p.x)+tri(p.y),tri(p.y+tri(p.x)));
            }

            float SurAuroraNoise(float2 pos) {
                float intensity=1.8;
                float size=2.5;
                float rz = 0;
                pos = mul(RotateMatrix(pos.x*0.06),pos);
                float2 bp = pos;
                for (int i=0; i<5; i++) {
                    float2 dg = tri2(bp*1.85)*.75;
                    dg = mul(RotateMatrix(_Time.y*_AuroraSpeed),dg);
                    pos -= dg/size;
                    bp *= 1.3;
                    size *= .45;
                    intensity *= .42;
                    pos *= 1.21 + (rz-1.0)*.02;
                    rz += tri(pos.x+tri(pos.y))*intensity;
                    pos = mul(-float2x2(0.95534, 0.29552, -0.29552, 0.95534),pos);
                }
                return clamp(1.0/pow(rz*29., 1.3),0,0.55);
            }

            float SurHash(float2 n){
                 return frac(sin(dot(n, float2(12.9898, 4.1414))) * 43758.5453); 
            }

            float4 SurAurora(float3 pos, float3 ro) {
                float4 col = float4(0,0,0,0);
                float4 avgCol = float4(0,0,0,0);
                for(int i=0;i<30;i++) {
                    float of = 0.006*SurHash(pos.xy)*smoothstep(0,15, i);       
                    float pt = ((0.8+pow(i,1.4)*0.002)-ro.y)/(pos.y*2.0+0.8);
                    pt -= of;
                    float3 bpos = ro + pt*pos;
                    float2 p = bpos.zx;
                    float noise = SurAuroraNoise(p);
                    float4 col2 = float4(0,0,0, noise);
                    col2.rgb = (sin(1.0-float3(2.15,-.5, 1.2)+i*_SurAuroraColFactor*0.1)*0.8+0.5)*noise;
                    avgCol =  lerp(avgCol, col2, 0.5);
                    col += avgCol*exp2(-i*0.065 - 2.5)*smoothstep(0.,5., i);
                }
                col *= (clamp(pos.y*15.+.4,0.,1.));
                return col*1.8;
            }

            // --- 顶点 & 片元 ---

            Varyings vert (Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                // 使用对象空间位置作为方向
                output.texcoord = input.positionOS.xyz;
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                float3 dir = normalize(input.texcoord);
                float p = dir.y;
                
                // 计算渐变背景
                float p1 = 1.0f - pow (min (1.0f, 1.0f - p), _Exponent1);
                float p3 = 1.0f - pow (min (1.0f, 1.0f + p), _Exponent2);
                float p2 = 1.0f - p1 - p3;
                // 这里的 reflection 变量现在只用于调整地平线以下的星星亮度和极光偏移，不再用于水面计算
                int reflection = dir.y < 0 ? -1 : 1;

                // Cloud
                float cloud = Cloudfbm(half2(dir.x, dir.z) * 8, 1);
                float4 cloudCol = float4(cloud * _CloudColor.rgb, cloud*0.8) * _CloudIntensity;

                // Stars
                float star = StarNoise(half3(dir.x, dir.y * reflection, dir.z) * 64);
                float4 starOriCol = float4(_StarColor.r + 3.25*sin(dir.x) + 2.45 * (sin(_Time.y * _StarSpeed) + 1)*0.5,
                                           _StarColor.g + 3.85*sin(dir.y) + 1.45 * (sin(_Time.y * _StarSpeed) + 1)*0.5,
                                           _StarColor.b + 3.45*sin(dir.z) + 4.45 * (sin(_Time.y * _StarSpeed) + 1)*0.5,
                                           _StarColor.a + 3.85*star);
                star = star > 0.8 ? star : smoothstep(0.81, 0.98, star);
                float4 starCol = half4((starOriCol * star).rgb, star);

                // SurAurora (带状极光)
                float4 surAuroraCol = smoothstep(0.0, 1.5, SurAurora(
                                                    float3(dir.x, abs(dir.y), dir.z),
                                                    float3(0, 0, -6.7)
                                                    )) + (reflection-1)*-0.2*0.5;

                // Aurora (普通极光)
                float3 aurora = float3 (_AuroraColor.rgb * GetAurora(float3(dir.xy*float2(1.2,1.0), _Time.y*_AuroraSpeed*0.22)) * 0.9 +
                                        _AuroraColor.rgb * GetAurora(float3(dir.xy*float2(1.0,0.7), _Time.y*_AuroraSpeed*0.27)) * 0.9 +
                                        _AuroraColor.rgb * GetAurora(float3(dir.xy*float2(0.8,0.6), _Time.y*_AuroraSpeed*0.29)) * 0.5 +
                                        _AuroraColor.rgb * GetAurora(float3(dir.xy*float2(0.9,0.5), _Time.y*_AuroraSpeed*0.20)) * 0.57);
                float4 auroraCol = float4(aurora, 0.1354);

                // --- 已移除 Mountain 计算逻辑 ---
                // float mountain = ...
                // float4 mountainCol = ...

                // Blend (混合)
                float4 skyCol = (_Color1 * p1 + _Color2 * p2 + _Color3 * p3) * _Intensity;
                // 地平线以下星星变暗
                starCol = reflection==1 ? starCol : starCol * 0.5; 
                skyCol = skyCol*(1 - starCol.a) + starCol * starCol.a;
                skyCol = skyCol*(1 - cloudCol.a) + cloudCol * cloudCol.a;
                skyCol = skyCol*(1 - surAuroraCol.a) + surAuroraCol * surAuroraCol.a;
                skyCol = skyCol*(1 - auroraCol.a) + auroraCol * auroraCol.a;
                
                // --- 已移除 Mountain 混合逻辑 ---
                // skyCol = skyCol*(1 - mountainCol.a) + mountainCol * mountainCol.a;

                // --- 已移除 Reflection (水面反射) 逻辑块 ---
                // if(reflection == -1){ ... }

                return skyCol;
            }
            ENDHLSL
        }
    }
}