// ChannelPackedMasks - custom_insert.hlsl
// Functions that depend on Unity/lilToon symbols.
// This file is included AFTER all uniform declarations.
//
// Each mask reads a user-selectable channel (R/G/B/A) via _CPM_*Channel properties.
// Color application from mask textures is disabled — only scalar masking is applied.

//------------------------------------------------------------------------------------------------------------------------------
// Channel Selection Helper
//------------------------------------------------------------------------------------------------------------------------------
float lilChannelSelect(float4 tex, uint ch)
{
    if(ch == 0) return tex.r;
    if(ch == 1) return tex.g;
    if(ch == 2) return tex.b;
    return tex.a;
}

//------------------------------------------------------------------------------------------------------------------------------
// 1. Emission 1st (Channel Packed)
// Original: emissionColor *= LIL_SAMPLE_2D_ST(_EmissionBlendMask, ...) — applies RGBA (color + mask)
// Modified: emissionColor *= selected channel only (scalar mask, no color)
//------------------------------------------------------------------------------------------------------------------------------
#if defined(LIL_FEATURE_EMISSION_1ST) && !defined(LIL_LITE) && !defined(LIL_FAKESHADOW)
    void lilEmissionChannelPacked(inout lilFragData fd LIL_SAMP_IN_FUNC(samp))
    {
        if(_UseEmission)
        {
            float4 emissionColor = _EmissionColor;
            // UV
            float2 emissionUV = fd.uv0;
            if(_EmissionMap_UVMode == 1) emissionUV = fd.uv1;
            if(_EmissionMap_UVMode == 2) emissionUV = fd.uv2;
            if(_EmissionMap_UVMode == 3) emissionUV = fd.uv3;
            if(_EmissionMap_UVMode == 4) emissionUV = fd.uvRim;
            float2 _EmissionMapParaTex = emissionUV + _EmissionParallaxDepth * fd.parallaxOffset;
            // Texture
            #if defined(LIL_FEATURE_EmissionMap)
                #if defined(LIL_FEATURE_ANIMATE_EMISSION_UV)
                    emissionColor *= LIL_GET_EMITEX(_EmissionMap, _EmissionMapParaTex);
                #else
                    emissionColor *= LIL_SAMPLE_2D_ST(_EmissionMap, sampler_EmissionMap, _EmissionMapParaTex);
                #endif
            #endif
            // Mask — Channel selection (RGB = default color+mask, R/G/B/A = scalar mask only)
            #if defined(LIL_FEATURE_EmissionBlendMask)
                #if defined(LIL_FEATURE_ANIMATE_EMISSION_MASK_UV)
                    float4 emissionMaskTex = LIL_GET_EMIMASK(_EmissionBlendMask, fd.uv0);
                #else
                    float4 emissionMaskTex = LIL_SAMPLE_2D_ST(_EmissionBlendMask, samp, fd.uvMain);
                #endif
                if(_CPM_Emission1stChannel >= 4)
                    emissionColor *= emissionMaskTex;
                else
                    emissionColor *= lilChannelSelect(emissionMaskTex, _CPM_Emission1stChannel);
            #endif
            // Gradation
            #if defined(LIL_FEATURE_EmissionGradTex)
                #if defined(LIL_FEATURE_EMISSION_GRADATION) && defined(LIL_FEATURE_AUDIOLINK)
                    if(_EmissionUseGrad)
                    {
                        float gradUV = _EmissionGradSpeed * LIL_TIME + fd.audioLinkValue * _AudioLink2EmissionGrad;
                        emissionColor *= LIL_SAMPLE_1D_LOD(_EmissionGradTex, lil_sampler_linear_repeat, gradUV, 0);
                    }
                #elif defined(LIL_FEATURE_EMISSION_GRADATION)
                    if(_EmissionUseGrad) emissionColor *= LIL_SAMPLE_1D(_EmissionGradTex, lil_sampler_linear_repeat, _EmissionGradSpeed * LIL_TIME);
                #endif
            #endif
            #if defined(LIL_FEATURE_AUDIOLINK)
                if(_AudioLink2Emission) emissionColor.a *= fd.audioLinkValue;
            #endif
            emissionColor.rgb = lerp(emissionColor.rgb, emissionColor.rgb * fd.invLighting, _EmissionFluorescence);
            emissionColor.rgb = lerp(emissionColor.rgb, emissionColor.rgb * fd.albedo, _EmissionMainStrength);
            float emissionBlend = _EmissionBlend * lilCalcBlink(_EmissionBlink) * emissionColor.a;
            #if LIL_RENDER == 2 && !defined(LIL_REFRACTION)
                emissionBlend *= fd.col.a;
            #endif
            fd.col.rgb = lilBlendColor(fd.col.rgb, emissionColor.rgb, emissionBlend, _EmissionBlendMode);
        }
    }
