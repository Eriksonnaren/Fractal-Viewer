#version 430
struct DataStruct
{
	double IterationCount;
	double MinDistance;
	double DistEstimate;
	int RawIter;
	int Period;
	dvec2 EndPoint;
};
layout(std140) buffer DataBlock
{
  DataStruct Data[];
};
in vec3 vPosition;
uniform int Iter;
uniform double Zoom;
uniform mat4 projectionMatrix;
uniform mat4 viewMatrix;
uniform mat4 modelMatrix;
out vec3 fPosition;
uniform ivec2 resolution;
uniform int MeshMode;
void main()
{
	
	vec3 inputPos = vPosition;
	if(MeshMode>0)
	{
		ivec2 storePos=ivec2(vPosition.xy);
		int index = int(storePos.y*resolution.x+storePos.x);
		float d = Data[index].DistEstimate==0?Iter:float(Data[index].IterationCount);
		//inputPos.z-=log(d)*40-100;
		inputPos.z+=sqrt(float(Data[index].DistEstimate/Zoom)*resolution.x)*8;
	}
	vec4 P = projectionMatrix*viewMatrix*modelMatrix*vec4(inputPos.x,inputPos.y,inputPos.z,1);
	//P.y =-P.y;
	

	gl_Position = P;
	fPosition=vec3(vPosition.x,vPosition.y,vPosition.z);
}