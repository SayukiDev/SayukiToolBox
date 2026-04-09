// ChannelPackedMasks - custom.hlsl
// Macro definitions for channel-packed mask reading
// Each mask reads a user-selectable channel (R/G/B/A) from its texture.

// Custom properties: channel selection indices (0=R, 1=G, 2=B, 3=A)
#define LIL_CUSTOM_PROPERTIES \
    uint _CPM_Emission1stChannel; \
    uint _CPM_Emission2ndChannel; \
    uint _CPM_GlitterChannel; \
    uint _CPM_MatCapChannel; \
    uint _CPM_MatCap2ndChannel; \
    uint _CPM_RimLightChannel; \
    uint _CPM_RimShadeChannel; \
    uint _CPM_BacklightChannel; \
    uint _CPM_AlphaMaskChannel;

// No custom textures needed (uses existing lilToon textures)
#define LIL_CUSTOM_TEXTURES

//------------------------------------------------------------------------------------------------------------------------------
// OVERRIDE: Emission 1st
//------------------------------------------------------------------------------------------------------------------------------
#define OVERRIDE_EMISSION_1ST \
    lilEmissionChannelPacked(fd LIL_SAMP_IN(sampler_MainTex));

//------------------------------------------------------------------------------------------------------------------------------
// OVERRIDE: Emission 2nd
//------------------------------------------------------------------------------------------------------------------------------
#define OVERRIDE_EMISSION_2ND \
    lilEmission2ndChannelPacked(fd LIL_SAMP_IN(sampler_MainTex));

//------------------------------------------------------------------------------------------------------------------------------
// OVERRIDE: Glitter
//------------------------------------------------------------------------------------------------------------------------------
#define OVERRIDE_GLITTER \
    lilGlitterChannelPacked(fd LIL_SAMP_IN(sampler_MainTex));

//------------------------------------------------------------------------------------------------------------------------------
// OVERRIDE: MatCap 1st
//------------------------------------------------------------------------------------------------------------------------------
#define OVERRIDE_MATCAP \
    lilGetMatCapChannelPacked(fd LIL_SAMP_IN(sampler_MainTex));

//------------------------------------------------------------------------------------------------------------------------------
// OVERRIDE: MatCap 2nd
//------------------------------------------------------------------------------------------------------------------------------
#define OVERRIDE_MATCAP_2ND \
    lilGetMatCap2ndChannelPacked(fd LIL_SAMP_IN(sampler_MainTex));

//------------------------------------------------------------------------------------------------------------------------------
// OVERRIDE: RimLight
//------------------------------------------------------------------------------------------------------------------------------
#define OVERRIDE_RIMLIGHT \
    lilGetRimChannelPacked(fd LIL_SAMP_IN(sampler_MainTex));

//------------------------------------------------------------------------------------------------------------------------------
// OVERRIDE: RimShade
//------------------------------------------------------------------------------------------------------------------------------
#define OVERRIDE_RIMSHADE \
    lilGetRimShadeChannelPacked(fd LIL_SAMP_IN(sampler_MainTex));

//------------------------------------------------------------------------------------------------------------------------------
// OVERRIDE: Backlight
//------------------------------------------------------------------------------------------------------------------------------
#define OVERRIDE_BACKLIGHT \
    lilBacklightChannelPacked(fd LIL_SAMP_IN(sampler_MainTex));

//------------------------------------------------------------------------------------------------------------------------------
// OVERRIDE: Alpha Mask
//------------------------------------------------------------------------------------------------------------------------------
#define OVERRIDE_ALPHAMASK \
    lilAlphaMaskChannelPacked(fd LIL_SAMP_IN(sampler_MainTex));