#endif

//------------------------------------------------------------------------------------------------------------------------------
// 2. Emission 2nd (Channel Packed)
// Original: emission2ndColor *= LIL_SAMPLE_2D_ST(_Emission2ndBlendMask, ...) — applies RGBA (color + mask)
// Modified: emission2ndColor *= selected channel only (scalar mask, no color)
//------------------------------------------------------------------------------------------------------------------------------
#if defined(LIL_FEATURE_EMISSION_2ND) && !defined(LIL_LITE) && !defined(LIL_FAKESHADOW)
    void lilEmission2ndChannelPacked(inout lilFragData fd LIL_SAMP_IN_FUNC(samp))
    {
        if(_UseEmission2nd)
        {
            float4 emission2ndColor = _Emission2ndColor;
            // UV
            float2 emission2ndUV = fd.uv0;
            if(_Emission2ndMap_UVMode == 1) emission2ndUV = fd.uv1;
            if(_Emission2ndMap_UVMode == 2) emission2ndUV = fd.uv2;
            if(_Emission2ndMap_UVMode == 3) emission2ndUV = fd.uv3;
            if(_Emission2ndMap_UVMode == 4) emission2ndUV = fd.uvRim;
            float2 _Emission2ndMapParaTex = emission2ndUV + _Emission2ndParallaxDepth * fd.parallaxOffset;
            // Texture
            #if defined(LIL_FEATURE_Emission2ndMap)
                #if defined(LIL_FEATURE_ANIMATE_EMISSION_UV)
                    emission2ndColor *= LIL_GET_EMITEX(_Emission2ndMap, _Emission2ndMapParaTex);
                #else
                    emission2ndColor *= LIL_SAMPLE_2D_ST(_Emission2ndMap, sampler_Emission2ndMap, _Emission2ndMapParaTex);
                #endif
            #endif
            // Mask — Channel selection (RGB = default color+mask, R/G/B/A = scalar mask only)
            #if defined(LIL_FEATURE_Emission2ndBlendMask)
                #if defined(LIL_FEATURE_ANIMATE_EMISSION_MASK_UV)
                    float4 emission2ndMaskTex = LIL_GET_EMIMASK(_Emission2ndBlendMask, fd.uv0);
                #else
                    float4 emission2ndMaskTex = LIL_SAMPLE_2D_ST(_Emission2ndBlendMask, samp, fd.uvMain);
                #endif
                if(_CPM_Emission2ndChannel >= 4)
                    emission2ndColor *= emission2ndMaskTex;
                else
                    emission2ndColor *= lilChannelSelect(emission2ndMaskTex, _CPM_Emission2ndChannel);
            #endif
            // Gradation
            #if defined(LIL_FEATURE_Emission2ndGradTex)
                #if defined(LIL_FEATURE_EMISSION_GRADATION) && defined(LIL_FEATURE_AUDIOLINK)
                    if(_Emission2ndUseGrad)
                    {
                        float gradUV = _Emission2ndGradSpeed * LIL_TIME + fd.audioLinkValue * _AudioLink2Emission2ndGrad;
                        emission2ndColor *= LIL_SAMPLE_1D_LOD(_Emission2ndGradTex, lil_sampler_linear_repeat, gradUV, 0);
                    }
                #elif defined(LIL_FEATURE_EMISSION_GRADATION)
                    if(_Emission2ndUseGrad) emission2ndColor *= LIL_SAMPLE_1D(_Emission2ndGradTex, lil_sampler_linear_repeat, _Emission2ndGradSpeed * LIL_TIME);
                #endif
            #endif
            #if defined(LIL_FEATURE_AUDIOLINK)
                if(_AudioLink2Emission2nd) emission2ndColor.a *= fd.audioLinkValue;
            #endif
            emission2ndColor.rgb = lerp(emission2ndColor.rgb, emission2ndColor.rgb * fd.invLighting, _Emission2ndFluorescence);
            emission2ndColor.rgb = lerp(emission2ndColor.rgb, emission2ndColor.rgb * fd.albedo, _Emission2ndMainStrength);
            float emission2ndBlend = _Emission2ndBlend * lilCalcBlink(_Emission2ndBlink) * emission2ndColor.a;
            #if LIL_RENDER == 2 && !defined(LIL_REFRACTION)
                emission2ndBlend *= fd.col.a;
            #endif
            fd.col.rgb = lilBlendColor(fd.col.rgb, emission2ndColor.rgb, emission2ndBlend, _Emission2ndBlendMode);
        }
    }
