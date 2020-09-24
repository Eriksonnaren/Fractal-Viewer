#version 400
struct DataStruct
{
	double IterationCount;
	double MinDistance;
	dvec2 EndPoint;
};
layout(std140) buffer DataBlock
{
  DataStruct Data[];
};
in vec2 fPosition;
out vec4 fragColor;
uniform ivec2 resolution;
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
float GetLight(ivec2 storePos)
{
	dvec3 Sun = normalize(vec3(-1,-1,0.0));
	double HeightScale = 7;
	int index = int(storePos.y*resolution.x+storePos.x);
	double H1 = HeightScale*Data[index].IterationCount;
	double H2 = HeightScale*Data[index+1].IterationCount;
	double H3 = HeightScale*Data[index+resolution.x].IterationCount;
	dvec3 V1 = dvec3(0,0,H1);
	dvec3 V2 = dvec3(1,0,H2);
	dvec3 V3 = dvec3(0,1,H3);
	dvec3 Norm = normalize(cross(V2-V1,V3-V1));
	return float(dot(Norm,Sun));
}
void main() {
	Data[100];
	ivec2 storePos = ivec2(fPosition);
	vec2 position = (fPosition+vec2(0,(resolution.x-resolution.y)/2))/resolution.x;
	vec4 InputColor = texelFetch(sourceTex,storePos,0);
	int index = int(storePos.y*resolution.x+storePos.x);
	DataStruct data = Data[index];
	//double S = sin(Time);
	//double C = cos(Time);
	//C=(C*0.5+0.5)*0.3;

	//double Scale = 1-C*0.3;
	//C=1;
	double RealC = position.x*2*Zoom-Zoom+CameraReal;
	double ImagC = position.y*2*Zoom-Zoom+CameraImag;
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
	float Dist = float(data.MinDistance);
	vec3 Col = vec3(InteriorColor);
	
	if(L >0)
	{
		float K = ColorOffset- ColorScale * (float(L) / 200);
		//K+=Time*0.2;
		//K=Dist*0.03;
		K = mod(K ,1.0);
		float a = exp(-0.1*max(float(L)-8,0));
		//float a =0;
		float s = 0.85*(1-a*0.2);
		a*=0.2;

		a+=GetLight(storePos)*0.3;

		Col = a+GetExteriorColor(1-K)*s;
		//Col.r = a+hueValue2(K) * s;
		//Col.g = a+hueValue2(K + 0.33) * s;
		//Col.b = a+hueValue2(K + 0.66) * s;
		//Col = FancyColor(K);
		//Col=vec3((E+L)/100.0);
		
	}
	else
	{
		if(PeriodHighlight>0)
		{
			
			/*dvec2 Z0 = C;
			for(int i =0;i<5;i++)//newtons method
			{
				dvec2 Z1=Z0;
				dvec2 DZ1=vec2(1,0);
				for(int j=0;j<PeriodHighlight;j++)
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
			for(int j=0;j<PeriodHighlight;j++)
			{
			  DZ0=Mult(ComputeDerivative(Z0),DZ0);
			  Z0=Compute(Z0);
			}
			if(DZ0.x*DZ0.x+DZ0.y*DZ0.y<1.0)
			{
				
				
					float Angle = atan(float(DZ0.y),float(DZ0.x));
					
					float P = 0.5+0.5*cos(4*pi*pi/Angle);
					
					float R = float(length(DZ0));
					P=P*P*P*P*smoothstep(1-0.5*abs(Angle/pi),1,R);
					float Q = smoothstep(0,1,float((1-abs(DZ0.y*10))*smoothstep(-1,0,DZ0.x*10)));
					Col.r=R*(1-Q);
					Col.b=Col.g=R*Q;
					Col.g+=(P);
				
			}*/
		
		}
		if(QuaternionJulia==0)
		{
			float V = -0.06*log(InputColor.z);
			Col+=vec3(V*V,V,sqrt(V))*V;
		}
		if(Julia==1)
		{
			
		}
		//float Q = sqrt(Close);
		//float s = 0.6366*atan(100*Q);
		//float K = Q*2;
		//r=g=b=1-s;

	}
	/*dvec2 J = dvec2(0,1);
	Z=Compute(dvec2(0));
	for(int i =0;i<4;i++)
	{
		Z = Compute(Z);
		//J=Compute(J);
	}
    Z-=J;
			float s =sqrt(float(length(Z)));
			float K = 5*atan(float(Z.y),float(Z.x))/pi;
			vec3 Col2=vec3(0);
			Col2.r = hueValue2(K) * s;
			Col2.g = hueValue2(K + 0.33) * s;
			Col2.b = hueValue2(K + 0.66) * s;
			Col+=Col2*0.5;*/
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
	//fragColor = InputColor;
	//fragColor=texture2D(sourceTex,fPosition/resolution);
	//imageStore(destTex, storePos, vec4(Col,1.0f));
	//imageStore(reverseTex, storePos, InputColor);
	
}
