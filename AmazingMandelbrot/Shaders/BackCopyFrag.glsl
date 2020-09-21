#version 400
struct DataStruct
{
	double IterationCount;
	double MinDistance;
	vec2 EndPoint;
};
layout(std140) buffer DataBlock
{
  DataStruct Data[];
};
layout(std140) buffer NewDataBlock
{
  DataStruct NewData[];
};
uniform sampler2D sourceTex;
in vec2 fPosition;
out vec4 fragColor;
uniform ivec2 resolution;
uniform mat4 projectionMatrix;
void main()
{
	ivec2 storePos = ivec2(fPosition);
	int index = storePos.y*resolution.x+storePos.x;
	NewData[index]=Data[index];
	vec4 Col = texelFetch(sourceTex,ivec2(fPosition.x,fPosition.y),0);
	fragColor = vec4(Col.xyz,1);
	//fragColor = texelFetch(sourceTex,ivec2(fPosition.x,fPosition.y),0);
}