#endif

//------------------------------------------------------------------------------------------------------------------------------
// 3. Glitter (Channel Packed)
// Original: glitterColor *= LIL_SAMPLE_2D_ST(_GlitterColorTex, ...) — applies RGBA (color + mask)
// Modified: glitterColor *= selected channel only (scalar mask, no color)
//------------------------------------------------------------------------------------------------------------------------------
#if defined(LIL_FEATURE_GLITTER) && !defined(LIL_LITE) && !defined(LIL_FAKESHADOW)
    void lilGlitterChannelPacked(inout lilFragData fd LIL_SAMP_IN_FUNC(samp))
    {
        if(_UseGlitter)
        {
            // View direction
            float3 glitterViewDirection = lilBlendVRParallax(fd.headV, fd.V, _GlitterVRParallaxStrength);
            float3 glitterCameraDirection = lerp(fd.cameraFront, fd.V, _GlitterVRParallaxStrength);

            // Normal
            float3 N = fd.N;
            #if defined(LIL_FEATURE_NORMAL_1ST) || defined(LIL_FEATURE_NORMAL_2ND)
                N = lerp(fd.origN, fd.N, _GlitterNormalStrength);
            #endif

            // Color
            float4 glitterColor = _GlitterColor;
            // Mask — Channel selection (RGB = default color+mask, R/G/B/A = scalar mask only)
            #if defined(LIL_FEATURE_GlitterColorTex)
                float2 uvGlitterColor = fd.uvMain;
                if(_GlitterColorTex_UVMode == 1) uvGlitterColor = fd.uv1;
                if(_GlitterColorTex_UVMode == 2) uvGlitterColor = fd.uv2;
                if(_GlitterColorTex_UVMode == 3) uvGlitterColor = fd.uv3;
                float4 glitterMaskTex = LIL_SAMPLE_2D_ST(_GlitterColorTex, samp, uvGlitterColor);
                if(_CPM_GlitterChannel >= 4)
                    glitterColor *= glitterMaskTex;
                else
                    glitterColor *= lilChannelSelect(glitterMaskTex, _CPM_GlitterChannel);
            #endif
            float2 glitterPos = _GlitterUVMode ? fd.uv1 : fd.uv0;
            #if defined(LIL_FEATURE_GlitterShapeTex)
                glitterColor.rgb *= lilCalcGlitter(glitterPos, N, glitterViewDirection, glitterCameraDirection, fd.L, _GlitterParams1, _GlitterParams2, _GlitterPostContrast, _GlitterSensitivity, _GlitterScaleRandomize, _GlitterAngleRandomize, _GlitterApplyShape, _GlitterShapeTex, _GlitterShapeTex_ST, _GlitterAtras);
            #else
                glitterColor.rgb *= lilCalcGlitter(glitterPos, N, glitterViewDirection, glitterCameraDirection, fd.L, _GlitterParams1, _GlitterParams2, _GlitterPostContrast, _GlitterSensitivity, _GlitterScaleRandomize, 0, false, _GlitterShapeTex, float4(0,0,0,0), float4(1,1,0,0));
            #endif
            glitterColor.rgb = lerp(glitterColor.rgb, glitterColor.rgb * fd.albedo, _GlitterMainStrength);
            #if LIL_RENDER == 2 && !defined(LIL_REFRACTION)
                if(_GlitterApplyTransparency) glitterColor.a *= fd.col.a;
            #endif
            glitterColor.a = fd.facing < (_GlitterBackfaceMask-1.0) ? 0.0 : glitterColor.a;

            // Blend
            #if !defined(LIL_PASS_FORWARDADD)
                glitterColor.a = lerp(glitterColor.a, glitterColor.a * fd.shadowmix, _GlitterShadowMask);
                glitterColor.rgb = lerp(glitterColor.rgb, glitterColor.rgb * fd.lightColor, _GlitterEnableLighting);
                fd.col.rgb += glitterColor.rgb * glitterColor.a;
            #else
                glitterColor.a = lerp(glitterColor.a, glitterColor.a * fd.shadowmix, _GlitterShadowMask);
                fd.col.rgb += glitterColor.a * _GlitterEnableLighting * glitterColor.rgb * fd.lightColor;
            #endif
        }
    }
