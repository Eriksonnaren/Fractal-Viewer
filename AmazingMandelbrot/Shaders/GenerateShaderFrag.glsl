#version 400
struct DataStruct
{
	double IterationCount;
	double MinDistance;
	vec2 EndPoint;
};
layout(std140) buffer OldDataBlock
{
  DataStruct OldData[];
};
layout(std140) buffer DataBlock
{
  DataStruct Data[];
};

uniform ivec2 resolution;
uniform sampler2D reverseTex;
in vec2 fPosition;
out vec4 fragColor;
uniform int Iter;
uniform double Zoom;
uniform double CameraReal;
uniform double CameraImag;
uniform int Julia;
uniform double JuliaReal;
uniform double JuliaImag;
uniform vec2 Size;
const float pi=3.1415926;
uniform double[64] CoefficientArray;
uniform int ArrayMaxZ;
uniform int ArrayMaxC;
dvec2[8] PolynomialConstants;
dvec2[8] PolynomialConstantsDerC;
uniform int MaxPeriod=20;
uniform ivec2 PixelShift;


dvec2 Mult(dvec2 A,dvec2 B)
{
	return dvec2(A.x*B.x-A.y*B.y,A.x*B.y+A.y*B.x);
}
dvec2 Conjugate(dvec2 Z)
{
	return dvec2(Z.x,-Z.y);
}
dvec2 Div(dvec2 A,dvec2 B)
{
	return Mult(A,Conjugate(B))/(B.x*B.x+B.y*B.y);
}
dvec2 Log(dvec2 Z)
{
	return dvec2(log(float(Z.x*Z.x+Z.y*Z.y))/2,atan(float(Z.y),float(Z.x)));
}
dvec2 Exp(dvec2 Z)
{
	float E = exp(float(Z.x));
	return dvec2(E*cos(float(Z.y)),E*sin(float(Z.y)));
}
dvec2 Pow(dvec2 A,dvec2 B)
{
	return Exp(Mult(B,Log(A)));
}
dvec2 Sin(dvec2 Z)
{
	return Div(Exp(Mult(Z,dvec2(0,1)))-Exp(Mult(Z,dvec2(0,-1))),dvec2(0,2));
}
dvec2 Cos(dvec2 Z)
{
	return Div(Exp(Mult(Z,dvec2(0,1)))+Exp(Mult(Z,dvec2(0,-1))),dvec2(2,0));
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
dvec2 ComputeDerivativeZ(dvec2 Z)
{
	dvec2 NewZ=dvec2(0,0);
	for(int i =ArrayMaxZ-1;i>=1;i--)
	{
	  NewZ=Mult(Z,NewZ)+PolynomialConstants[i]*i;
	}
	return NewZ;
}
dvec2 ComputeDerivativeC(dvec2 Z)
{
	dvec2 NewZ=dvec2(0,0);
	for(int i =ArrayMaxZ-1;i>=0;i--)
	{
	  NewZ=Mult(Z,NewZ)+PolynomialConstantsDerC[i];
	}
	return NewZ;
}
double logd(double a)
{
	const double s=0.001;
	double L1 = log(float(a));
	double L2 = log(float(a+s));
	double Q = mod(a/s,1.0);
	return L1*(1-Q)+L2*Q;
}
vec4 MainCompute(dvec2 C,int index)
{
	dvec2 Z = dvec2(0);
	if(Julia==1)
	{
		Z=C;
		C= dvec2(JuliaReal,JuliaImag);
	}
	int L;
	double E = 0;
	
	for(int i=0;i<ArrayMaxZ;i++)
	{
		PolynomialConstants[i]=dvec2(0,0);
	}
	for(int i=0;i<ArrayMaxZ;i++)
	{
		int k = 2*(i);
		dvec2 M = dvec2(CoefficientArray[k],CoefficientArray[k+1]);
		PolynomialConstants[i] = M;
		PolynomialConstantsDerC[i]=vec2(0);
		dvec2 PowerC = dvec2(1,0);
		for(int j=1;j<ArrayMaxC;j++)
		{
			k = 2*(i+ArrayMaxZ*j);
			M = dvec2(CoefficientArray[k],CoefficientArray[k+1]);
			PolynomialConstantsDerC[i]+=Mult(PowerC,M);
			PowerC=Mult(PowerC,C);
			PolynomialConstants[i]+=Mult(PowerC,M);
		}
		
	}
	dvec2 DC = dvec2(1,0);
	int ArrayMax = ArrayMaxZ-1;
	double MinDist = 100;
	double AverageDist =0;
	for(L=0;L<Iter;L++)
	{

		//DC=Mult(DC,ComputeDerivativeZ(Z))+ComputeDerivativeC(Z);
		//Z=Compute(Z);

		dvec2 DerZ=PolynomialConstants[ArrayMax]*ArrayMax;
		dvec2 DerC=PolynomialConstantsDerC[ArrayMax];
		dvec2 NewZ=PolynomialConstants[ArrayMax];
		for(int i =ArrayMaxZ-2;i>=1;i--)
		{
			//DerZ=Mult(Z,DerZ)+PolynomialConstants[i]*i;
			//DerC=Mult(Z,DerC)+PolynomialConstantsDerC[i];
			NewZ=Mult(Z,NewZ)+PolynomialConstants[i];
		}
		//DC=Mult(DC,DerZ)+Mult(Z,DerC)+PolynomialConstantsDerC[0];
		Z=Mult(Z,NewZ)+PolynomialConstants[0];
		
		
		double RR = Z.x*Z.x;
		double II = Z.y*Z.y;
		//double M = 0.05*(RR*II)/((RR+II)*sqrt(RR+II));
		MinDist=min(MinDist,RR+II);
		if(RR+II>100*100)
		{
			int Power = ArrayMaxZ-1;
			E=1-logd(logd((length(PolynomialConstants[Power])))/(Power-1)+logd((RR+II))/2)/logd(Power);
			break;
		}
	}
	float r = float(length(Z));
	float dr = float(length(DC));
	
	float Dist = log(0.5*(r*log(r)/dr));
	Dist =1;
	double A = max(L+E,1);
	if(L==Iter)
	{
		A=0;
	}
	Data[index]=DataStruct(A,MinDist,vec2(Z));
	return vec4(L,Dist,MinDist,1);
}
void main() {
	ivec2 storePos = ivec2(fPosition);
	vec2 position = (vec2(fPosition.xy)+vec2(0,(resolution.x-resolution.y)/2))/resolution.x;
	int index = int(storePos.y*resolution.x+storePos.x);
	
	dvec2 C = position*2*Zoom-Zoom+dvec2(CameraReal,CameraImag);
	vec4 Out;
	if(PixelShift.x==0&&PixelShift.y==0)
	{
		Out = MainCompute(C,index);
	}else
	{
		ivec2 SamplePos=storePos+PixelShift;
		if(SamplePos.x>=0&&SamplePos.y>=0&&SamplePos.x<resolution.x&&SamplePos.y<resolution.y)
		{
			Out = texelFetch(reverseTex,SamplePos,0);
			int oldIndex = int(SamplePos.y*resolution.x+SamplePos.x);
			Data[index]=OldData[oldIndex];
		}else
		{
			Out = MainCompute(C,index);
		}
	}
	fragColor = vec4(Out.xyz,1);
}



