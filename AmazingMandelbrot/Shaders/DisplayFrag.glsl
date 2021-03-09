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
layout(r32i) uniform coherent iimage2D BuddhaTexR;
layout(r32i) uniform coherent iimage2D BuddhaTexG;
layout(r32i) uniform coherent iimage2D BuddhaTexB;
uniform int BuddhaActive;
uniform int BuddhaReset;
#define Tau 6.28318530718
in vec3 fPosition;
out vec4 fragColor;
uniform ivec2 resolution;
uniform ivec2 outputResolution;
uniform sampler2D sourceTex;

uniform double Zoom;
uniform double CameraReal;
uniform double CameraImag;
uniform int Julia;
uniform double JuliaReal;
uniform double JuliaImag;
uniform vec2 Size;
uniform float ColorOffset;
uniform float ColorScale;
uniform float Time;
const float pi=3.1415926;
uniform double[64] CoefficientArray;
uniform int ArrayMaxZ;
uniform int ArrayMaxC;
uniform int PeriodHighlight;
dvec2[8] PolynomialConstants;
uniform float[32] ColorData;
uniform int PalleteSize;
uniform vec4 InteriorColor;
uniform int QuaternionJulia;
uniform vec2 IterationPoint;
uniform float CenterDotStrength;
uniform float FinalDotStrength;
uniform dvec2 FinalDotPosition;
uniform float DistanceEstimateColoringLerp;