#endif

//------------------------------------------------------------------------------------------------------------------------------
// 4. MatCap 1st (Channel Packed)
// Original: matCapMask = LIL_SAMPLE_2D_ST(_MatCapBlendMask, ...).rgb — uses RGB as blend mask
// Modified: matCapMask = selected channel only (scalar mask)
//------------------------------------------------------------------------------------------------------------------------------
#if defined(LIL_FEATURE_MATCAP) && !defined(LIL_LITE) && !defined(LIL_FAKESHADOW)
    void lilGetMatCapChannelPacked(inout lilFragData fd LIL_SAMP_IN_FUNC(samp))
    {
        if(_UseMatCap)
        {
            // Normal
            float3 N = fd.matcapN;
            #if defined(LIL_FEATURE_NORMAL_1ST) || defined(LIL_FEATURE_NORMAL_2ND)
                N = lerp(fd.origN, fd.matcapN, _MatCapNormalStrength);
            #endif
            #if defined(LIL_FEATURE_MatCapBumpMap)
                if(_MatCapCustomNormal)
                {
                    float4 normalTex = LIL_SAMPLE_2D_ST(_MatCapBumpMap, samp, fd.uvMain);
                    float3 normalmap = lilUnpackNormalScale(normalTex, _MatCapBumpScale);
                    N = normalize(mul(normalmap, fd.TBN));
                    N = fd.facing < (_FlipNormal-1.0) ? -N : N;
                }
            #endif

            // UV
            float2 matUV = lilCalcMatCapUV(fd.uv1, normalize(N), fd.V, fd.headV, _MatCapTex_ST, _MatCapBlendUV1.xy, _MatCapZRotCancel, _MatCapPerspective, _MatCapVRParallaxStrength);

            // Color
            float4 matCapColor = _MatCapColor;
            #if defined(LIL_FEATURE_MatCapTex)
                matCapColor *= LIL_SAMPLE_2D_LOD(_MatCapTex, lil_sampler_linear_repeat, matUV, _MatCapLod);
            #endif
            #if !defined(LIL_PASS_FORWARDADD)
                matCapColor.rgb = lerp(matCapColor.rgb, matCapColor.rgb * fd.lightColor, _MatCapEnableLighting);
                matCapColor.a = lerp(matCapColor.a, matCapColor.a * fd.shadowmix, _MatCapShadowMask);
            #else
                if(_MatCapBlendMode < 3) matCapColor.rgb *= fd.lightColor * _MatCapEnableLighting;
                matCapColor.a = lerp(matCapColor.a, matCapColor.a * fd.shadowmix, _MatCapShadowMask);
            #endif
            #if LIL_RENDER == 2 && !defined(LIL_REFRACTION)
                if(_MatCapApplyTransparency) matCapColor.a *= fd.col.a;
            #endif
            matCapColor.a = fd.facing < (_MatCapBackfaceMask-1.0) ? 0.0 : matCapColor.a;
            // Mask — Channel selection (RGB = default .rgb mask, R/G/B/A = scalar mask only)
            float3 matCapMask = 1.0;
            #if defined(LIL_FEATURE_MatCapBlendMask)
                float4 matCapMaskTex = LIL_SAMPLE_2D_ST(_MatCapBlendMask, samp, fd.uvMain);
                if(_CPM_MatCapChannel >= 4)
                    matCapMask = matCapMaskTex.rgb;
                else
                    matCapMask = lilChannelSelect(matCapMaskTex, _CPM_MatCapChannel);
            #endif

            // Blend
            matCapColor.rgb = lerp(matCapColor.rgb, matCapColor.rgb * fd.albedo, _MatCapMainStrength);
            fd.col.rgb = lilBlendColor(fd.col.rgb, matCapColor.rgb, _MatCapBlend * matCapColor.a * matCapMask, _MatCapBlendMode);
        }
    }
