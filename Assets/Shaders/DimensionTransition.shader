// 维度切换转场 shader（任务 11）
// 两种模式（_Mode）：
//   0 = 扫屏：一条光边沿 _Direction 推进，已过区域压暗，边缘带亮线
//   1 = 撕裂：画面按行分块错位，块数/幅度随 _Progress 衰减，重组出新世界
// 为什么 progress<=0||>=1 直接返回原图：转场播放外的时间走全屏 Pass 也必须零改动（防残留）。
// 为什么全屏三角形（SV_VertexID 合成）：CommandBuffer.DrawProcedural 无顶点缓冲，
//   三个顶点铺满视口，配合显式 SetRenderTarget/SetViewport 杜绝 cmd.Blit 的隐式视口残留（1/4 屏 bug 根因）。
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

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        struct Attributes { uint vertexID : SV_VertexID; };
        struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

        TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
        float _Progress; float _Mode; float4 _Direction; float _Width;
        float _BlockCount; float _Amplitude; float _Darken;

        // 全屏三角形：顶点 0/1/2 合成 (0,0)(2,0)(0,2) → NDC 覆盖 [-1,1]²
        Varyings vert(Attributes input)
        {
            Varyings o;
            float2 pos = float2((input.vertexID << 1) & 2, input.vertexID & 2);
            o.positionCS = float4(pos * float2(2.0, -2.0) + float2(-1.0, 1.0), 0.0, 1.0);
            o.uv = pos;
            return o;
        }
        ENDHLSL

        Pass
        {
            Name "Transition"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragTransition
            half4 fragTransition(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                // 采样坐标直接透传：全屏三角形 uv 与 RenderTexture 采样方向一致（uv(0,0)=屏幕左上、v=0=画面顶部），
                // 不要做 y 翻转——翻转是早期 cmd.Blit 路径的遗留（其内部 quad uv 约定相反），会导致画面上下颠倒。
                float2 suv = uv;
                float prog = saturate(_Progress);

                // 播放范围外：原样返回（防残留）
                if (prog <= 0.0001 || prog >= 0.9999)
                {
                    return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, suv);
                }

                float4 col;
                if (_Mode < 0.5)
                {
                    // ===== 扫屏模式 =====
                    // 沿方向推进：t 从 -0.5 走到 +0.5，edge>0 表示已扫过
                    float t = lerp(-0.5, 0.5, prog);
                    float edge = dot(uv - 0.5, normalize(_Direction.xy)) - t;
                    // 光带：edge≈0 处最亮（线性衰减，避免 smoothstep 逆序边界的未定义行为）
                    float band = (1.0 - saturate(abs(edge) / max(_Width, 0.001))) * 0.8;
                    col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, suv);
                    col.rgb = lerp(col.rgb, col.rgb * (1.0 - _Darken), step(0.0, edge)); // 已扫区压暗
                    col.rgb += band;
                    return col;
                }
                else
                {
                    // ===== 撕裂模式 =====
                    // 行块错位：块数随进度由多归 1（碎裂 → 重组），幅度同步衰减
                    float blocks = lerp(_BlockCount, 1.0, prog);
                    float idx = floor(suv.y * blocks);
                    float n = frac(sin(idx * 127.1 + prog * 31.7) * 43758.5453) - 0.5;
                    float offset = n * _Amplitude * (1.0 - prog);
                    // clamp 而非 frac：frac 会折叠采样坐标导致画面呈现多个"缩小版"
                    float2 tsuv = float2(saturate(suv.x + offset), suv.y);
                    col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, tsuv);
                    float seam = 1.0 - saturate(abs(offset) * 30.0);
                    col.rgb += seam * 0.5;   // 裂缝亮线
                    col.rgb *= lerp(1.0, 0.7, prog); // 整体轻度压暗增加故障感
                    return col;
                }
            }
            ENDHLSL
        }
    }
}