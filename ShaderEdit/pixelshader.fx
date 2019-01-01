
float4 mainImage(float2 texCoord)
{
    return SAMPLE_TEXTURE(Texture, texCoord);
}