#version 430
layout (local_size_x = 100, local_size_y = 1,local_size_z = 1) in;
//layout(r32i) uniform coherent iimage2D TexR;
//void main() {
//	imageAtomicAdd(TexR,ivec2(1,1),5);
//}

layout(std430,binding = 50) buffer Block
{
  double Values[];
  //int Permutation[];
};
layout(r32i) uniform coherent iimage2D TexR;
layout(r32i) uniform coherent iimage2D TexG;
layout(r32i) uniform coherent iimage2D TexB;
uniform double Zoom=2;
uniform dvec2 CameraPos=vec2(0);
uniform vec2 resolution;
uniform int CurrentIndex;
uniform int Stage;
uniform int FinalSampleDensity;
uniform int CurrentFinalSample;
uniform double StepSize;
uniform dvec2 RectanglePos;
uniform dvec2 RectangleSize;
const int IterBlue=40;//40
const int IterGreen=400;//400
const int IterRed=1000;
const int MaxIter=1000;
const int MinIter=40;
const vec3 PresicionScale=vec3(1/log(IterRed),1/log(IterGreen),1/log(IterBlue))*1000;
uniform int MaxBufferData;
uniform int PermutationFactor;
uniform int PermutationCycleLength;
uniform double[64] CoefficientArray;
uniform int ArrayMaxZ;
uniform int ArrayMaxC;
dvec2[8] PolynomialConstants;
uniform double OffsetReal;
uniform double OffsetImag;
dvec2 Mult(dvec2 A,dvec2 B)
{
	return dvec2(A.x*B.x-A.y*B.y,A.x*B.y+A.y*B.x);
}
dvec2 Compute(dvec2 Z)
{
	dvec2 NewZ=dvec2(0,0);
	for(int i =ArrayMaxZ-1;i>=0;i--)
	{
	   NewZ=Mult(Z,NewZ)+PolynomialConstants[i];
	}
	return NewZ;
}
int GetPermutationOffset(uint K)
{
	int N = FinalSampleDensity*FinalSampleDensity-1;
	K=K%PermutationCycleLength;
	int h = PermutationFactor;
	for(int i=0;i<K;i++)
	{
		h=(h*PermutationFactor)%N;
	}
	return h;
}
void AddToTexture(int L,ivec2 Pos, float Val)
{
	//int a =imageLoad(TexR,Pos).x;
	if(L<IterRed)
		//imageStore(TexR,Pos,ivec4(imageLoad(TexR,Pos).x+int(Val*PresicionScale.x),0,0,0));
		imageAtomicAdd(TexR,Pos,int(Val*PresicionScale.x));
	if(L<IterGreen)
		//imageStore(TexG,Pos,ivec4(imageLoad(TexR,Pos).x+int(Val*PresicionScale.y),0,0,0));
		imageAtomicAdd(TexG,Pos,int(Val*PresicionScale.y));
	if(L<IterBlue)
		//imageStore(TexB,Pos,ivec4(imageLoad(TexB,Pos).x+int(Val*PresicionScale.z),0,0,0));
		imageAtomicAdd(TexB,Pos,int(Val*PresicionScale.z));
}
dvec2 GetScreenFromWorld(dvec2 Complex)
{
    dvec2 position = (Complex - CameraPos +dvec2(Zoom,Zoom)) / (2 * Zoom);
    return position * resolution.x - dvec2(0, (resolution.x - resolution.y) / 2);
}
void DebugDraw(ivec3 col,ivec2 p)
{
int s = 12000;
	for(int i =0;i<1;i++)
	{
		for(int j =0;j<1;j++)
		{
			if(col.x>0)
				imageAtomicAdd(TexR,p+ivec2(i,j),s);
			if(col.y>0)
				imageAtomicAdd(TexG,p+ivec2(i,j),s);
			if(col.z>0)
				imageAtomicAdd(TexB,p+ivec2(i,j),s);
		}
	}
	
}
bool RectangleTest(dvec2 Point)
{
	dvec2 RelPos = abs(Point-RectanglePos);
	return RelPos.x<RectangleSize.x && RelPos.y<RectangleSize.y;
}

void main() {
	//imageAtomicAdd(TexR,ivec2(1,1),5);
	uint Index =(gl_GlobalInvocationID.x+CurrentIndex);
	if(Index>=MaxBufferData)
	{
		return;
	}
	uint n = 2*Index;
	dvec2 C = dvec2(Values[n],Values[n+1]);
	if(Values[n]==0&&Values[n+1]==0)
	{
		return;
	}
	if(Stage>0)
	{
		int N = FinalSampleDensity*FinalSampleDensity-1;
		//int c = (GetPermutationOffset(Index)*CurrentFinalSample)%N+1;
		int c = (int(1)*CurrentFinalSample)%N+1;
		double x = StepSize*(c%FinalSampleDensity);
		double y = StepSize*(c/FinalSampleDensity);
		C+=dvec2(x,y);
		//C+=dvec2(0.1,0.1);
	}

	for(int i=0;i<ArrayMaxZ;i++)
	{
		int k = 2*i;
		dvec2 M = dvec2(CoefficientArray[k],CoefficientArray[k+1]);
		PolynomialConstants[i] = M;
		
		dvec2 PowerC = dvec2(1,0);
		for(int j=1;j<ArrayMaxC;j++)
		{
			k = 2*(i+ArrayMaxZ*j);
			M = dvec2(CoefficientArray[k],CoefficientArray[k+1]);
			PowerC=Mult(PowerC,C);
			PolynomialConstants[i]+=Mult(PowerC,M);
		}
		
	}

	dvec2 Z = dvec2(OffsetReal,OffsetImag);
	int L;
	dvec2[MaxIter] Path;
	int E = 0;
	int TouchesRectangle = 0;
	for(L=0;L<MaxIter;L++)
	{
		Z=Compute(Z);
		Path[L]=Z;
		TouchesRectangle += RectangleTest(Z)?1:0;
		if(Z.x*Z.x+Z.y*Z.y>20)
		{
			break;
		}
	}
	ivec2 P = ivec2(GetScreenFromWorld(C));
	//if(Stage==2)
	//{
	//	DebugDraw(ivec3(0,1,0),P);
	//}
	//else if(Stage==1)
	//{
	//	DebugDraw(ivec3(0,1,1),P);
	//	//imageAtomicAdd(TexB,P,1500);
	//	//imageAtomicAdd(TexG,P,1500);
	//}else{
	//	DebugDraw(ivec3(1,0,0),P);
	//
	//	//imageAtomicAdd(TexR,P,1500);
	//}
	if(TouchesRectangle>0)
	{
		if(L<MaxIter)
		{
			for(int i =1;i<L;i++)
			{
				vec2 ScreenPos = vec2(GetScreenFromWorld(Path[i]));
			
				int x=int(ScreenPos.x);
				float fx = ScreenPos.x-x;
				int y=int(ScreenPos.y);
				float fy = ScreenPos.y-y;
				if(x<resolution.x-1&&y<resolution.y-1&&x>0&&y>0)
				{
				  
				  AddToTexture(L,ivec2(x,y),((1-fx)*(1-fy)));
				  AddToTexture(L,ivec2(x+1,y),((fx)*(1-fy)));
				  AddToTexture(L,ivec2(x,y+1),((1-fx)*(fy)));
				  AddToTexture(L,ivec2(x+1,y+1),((fx)*(fy)));
				  
				}
			}
		}
	}
	else 
	{
		if(Stage==0)
			Values[n]=Values[n+1]=0;
	}
}