#endif

//------------------------------------------------------------------------------------------------------------------------------
// 5. MatCap 2nd (Channel Packed)
// Original: matCapMask = LIL_SAMPLE_2D_ST(_MatCap2ndBlendMask, ...).rgb — uses RGB as blend mask
// Modified: matCapMask = selected channel only (scalar mask)
//------------------------------------------------------------------------------------------------------------------------------
#if defined(LIL_FEATURE_MATCAP_2ND) && !defined(LIL_LITE) && !defined(LIL_FAKESHADOW)
    void lilGetMatCap2ndChannelPacked(inout lilFragData fd LIL_SAMP_IN_FUNC(samp))
    {
        if(_UseMatCap2nd)
        {
            // Normal
            float3 N = fd.matcap2ndN;
            #if defined(LIL_FEATURE_NORMAL_1ST) || defined(LIL_FEATURE_NORMAL_2ND)
                N = lerp(fd.origN, fd.matcap2ndN, _MatCap2ndNormalStrength);
            #endif
            #if defined(LIL_FEATURE_MatCap2ndBumpMap)
                if(_MatCap2ndCustomNormal)
                {
                    float4 normalTex = LIL_SAMPLE_2D_ST(_MatCap2ndBumpMap, samp, fd.uvMain);
                    float3 normalmap = lilUnpackNormalScale(normalTex, _MatCap2ndBumpScale);
                    N = normalize(mul(normalmap, fd.TBN));
                    N = fd.facing < (_FlipNormal-1.0) ? -N : N;
                }
            #endif

            // UV
            float2 mat2ndUV = lilCalcMatCapUV(fd.uv1, N, fd.V, fd.headV, _MatCap2ndTex_ST, _MatCap2ndBlendUV1.xy, _MatCap2ndZRotCancel, _MatCap2ndPerspective, _MatCap2ndVRParallaxStrength);

            // Color
            float4 matCap2ndColor = _MatCap2ndColor;
            #if defined(LIL_FEATURE_MatCap2ndTex)
                matCap2ndColor *= LIL_SAMPLE_2D_LOD(_MatCap2ndTex, lil_sampler_linear_repeat, mat2ndUV, _MatCap2ndLod);
            #endif
            #if !defined(LIL_PASS_FORWARDADD)
                matCap2ndColor.rgb = lerp(matCap2ndColor.rgb, matCap2ndColor.rgb * fd.lightColor, _MatCap2ndEnableLighting);
                matCap2ndColor.a = lerp(matCap2ndColor.a, matCap2ndColor.a * fd.shadowmix, _MatCap2ndShadowMask);
            #else
                if(_MatCap2ndBlendMode < 3) matCap2ndColor.rgb *= fd.lightColor * _MatCap2ndEnableLighting;
                matCap2ndColor.a = lerp(matCap2ndColor.a, matCap2ndColor.a * fd.shadowmix, _MatCap2ndShadowMask);
            #endif
            #if LIL_RENDER == 2 && !defined(LIL_REFRACTION)
                if(_MatCap2ndApplyTransparency) matCap2ndColor.a *= fd.col.a;
            #endif
            matCap2ndColor.a = fd.facing < (_MatCap2ndBackfaceMask-1.0) ? 0.0 : matCap2ndColor.a;
            // Mask — Channel selection (RGB = default .rgb mask, R/G/B/A = scalar mask only)
            float3 matCapMask = 1.0;
            #if defined(LIL_FEATURE_MatCap2ndBlendMask)
                float4 matCap2ndMaskTex = LIL_SAMPLE_2D_ST(_MatCap2ndBlendMask, samp, fd.uvMain);
                if(_CPM_MatCap2ndChannel >= 4)
                    matCapMask = matCap2ndMaskTex.rgb;
                else
                    matCapMask = lilChannelSelect(matCap2ndMaskTex, _CPM_MatCap2ndChannel);
            #endif

            // Blend
            matCap2ndColor.rgb = lerp(matCap2ndColor.rgb, matCap2ndColor.rgb * fd.albedo, _MatCap2ndMainStrength);
            fd.col.rgb = lilBlendColor(fd.col.rgb, matCap2ndColor.rgb, _MatCap2ndBlend * matCap2ndColor.a * matCapMask, _MatCap2ndBlendMode);
        }
    }
