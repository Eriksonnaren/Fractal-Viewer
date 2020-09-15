#version 400
uniform vec2 resolution;
uniform int Iter;
in vec2 fPosition;
out vec4 fragColor;
uniform mat4 projectionMatrix;
uniform double JuliaReal;
uniform double JuliaImag;
uniform double[64] CoefficientArray;
uniform int ArrayMaxZ;
uniform int ArrayMaxC;
dvec4[8] PolynomialConstants;
uniform vec3 CameraPos=vec3(2,0,2);
uniform int Cutoff;
dvec4 Mult(dvec4 Q1, dvec4 Q2)
{
	return dvec4(
    Q1.x*Q2.x-Q1.y*Q2.y-Q1.z*Q2.z-Q1.w*Q2.w,
    Q1.x*Q2.y+Q1.y*Q2.x+Q1.z*Q2.w-Q1.w*Q2.z,
    Q1.x*Q2.z-Q1.y*Q2.w+Q1.z*Q2.x+Q1.w*Q2.y,
    Q1.x*Q2.w+Q1.y*Q2.z-Q1.z*Q2.y+Q1.w*Q2.x 
    );
}
dvec4 Compute(dvec4 Z)
{
	dvec4 NewZ=dvec4(0);
	for(int i =ArrayMaxZ-1;i>=0;i--)
	{
	   NewZ=Mult(Z,NewZ)+PolynomialConstants[i];
	}
	return NewZ;
}
dvec4 ComputeDerivative(dvec4 Z)
{
	dvec4 NewZ=dvec4(0);
	for(int i =ArrayMaxZ-1;i>=1;i--)
	{
	  NewZ=Mult(Z,NewZ)+PolynomialConstants[i]*i;
	}
	return NewZ;
}
vec4 GetQuat(vec3 V)
{
	return vec4(V.y,V.z,V.x,0.0);
}
vec3 HueColor(float H)
{
	return 0.5*(sin(H+vec3(0,2.094,4.188))+1.0);
}
vec3 GetColor(float Iter,float Hit,vec3 BaseColor)
{
    Iter-=0.0;
    Iter=max(0.0,Iter);
    vec3 Col = vec3(0.2,0.4,0.7)*(1.0-Hit)*0.8;
    Col+=BaseColor*Hit;
    Col+=vec3(1,1,1)*(1.0-exp(-Iter*0.004))*0.8;
	return Col;
}
vec2 DistanceEstimate(dvec4 A)
{
    dvec4 DZ = dvec4(1,0,0,0);
    dvec4 Z = dvec4(A);
    
    for(int i =0;i<100;i++)
    {
       DZ=Mult(ComputeDerivative(Z),DZ);
	   Z=Compute(Z);
        if(Z.x*Z.x+Z.y*Z.y+Z.z*Z.z+Z.w*Z.w>10.0)
        {
            double r = length(Z);
    		double dr = length(DZ);
    		return vec2(r*log(float(r))/(dr),i);
            
        }
    }
    return vec2(-1.0,0);
    
}
vec3 Ray(vec3 Pos,vec3 Vel)
{
	if(Cutoff)
		Pos+=Vel*(-Pos.x/Vel.x);
    vec3 Background=vec3(0,0,0);
    vec3 Color=vec3(0,0,0);
    float CloudDensity=0.0;
    float iter=0.0;
	for(int i =0;i<300;i++)
    {
        vec4 Quat = GetQuat(Pos);
        vec2 Output = DistanceEstimate(Quat);
        float Dist = Output.x;
        Dist = Dist*0.5;
        if(Dist<0.0006)
        {
        	return vec3(iter,1.0,Output.y);
        }
        if(length(Pos)>3.0)
        {
        	return vec3(iter,0.0,0);
        }
        iter+=exp(-4.0*Dist);
        Pos+=Vel*Dist;
    }
    return vec3(iter,0.0,0);
	//return Color+Background+vec3(1,1,1)*0.8*(1.0-exp(-CloudDensity));
}
void main() {
	ivec2 storePos = ivec2(fPosition);
	vec2 position = 1.0*vec2(fPosition - resolution.xy*0.5)/resolution.x;
	mat3 CameraMatrix = mat3(
	normalize(CameraPos),
	vec3(0),
	vec3(0)
	);
	CameraMatrix[1]=normalize(cross(CameraPos,vec3(0,0,1)));
	CameraMatrix[2]=normalize(cross(CameraPos,CameraMatrix[1]));
	
	vec3 RayVel = -normalize(CameraMatrix*vec3(1,position.x,position.y));
	dvec4 C = dvec4(JuliaReal,JuliaImag,0,0);
	dvec4 PowerC = dvec4(1,0,0,0);
	for(int i=0;i<ArrayMaxZ;i++)
	{
		PolynomialConstants[i]=dvec4(0,0,0,0);
	}
	for(int j=0;j<ArrayMaxC;j++)
	{
		for(int i=0;i<ArrayMaxZ;i++)
		{
			int k = 2*(i+ArrayMaxZ*j);
			dvec4 M = dvec4(CoefficientArray[k],CoefficientArray[k+1],0,0);
			PolynomialConstants[i]+=Mult(PowerC,M);
		}
		PowerC=Mult(PowerC,C);
	}
	vec3 Out = Ray(CameraPos,RayVel);
	fragColor = vec4(Out.z*Out.y,Out.y,Out.x,1);
}