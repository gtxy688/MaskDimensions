// 维度切换转场 shader（任务 11）
// 两种模式（_Mode）：
//   0 = 扫屏：一条光边沿 _Direction 推进，已过区域压暗，边缘带亮线
//   1 = 撕裂：画面按行分块错位，块数/幅度随 _Progress 衰减，重组出新世界
// 为什么 progress<=0||>=1 直接返回原图：转场播放外的时间走全屏 Pass 也必须零改动（防残留）。
Shader "Hidden/DimensionTransition"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Progress ("Progress", Range(0,1)) = 0
        _Mode ("Mode", Float) = 1
        _Direction ("Direction", Vector) = (1,0,0,0)
        _Width ("Width", Range(0,1)) = 0.15
        _BlockCount ("Block Count", Float) = 10
        _Amplitude ("Amplitude", Range(0,0.5)) = 0.12
        _Darken ("Darken", Range(0,1)) = 0.85
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            // 采样纹理：老式 cmd.Blit 会把源绑定到 _MainTex（Blitter 才用 _BlitTexture——本 Pass 用 cmd.Blit，故采样 _MainTex）
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float _Progress; float _Mode; float4 _Direction; float _Width;
            float _BlockCount; float _Amplitude; float _Darken;

            // blit 全屏四边形时顶点已是裁剪空间，直接透传（不能再过 TransformObjectToHClip）
            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionCS = input.positionOS;
                o.uv = input.uv;
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float prog = saturate(_Progress);

                // 播放范围外：原样返回（防残留）
                if (prog <= 0.0001 || prog >= 0.9999)
                {
                    return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                }

                float4 col;
                if (_Mode < 0.5)
                {
                    // ===== 扫屏模式 =====
                    // 沿方向推进：t 从 -0.5 走到 +0.5，edge>0 表示已扫过
                    float t = lerp(-0.5, 0.5, prog);
                    float edge = dot(uv - 0.5, normalize(_Direction.xy)) - t;
                    float band = smoothstep(_Width, 0.0, abs(edge)) * 0.8;   // 边缘亮带
                    col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                    col.rgb = lerp(col.rgb, col.rgb * (1.0 - _Darken), step(0.0, edge)); // 已扫区压暗
                    col.rgb += band;
                    return col;
                }
                else
                {
                    // ===== 撕裂模式 =====
                    // 行块错位：块数随进度由多归 1（碎裂 → 重组），幅度同步衰减
                    float blocks = lerp(_BlockCount, 1.0, prog);
                    float idx = floor(uv.y * blocks);
                    float n = frac(sin(idx * 127.1 + prog * 31.7) * 43758.5453) - 0.5;
                    float offset = n * _Amplitude * (1.0 - prog);
                    uv.x = frac(uv.x + offset);
                    // 裂缝处画亮线
                    col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                    float seam = 1.0 - saturate(abs(offset) * 30.0);
                    col.rgb += seam * 0.5;
                    // 整体轻度压暗增加故障感
                    col.rgb *= lerp(1.0, 0.7, prog);
                    return col;
                }
            }
            ENDHLSL
        }
    }
}