#endif

//------------------------------------------------------------------------------------------------------------------------------
// 6. RimLight (Channel Packed)
// Original: rimColor *= LIL_SAMPLE_2D_ST(_RimColorTex, ...) — applies RGBA (color + mask)
// Modified: rimColor *= selected channel only (scalar mask, no color)
//------------------------------------------------------------------------------------------------------------------------------
#if defined(LIL_FEATURE_RIMLIGHT) && !defined(LIL_LITE) && !defined(LIL_FAKESHADOW)
    void lilGetRimChannelPacked(inout lilFragData fd LIL_SAMP_IN_FUNC(samp))
    {
        if(_UseRim)
        {
            #if defined(LIL_FEATURE_RIMLIGHT_DIRECTION)
                // Color
                float4 rimColor = _RimColor;
                float4 rimIndirColor = _RimIndirColor;
                // Mask — Channel selection (RGB = default color+mask, R/G/B/A = scalar mask only)
                #if defined(LIL_FEATURE_RimColorTex)
                    float4 rimColorTex = LIL_SAMPLE_2D_ST(_RimColorTex, samp, fd.uvMain);
                    if(_CPM_RimLightChannel >= 4)
                    {
                        rimColor *= rimColorTex;
                        rimIndirColor *= rimColorTex;
                    }
                    else
                    {
                        float rimMask = lilChannelSelect(rimColorTex, _CPM_RimLightChannel);
                        rimColor *= rimMask;
                        rimIndirColor *= rimMask;
                    }
                #endif
                rimColor.rgb = lerp(rimColor.rgb, rimColor.rgb * fd.albedo, _RimMainStrength);

                // View direction
                float3 V = lilBlendVRParallax(fd.headV, fd.V, _RimVRParallaxStrength);

                // Normal
                float3 N = fd.N;
                #if defined(LIL_FEATURE_NORMAL_1ST) || defined(LIL_FEATURE_NORMAL_2ND)
                    N = lerp(fd.origN, fd.N, _RimNormalStrength);
                #endif
                float nvabs = abs(dot(N,V));

                // Factor
                float lnRaw = dot(fd.L, N) * 0.5 + 0.5;
                float lnDir = saturate((lnRaw + _RimDirRange) / (1.0 + _RimDirRange));
                float lnIndir = saturate((1.0-lnRaw + _RimIndirRange) / (1.0 + _RimIndirRange));
                float rim = pow(saturate(1.0 - nvabs), _RimFresnelPower);
                rim = fd.facing < (_RimBackfaceMask-1.0) ? 0.0 : rim;
                float rimDir = lerp(rim, rim*lnDir, _RimDirStrength);
                float rimIndir = rim * lnIndir * _RimDirStrength;

                rimDir = lilTooningScale(_AAStrength, rimDir, _RimBorder, _RimBlur);
                rimIndir = lilTooningScale(_AAStrength, rimIndir, _RimIndirBorder, _RimIndirBlur);

                rimDir = lerp(rimDir, rimDir * fd.shadowmix, _RimShadowMask);
                rimIndir = lerp(rimIndir, rimIndir * fd.shadowmix, _RimShadowMask);
                #if LIL_RENDER == 2 && !defined(LIL_REFRACTION)
                    if(_RimApplyTransparency)
                    {
                        rimDir *= fd.col.a;
                        rimIndir *= fd.col.a;
                    }
                #endif

                // Blend
                #if !defined(LIL_PASS_FORWARDADD)
                    float3 rimLightMul = 1 - _RimEnableLighting + fd.lightColor * _RimEnableLighting;
                #else
                    float3 rimLightMul = _RimBlendMode < 3 ? fd.lightColor * _RimEnableLighting : 1;
                #endif
                fd.col.rgb = lilBlendColor(fd.col.rgb, rimColor.rgb * rimLightMul, rimDir * rimColor.a, _RimBlendMode);
                fd.col.rgb = lilBlendColor(fd.col.rgb, rimIndirColor.rgb * rimLightMul, rimIndir * rimIndirColor.a, _RimBlendMode);
            #else
                // Color
                float4 rimColor = _RimColor;
                // Mask — Channel selection (RGB = default color+mask, R/G/B/A = scalar mask only)
                #if defined(LIL_FEATURE_RimColorTex)
                    float4 rimColorTex2 = LIL_SAMPLE_2D_ST(_RimColorTex, samp, fd.uvMain);
                    if(_CPM_RimLightChannel >= 4)
                        rimColor *= rimColorTex2;
                    else
                        rimColor *= lilChannelSelect(rimColorTex2, _CPM_RimLightChannel);
                #endif
                rimColor.rgb = lerp(rimColor.rgb, rimColor.rgb * fd.albedo, _RimMainStrength);

                // Normal
                float3 N = fd.N;
                #if defined(LIL_FEATURE_NORMAL_1ST) || defined(LIL_FEATURE_NORMAL_2ND)
                    N = lerp(fd.origN, fd.N, _RimNormalStrength);
                #endif
                float nvabs = abs(dot(N,fd.V));

                // Factor
                float rim = pow(saturate(1.0 - nvabs), _RimFresnelPower);
                rim = fd.facing < (_RimBackfaceMask-1.0) ? 0.0 : rim;
                rim = lilTooningScale(_AAStrength, rim, _RimBorder, _RimBlur);
                #if LIL_RENDER == 2 && !defined(LIL_REFRACTION)
                    if(_RimApplyTransparency) rim *= fd.col.a;
                #endif
                rim = lerp(rim, rim * fd.shadowmix, _RimShadowMask);

                // Blend
                #if !defined(LIL_PASS_FORWARDADD)
                    rimColor.rgb = lerp(rimColor.rgb, rimColor.rgb * fd.lightColor, _RimEnableLighting);
                #else
                    if(_RimBlendMode < 3) rimColor.rgb *= fd.lightColor * _RimEnableLighting;
                #endif
                fd.col.rgb = lilBlendColor(fd.col.rgb, rimColor.rgb, rim * rimColor.a, _RimBlendMode);
            #endif
        }
    }