vec3 GetBuddhaColor(ivec2 Pos)
{
	float r = imageLoad(BuddhaTexR,Pos).x;
	float g = imageLoad(BuddhaTexG,Pos).x;
	float b = imageLoad(BuddhaTexB,Pos).x;
	vec3 V = vec3(r,g,b)/100000;//100000
	float t = Time*0.2;
	//V *= vec3(ColorScale(t),ColorScale(t+2.094),ColorScale(t+4.1887));
	return (V.x*vec3(1,0,0))+(V.y*vec3(0,1,0))+(V.z*vec3(0,0,1));
}
float hueValue(float h)
{
    float a = (2.0 * (h - int(h)) - 1.0);
    if (a < 0)
        a = -a;
    a = 3 * a - 1;
    if (a > 1) return 1;
    else if (a < 0) return 0;
    else return a;
}
float Lerp(float a,float b,float t)
{
	return a*(1-t)+b*t;
}
vec3 LerpColor(vec3 a,vec3 b,float t)
{
	return a*(1-t)+b*t;
}
float Beizer(float t)
{
	return t*t*(3-2*t);
}
vec3 GetExteriorColor(float t)
{
	int Id1 = PalleteSize - 1;
    int Id2 = 0;
    while(t> ColorData[Id2*4]&& Id2< PalleteSize)
    {
		Id1++;
        Id2++;
        Id1 = (Id1%PalleteSize);
    }
    Id2 = Id2 % PalleteSize;
    float LerpParameter = (mod(t- ColorData[Id1*4]+1,1)) /(mod(ColorData[Id2*4]- ColorData[Id1*4]+1,1));
	vec3 Color1 = vec3(ColorData[Id1*4+1],ColorData[Id1*4+2],ColorData[Id1*4+3]);
	vec3 Color2 = vec3(ColorData[Id2*4+1],ColorData[Id2*4+2],ColorData[Id2*4+3]);
    return LerpColor(Color1, Color2, Beizer(LerpParameter));
}
float GetApproximateDistanceEstimate(ivec2 storePos)
{
	int index = int(storePos.y*resolution.x+storePos.x);
	dvec2 Z0 = Data[index].EndPoint;//current pixel
	int L = Data[index].RawIter;
	dvec2 DC = dvec2(0);
	dvec2 DCI = dvec2(0);
	DC+=Data[index+1].RawIter==L?Z0-Data[index+1].EndPoint:vec2(0);
	DCI+=Data[index+resolution.x].RawIter==L?(Z0-Data[index+resolution.x].EndPoint):vec2(0);
	DC-=Data[index-1].RawIter==L?Z0-Data[index-1].EndPoint:vec2(0);
	DCI-=Data[index-resolution.x].RawIter==L?(Z0-Data[index-resolution.x].EndPoint):vec2(0);
	int amount = 0;
	amount +=Data[index+1].RawIter==L?1:0;
	amount +=Data[index+resolution.x].RawIter==L?1:0;
	amount +=Data[index-1].RawIter==L?1:0;
	amount +=Data[index-resolution.x].RawIter==L?1:0;

	DC = (DC+dvec2(DCI.y,-DCI.x))/amount;

	float r = float(length(Z0));
	float dr = float(length(DC));
	
	float Dist = log(0.5*(r*log(r)/dr));
	return Dist;
}
float hueValue2(float h)
{
	return (cos(h*pi*2)+1)/2;
}
//fancy color by wes
vec3 FancyColor(float h)
{
	h=mod(h,1);
	h*=2;
	vec3 A = vec3(h*h,h,sqrt(h));
	h=2-h;
	vec3 B = vec3(h*h,h,sqrt(h));
	return min(A,B.zyx);
}
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
dvec2 ComputeDerivative(dvec2 Z)
{
	dvec2 NewZ=dvec2(0,0);
	for(int i =ArrayMaxZ-1;i>=1;i--)
	{
	  NewZ=Mult(Z,NewZ)+PolynomialConstants[i]*i;
	}
	return NewZ;
}
double logd(double a)
{
	const double s=0.000000000001;
	double L1 = log(float(a));
	double L2 = log(float(a+s));
	double Q = mod(a/s,1.0);
	return L1*(1-Q)+L2*Q;
}
float GetLight(ivec2 storePos)
{
	dvec3 Sun = normalize(vec3(-1,-1,0.0));
	double HeightScale = 7;
	int index = int(storePos.y*resolution.x+storePos.x);
	double H1 = HeightScale*Data[index].IterationCount;//current pixel
	double H2 = HeightScale*Data[index+1].IterationCount;//pixel to the right
	double H3 = HeightScale*Data[index+resolution.x].IterationCount;//pixel below
	dvec3 V1 = dvec3(0,0,H1);//triangle using the 3 pixels with the iterationcount as height in z dimension
	dvec3 V2 = dvec3(1,0,H2);
	dvec3 V3 = dvec3(0,1,H3);
	dvec3 Norm = normalize(cross(V2-V1,V3-V1));//normal vector of triangle
	return float(dot(Norm,Sun));//dot between normal and sun to give light/shadow
}
ivec2 Get3dPosition(ivec2 OriginalPos)
{
	vec2 ScreenPos = (vec2(OriginalPos.xy)-vec2(resolution.xy)/2)/resolution.x;
	float a = Time*0.05;
	vec3 CameraVector = vec3(cos(a),sin(a),0.5)*0.7;
	vec3 I = -normalize(CameraVector);
	vec3 K = normalize(cross(I,vec3(0,0,1)));
	vec3 J = cross(I,K);
	vec3 Vel = I+J*ScreenPos.y-K*ScreenPos.x;
	vec3 Pos = CameraVector;
	Pos -= Vel*(Pos.z/Vel.z);
	ivec2 storePos =ivec2(Pos.xy*resolution.x+vec2(resolution.xy)/2);
	float PreviousHeight=0;
	for(int i = 0;i<100;i++)
	{
		int index = int(storePos.y*resolution.x+storePos.x);
		DataStruct data = Data[index];
		double DistEstimate = data.DistEstimate;
		float Height = -0.001*exp(sin(Time*0.5))*float(DistEstimate)/resolution.x;
		if(abs(Pos.z-Height)<3/resolution.x)
		{
			break;
		}
		Pos -= Vel*((Pos.z-(Height+PreviousHeight)/2)/Vel.z);
		PreviousHeight=Height;
		//Pos+=Vel/resolution.x;
		//float T=0.1*(Height-Pos.z)/Vel.z;
		//Pos += Vel*T;
		
		storePos =ivec2(Pos.xy*resolution.x+vec2(resolution.xy)/2);
	}
	return storePos;
}
void main() {
	
	ivec2 storePos = ivec2(fPosition);
	float scaleFactor = min(resolution.x/float(outputResolution.x),resolution.y/float(outputResolution.y));
	ivec2 inputPos = ivec2((storePos-outputResolution.xy*0.5)*scaleFactor+resolution.xy*0.5);

	//storePos=Get3dPosition(storePos);
	vec2 position = (vec2(fPosition.xy)-vec2(resolution.xy)/2)/resolution.x;
	
	
	//storePos =ivec2(position*resolution.x+vec2(resolution.xy)/2);

	vec4 InputColor = texelFetch(sourceTex,inputPos,0);
	int index = int(inputPos.y*resolution.x+inputPos.x);
	DataStruct data = Data[index];
	//double S = sin(Time);
	//double C = cos(Time);
	//C=(C*0.5+0.5)*0.3;

	//double Scale = 1-C*0.3;
	//C=1;
	double RealC = position.x*2*Zoom+CameraReal;
	double ImagC = position.y*2*Zoom+CameraImag;
	double RealZ = 0;
	double ImagZ = 0;
	if(RealC*RealC+ImagC*ImagC<0.0001)
	{
	//imageStore(destTex, storePos, vec4(1,1,1,1.0f));
		//return;
	}
	if(Julia==1)
	{
		RealZ = RealC;
		ImagZ = ImagC;
		RealC = JuliaReal;
		ImagC = JuliaImag;
	}
	//int L;
	//float E = 0;
	
	//C=-0.866;
	//S=0.5;
	float Close = 20;
	dvec2 A = dvec2(0,2);
	dvec2 Z = dvec2(RealZ,ImagZ);
	dvec2 C = dvec2(RealC,ImagC);
	dvec2 PowerC = dvec2(1,0);
	for(int i=0;i<ArrayMaxZ;i++)
	{
		PolynomialConstants[i]=dvec2(0,0);
	}
	for(int j=0;j<ArrayMaxC;j++)
	{
		for(int i=0;i<ArrayMaxZ;i++)
		{
			int k = 2*(i+ArrayMaxZ*j);
			dvec2 M = dvec2(CoefficientArray[k],CoefficientArray[k+1]);
			PolynomialConstants[i]+=Mult(PowerC,M);
		}
		PowerC=Mult(PowerC,C);
	}
	
	
	double L = data.IterationCount;
	//double L = InputColor.x;
	//L=0;
	double DistEstimate = data.DistEstimate;
	float Dist = float(data.MinDistance);
	vec3 Col = vec3(InteriorColor);
	
	if(L >0)
	{
	//DistEstimate =GetApproximateDistanceEstimate(storePos);
		//float K = ColorOffset- ColorScale * (float(L)/200);
		float K = Lerp(
		ColorOffset- ColorScale * (float(L)/200),
		ColorOffset- ColorScale * (-log(float(DistEstimate))/40),
		DistanceEstimateColoringLerp);
		//K+=Time*0.2;
		//K=Dist*0.03;
		
		float a = exp(-0.1*max(float(L)-8,0));
		//float a =0;
		float s = 0.85*(1-a*0.2);
		a*=0.2;
		float Angle = atan(float(data.EndPoint.y),float(data.EndPoint.x))+Time;
		Angle=mod(Angle+Tau/2,Tau)-Tau/2;
		float A = exp(-Angle*Angle*8);
		K = mod(K ,1.0);
		//Col = vec3(K);
		Col = a+GetExteriorColor(1-K)*s;
		

		//Col *=(GetLight(storePos)*0.5+1);
		//Col = vec3(GetLight(storePos),0,-GetLight(storePos))*0.5;
		
		//Col.r = a+hueValue2(K) * s;
		//Col.g = a+hueValue2(K + 0.33) * s;
		//Col.b = a+hueValue2(K + 0.66) * s;
		Col +=(GetLight(inputPos)*0.3);
		//Col = FancyColor(K);
		//Col=vec3((E+L)/100.0);
		double FractionalIteration = fract(data.IterationCount);
		//Col.x+=float(FractionalIteration);
		//Col+=A*0.2;
	}
	else
	{
		
		if(QuaternionJulia==0)
		{
			/*int t = int(Time/2);
			int a = int(mod(data.Period+t+data.Period/3,3));
			if(a==0)
			{
				Col.r+=1;
			}
			if(a==1)
			{
				Col.g+=1;
			}if(a==2)
			{
				Col+=1;
			}*/
			if(FinalDotStrength==0)
			{
				float V = float(-0.06*logd(data.MinDistance))*CenterDotStrength;
				Col+=vec3(V*V,V,sqrt(V))*V;
			}else
			{
				float V = float(-0.12*logd(length(data.EndPoint-FinalDotPosition)))*CenterDotStrength;
				Col+=vec3(V*V,V,sqrt(V))*V;
			}

		}
		if(Julia==1)
		{
			
		}
		//float Q = sqrt(Close);
		//float s = 0.6366*atan(100*Q);
		//float K = Q*2;
		//r=g=b=1-s;

	}
	if(PeriodHighlight<0)
		{
			int P = min(PeriodHighlight,data.Period);
			//P=PeriodHighlight;
			P=6;
			dvec2 Z0 = C;
			for(int i =0;i<0;i++)//newtons method
			{
				dvec2 Z1=Z0;
				dvec2 DZ1=vec2(1,0);
				for(int j=0;j<P;j++)
				{
				  DZ1=Mult(ComputeDerivative(Z1),DZ1);
				  Z1=Compute(Z1);
				}
				dvec2 Change = Div((Z1-Z0),(DZ1-vec2(1,0)));
				Z0=Z0-Change;
				if(length(Change)<0.1*length(Z0-C))
					break;
			}
			dvec2 FixedPoint = Z0;
			dvec2 DZ0=dvec2(1,0);
			for(int j=0;j<P;j++)
			{
			  DZ0=Mult(ComputeDerivative(Z0),DZ0);
			  Z0=Compute(Z0);
			}
			//if(DZ0.x*DZ0.x+DZ0.y*DZ0.y<1.0)
			{
				
				
					float Angle = atan(float(DZ0.y),float(DZ0.x));
					
					float P = 0.5+0.5*cos(4*pi*pi/Angle);
					
					float R = float(length(DZ0));
					P=P*P*P*P*smoothstep(1-0.5*abs(Angle/pi),1,R);
					float Q = smoothstep(0,1,float((1-abs(DZ0.y*10))*smoothstep(-1,0,DZ0.x*10)));
					vec3 Col2 = vec3(0);
					Col2.r=R*(1-Q);
					Col2.b=R*Q;
					//Col.g+=(P);

					Col2.g=clamp(cos(float(DZ0.y*30))+cos(float(DZ0.x*8-Time*0.5))-1,0,1);
				if(L >0)
				{
					Col=Col+Col2*0.2;
				}else{
					Col=Col2;
				}
			}
		
		}
	/*dvec2 J = dvec2(0,1);
	dvec2 Z1 = dvec2(0);
	Z1=Z;
	dvec2 Z2 = IterationPoint;
	dvec2 Z3 = dvec2(1,0);
	for(int i =0;i<8;i++)
	{
		Z1 = Compute(Z1);
		//Z3 *= Div(dvec2(1,0),Z1-Compute(Z1));
		//Z2 = Mult(Z2,Z2)+C+vec2(Zoom,0)/resolution;
	}
	Z3=(Z1);
    //Z-=J;
			float s =1-exp(-5*float(length(Z)));
			s=1;
			float K = 0.5*atan(float(Z3.y),float(Z3.x))/pi;
			vec3 Col2=vec3(0);
			Col2.r = hueValue2(K) * s;
			Col2.g = hueValue2(K + 0.33) * s;
			Col2.b = hueValue2(K + 0.66) * s;
			Col=LerpColor(Col,Col2,0.7);
			//if(abs(Z3.x)<0.05*(Z3.y))
				//Col+=vec3(1,1,1)*0.3;*/
	if(BuddhaActive>0)
	{
		Col=GetBuddhaColor(storePos)*5;
		if(BuddhaReset>0)
		{
			imageStore(BuddhaTexR,storePos,ivec4(0));
			imageStore(BuddhaTexG,storePos,ivec4(0));
			imageStore(BuddhaTexB,storePos,ivec4(0));
		}
	}
	if(QuaternionJulia==1)
	{
		vec3 BaseColor=Col;
		float Iter = InputColor.z;
		Iter-=0.0;
		Iter=max(0.0,Iter);
		Col = vec3(0.2,0.4,0.7)*(1.0-InputColor.y)*0.8;
		Col+=BaseColor*InputColor.y;
		Col+=vec3(1,1,1)*(1.0-exp(-Iter*0.004))*0.8;
	}
	/*if(min(mod(abs(RealC),1),mod(abs(ImagC),1))<0.005*Zoom)
	{
		Col=vec3(0,0,0);
	}
	if(abs(RealC)<0.005*Zoom)
	{
		Col=vec3(1,0,0);
	}
	if(abs(ImagC)<0.005*Zoom)
	{
		Col=vec3(0,1,0);
	}*/

	fragColor=vec4(Col,1.0f);//+vec4(0.1,0,0,0);//+texelFetch(sourceTex,storePos,0);
	//fragColor=texture2D(sourceTex,fPosition/resolution);
	//imageStore(destTex, storePos, vec4(Col,1.0f));
	//imageStore(reverseTex, storePos, InputColor);
	
}
