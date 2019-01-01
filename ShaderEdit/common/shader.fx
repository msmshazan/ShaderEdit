#ifdef SM4

// Macros for targetting shader model 4.0 (DX11)

#define TECHNIQUE(name, vsname, psname ) \
	technique name { pass { VertexShader = compile vs_4_0_level_9_1 vsname (); PixelShader = compile ps_4_0_level_9_1 psname(); } }

#define BEGIN_CONSTANTS     cbuffer Parameters : register(b0) {
#define MATRIX_CONSTANTS
#define END_CONSTANTS       };

#define _vs(r)
#define _ps(r)
#define _cb(r)

#define DECLARE_TEXTURE(Name, index) \
    Texture2D<float4> Name : register(t##index); \
    sampler Name##Sampler : register(s##index)

#define DECLARE_CUBEMAP(Name, index) \
    TextureCube<float4> Name : register(t##index); \
    sampler Name##Sampler : register(s##index)

#define SAMPLE_TEXTURE(Name, texCoord)  Name.Sample(Name##Sampler, texCoord)
#define SAMPLE_CUBEMAP(Name, texCoord)  Name.Sample(Name##Sampler, texCoord)


#else


// Macros for targetting shader model 2.0 (DX9)

#define TECHNIQUE(name, vsname, psname ) \
	technique name { pass { VertexShader = compile vs_2_0 vsname (); PixelShader = compile ps_2_0 psname(); } }

#define BEGIN_CONSTANTS
#define MATRIX_CONSTANTS
#define END_CONSTANTS

#define _vs(r)  : register(vs, r)
#define _ps(r)  : register(ps, r)
#define _cb(r)

#define DECLARE_TEXTURE(Name, index) \
    sampler2D Name : register(s##index);


#define DECLARE_TEXTURE2D(Name, index) \
    sampler2D Name : register(s##index);

#define DECLARE_CUBEMAP(Name, index) \
    samplerCUBE Name : register(s##index);

#define SAMPLE_TEXTURE(Name, texCoord)  tex2D(Name, texCoord)
#define SAMPLE_CUBEMAP(Name, texCoord)  texCUBE(Name, texCoord)


#endif


DECLARE_TEXTURE(Channel0, 0);
DECLARE_TEXTURE(Channel1, 1);
DECLARE_TEXTURE(Channel2, 2);
DECLARE_TEXTURE(Channel3, 3);


BEGIN_CONSTANTS

float4 Date;
float3 Resolution;
float Time;
MATRIX_CONSTANTS

    float4x4 MatrixTransform    _vs(c0) _cb(c0);

END_CONSTANTS


struct ShaderData
{
	float4 position		: SV_Position;
	float4 color		: COLOR0;
    float2 texCoord		: TEXCOORD0;
};

ShaderData SpriteVertexShader(	float4 position	: POSITION0,
								float4 color	: COLOR0,
								float2 texCoord	: TEXCOORD0)
{
	ShaderData output;
    output.position = mul(position, MatrixTransform);
	output.color = color;
	output.texCoord = texCoord;
	return output;
}

#include "pixelshader.fx"

float4 SpritePixelShader(ShaderData uniforms) : SV_Target0
{
    return mainImage( uniforms.texCoord) * uniforms.color;
}

TECHNIQUE( SpriteBatch, SpriteVertexShader, SpritePixelShader );