#endif

//------------------------------------------------------------------------------------------------------------------------------
// 7. RimShade (Channel Packed)
// Original: rim *= LIL_SAMPLE_2D(_RimShadeMask, ...).r — reads R channel only
// Modified: rim *= selected channel (scalar mask)
//------------------------------------------------------------------------------------------------------------------------------
#if defined(LIL_FEATURE_RIMSHADE) && !defined(LIL_FAKESHADOW)
    void lilGetRimShadeChannelPacked(inout lilFragData fd LIL_SAMP_IN_FUNC(samp))
    {
        if(_UseRimShade)
        {
            float3 N = fd.N;
            #if defined(LIL_FEATURE_NORMAL_1ST) || defined(LIL_FEATURE_NORMAL_2ND)
                N = lerp(fd.origN, fd.N, _RimShadeNormalStrength);
            #endif
            float nvabs = abs(dot(N,fd.headV));
            float rim = pow(saturate(1.0 - nvabs), _RimShadeFresnelPower);
            rim = lilTooningScale(_AAStrength, rim, _RimShadeBorder, _RimShadeBlur);
            rim *= _RimShadeColor.a;
            // Mask — Channel Packed: selected channel (scalar mask)
            #if defined(LIL_FEATURE_RimShadeMask)
                rim *= lilChannelSelect(LIL_SAMPLE_2D(_RimShadeMask, samp, fd.uvMain), _CPM_RimShadeChannel);
            #endif
            fd.col.rgb = lerp(fd.col.rgb, fd.col.rgb * _RimShadeColor.rgb, rim);
        }
    }
#endif

//------------------------------------------------------------------------------------------------------------------------------
// 8. Backlight (Channel Packed)
// Original: backlightColor *= LIL_SAMPLE_2D_ST(_BacklightColorTex, ...) — applies RGBA (color + mask)
// Modified: backlightColor *= selected channel only (scalar mask, no color)
//------------------------------------------------------------------------------------------------------------------------------
#if defined(LIL_FEATURE_BACKLIGHT) && !defined(LIL_LITE) && !defined(LIL_GEM) && !defined(LIL_FAKESHADOW)
    void lilBacklightChannelPacked(inout lilFragData fd LIL_SAMP_IN_FUNC(samp))
    {
        if(_UseBacklight)
        {
            // Normal
            float3 N = fd.N;
            #if defined(LIL_FEATURE_NORMAL_1ST) || defined(LIL_FEATURE_NORMAL_2ND)
                N = lerp(fd.origN, fd.N, _BacklightNormalStrength);
            #endif

            // Color
            float4 backlightColor = _BacklightColor;
            // Mask — Channel selection (RGB = default color+mask, R/G/B/A = scalar mask only)
            #if defined(LIL_FEATURE_BacklightColorTex)
                float4 backlightMaskTex = LIL_SAMPLE_2D_ST(_BacklightColorTex, samp, fd.uvMain);
                if(_CPM_BacklightChannel >= 4)
                    backlightColor *= backlightMaskTex;
                else
                    backlightColor *= lilChannelSelect(backlightMaskTex, _CPM_BacklightChannel);
            #endif

            // Factor
            float backlightFactor = pow(saturate(-fd.hl * 0.5 + 0.5), _BacklightDirectivity);
            float backlightLN = dot(normalize(-fd.headV * _BacklightViewStrength + fd.L), N) * 0.5 + 0.5;
            #if defined(LIL_USE_SHADOW) || defined(LIL_LIGHTMODE_SHADOWMASK)
                if(_BacklightReceiveShadow) backlightLN *= saturate(fd.attenuation + distance(fd.L, fd.origL));
            #endif
            backlightLN = lilTooningScale(_AAStrength, backlightLN, _BacklightBorder, _BacklightBlur);
            float backlight = saturate(backlightFactor * backlightLN);
            backlight = fd.facing < (_BacklightBackfaceMask-1.0) ? 0.0 : backlight;

            // Blend
            backlightColor.rgb = lerp(backlightColor.rgb, backlightColor.rgb * fd.albedo, _BacklightMainStrength);
            fd.col.rgb += backlight * backlightColor.a * backlightColor.rgb * fd.lightColor;
        }
    }
#endif

//------------------------------------------------------------------------------------------------------------------------------
// 9. Alpha Mask (Channel Packed)
// Original: alphaMask = LIL_SAMPLE_2D_ST(_AlphaMask, ...).r — reads R channel only
// Modified: reads user-selected channel via _CPM_AlphaMaskChannel
//------------------------------------------------------------------------------------------------------------------------------
#if !defined(LIL_FAKESHADOW)
void lilAlphaMaskChannelPacked(inout lilFragData fd LIL_SAMP_IN_FUNC(samp))
{
    if(_AlphaMaskMode)
    {
        float4 alphaMaskTex = LIL_SAMPLE_2D_ST(_AlphaMask, samp, fd.uvMain);
        float alphaMask = lilChannelSelect(alphaMaskTex, _CPM_AlphaMaskChannel);
        alphaMask = saturate(alphaMask * _AlphaMaskScale + _AlphaMaskValue);
        if(_AlphaMaskMode == 1) fd.col.a = alphaMask;
        if(_AlphaMaskMode == 2) fd.col.a = fd.col.a * alphaMask;
        if(_AlphaMaskMode == 3) fd.col.a = saturate(fd.col.a + alphaMask);
        if(_AlphaMaskMode == 4) fd.col.a = saturate(fd.col.a - alphaMask);
    }
}
#endif
