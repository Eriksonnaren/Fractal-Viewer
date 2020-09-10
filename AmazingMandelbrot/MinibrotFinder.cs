using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmazingMandelbrot
{
    class MinibrotFinder
    {
        int Order = 10;
        int Resolution = 50;
        public struct MinibrotInfo
        {
            public int Order;
            public Complex Pos;
            public Complex Cusp;
            public MinibrotInfo(int order, Complex pos, Complex cusp)
            {
                Order = order;
                Pos = pos;
                Cusp = cusp;
            }
        }
        List<MinibrotInfo> Info;
        public List<MinibrotInfo> GetMinibrots(int Order,Complex Corner1,Complex Corner2)
        {
            Info = new List<MinibrotInfo>();
            this.Order = Order;
            RecurseMinibrots(Corner1, Corner2,10);
            return Info;
        }
        void RecurseMinibrots(Complex Corner1, Complex Corner2,int ExtraIterations)
        {
            int Roots = GetRootsInRegion(Corner1, Corner2);
            if(Roots==0)
            {
                return;
            }else if(Roots==1)
            {
                ExtraIterations--;
            }
            //Rectangles.Add(new Tuple<Complex,Complex>(Corner1, Corner2));
            if (ExtraIterations==0)
            {
                Complex C = (Corner1 + Corner2) / 2;
                for (int i = 0; i < 10; i++)
                {
                    DerivativeC(new Complex(), C, out Complex Der, out Complex Der2);
                    C -= Function(new Complex(),C,out bool B) / Der;
                }
                if(IsRootValid(C,out Complex Cusp))
                    Info.Add(new MinibrotInfo(Order,C , Cusp));
            }
            else
            {
                if(Corner2.real-Corner1.real> Corner2.imag - Corner1.imag)
                {
                    double mid = (Corner1.real + Corner2.real) / 2;
                    RecurseMinibrots(Corner1, new Complex(mid, Corner2.imag), ExtraIterations);
                    RecurseMinibrots(new Complex(mid, Corner1.imag), Corner2, ExtraIterations);
                }
                else
                {
                    double mid = (Corner1.imag + Corner2.imag) / 2;
                    RecurseMinibrots(Corner1, new Complex(Corner2.real, mid), ExtraIterations);
                    RecurseMinibrots(new Complex(Corner1.real, mid), Corner2, ExtraIterations);
                }
            }

        }
        bool IsRootValid(Complex C, out Complex Cusp)
        {
            Complex Start = C;
            Complex Z = C;
            /*for (int i = 1; i < Order-1; i++)
            {
                Z = Z * Z + C;
                if(Z.MagSq()<0.001)
                {
                    Cusp = Z;
                    return false;
                }
            }*/
            Z = C;
            Complex Der2Z=new Complex();
            for (int i = 0; i < 20; i++)
            {
                //function 1 is Function(Z,C)-Z=0
                //function 2 is DerivativeZ(Z,C)-1=0
                Complex Der1Z;
                DerivativesZ(Z,C,out Der1Z, out Der2Z);
                Complex Der1C;
                DerivativeC(Z,C, out Der1C,out Complex SecondC);
                Complex Der2C = DerivativeCZ(Z,C);
                Complex[,] Jacob = new Complex[,] { 
                    { Der1Z-1, Der1C }, 
                    { Der2Z, Der2C }
                };
                Complex Determinant = Jacob[0, 0] * Jacob[1, 1]- Jacob[1, 0] * Jacob[0, 1];
                Complex[,] InverseJacob = new Complex[,]{
                    {Jacob[1, 1]/Determinant,-Jacob[0, 1]/Determinant },
                    {-Jacob[1, 0]/Determinant, Jacob[0, 0]/Determinant}
                };
                Complex[] Vector = { Function(Z, C, out bool Bail)-Z, Der1Z-1 };
                Z -= (Vector[0] * InverseJacob[0, 0] + Vector[1] * InverseJacob[0, 1])/2;
                C -= (Vector[0] * InverseJacob[1, 0] + Vector[1] * InverseJacob[1, 1])/2;

                if (C.MagSq() > 4|| Z.MagSq() > 4)
                {
                    Cusp = C;
                    return false;
                }
            }
            Cusp = C;
            Complex RelativeCusp = (Cusp - Start);
            Complex A = Function(Z - RelativeCusp, C, out bool Bail2);
            Complex B = Function(Z + RelativeCusp, C, out Bail2);
            double M = Math.Sqrt(2*Der2Z.MagSq() * RelativeCusp.MagSq());
            if (M>0.2)
            return true;
            else
            {
                return false;
            }
        }
        public int GetRootsInRegion(Complex Corner1, Complex Corner2)
        {
            bool IsInside = false;
            bool Bail;
            Complex PreviousNum = Function(new Complex(), Corner1,out Bail);
            if (!Bail)
                IsInside=true;
            Complex StartNum = PreviousNum;
            double PreviousDot=1;
            double PreviousCross=0;
            int Count = 0;
            for (int i = 0; i < 4; i++)
            {
                Complex Start = new Complex((i & 2) == 0 ? Corner1.real : Corner2.real, ((i + 1) & 2) == 0 ? Corner1.imag : Corner2.imag);
                Complex End = new Complex(((i+1) & 2) == 0 ? Corner1.real : Corner2.real, ((i + 2) & 2) == 0 ? Corner1.imag : Corner2.imag);
                for (int j = 0; j < Resolution; j++)
                {
                    Complex Current = Function(new Complex(),LerpC(Start, End, j / (double)Resolution), out Bail);
                    if (!Bail)
                        IsInside = true;
                    double Dot = Current.real * StartNum.real+Current.imag*StartNum.imag;
                    double Cross = Current.real * StartNum.imag - Current.imag * StartNum.real;
                    if (Cross * PreviousCross < 0)
                    {
                        if (PreviousDot < 0 && Dot < 0)
                        {
                            if (Cross < 0 && PreviousCross > 0)
                            {
                                Count++;
                            } else if (Cross > 0 && PreviousCross < 0)
                            {
                                Count--;
                            }
                        }else if (PreviousDot*Dot<0)
                        {
                            Count++;//may cauce double counting, must be cleaned up later
                        }
                    }
                    PreviousDot = Dot;
                    PreviousCross = Cross;
                }
            }
            if (!IsInside)
                return 0;
            return Count;
        }
        Complex LerpC(Complex A, Complex B, double T) => A * (1 - T) + B * T;
        Complex Function(Complex Z,Complex C,out bool Bail)
        {
            Bail = false;
            for (int i = 0; i < Order; i++)
            {
                Z = Z * Z + C;
                if(Z.MagSq()>4)
                {
                    Bail = true;
                    if (Z.MagSq() > 100)
                    {
                        Z /= Math.Sqrt(Z.MagSq() / 100);
                    }
                }
            }
            return Z;
        }
        void DerivativesZ(Complex Z, Complex C,out Complex Derivative,out Complex SecondDerivative)
        {
            Complex Function = Z;
            Derivative = new Complex(0,0);
            SecondDerivative = new Complex(2,0);
            for (int i = 0; i < Order; i++)
            {
                SecondDerivative = 2 * (Function * SecondDerivative + Derivative * Derivative);
                if(i==0)
                    Derivative = new Complex(1, 0);
                Derivative = 2 * Function * Derivative;
                Function = Function * Function + C;
            }
        }
        void DerivativeC(Complex Z, Complex C,out Complex Derivative,out Complex SecondDerivative)
        {
            Complex Function = Z;
            Derivative = new Complex();
            SecondDerivative = new Complex();
            for (int i = 0; i < Order; i++)
            {
                SecondDerivative = 2* (Function * SecondDerivative + Derivative * Derivative);
                Derivative = 2 * Function * Derivative + 1;
                Function = Function * Function + C;
            }
        }
        Complex DerivativeCZ(Complex Z, Complex C)
        {
            Complex Function = Z;
            Complex DerivativeZ = new Complex(1, 0);// d/dz f(z,c)
            Complex DerivativeC = new Complex();// d/dc f(z,c)
            Complex DerivativeCZ = new Complex();// d/dc DerivativeZ
            for (int i = 0; i < Order-1; i++)
            {
                DerivativeZ = 2 * Function * DerivativeZ;
                DerivativeC = 2 * Function * DerivativeC + 1;
                Function = Function * Function + C;
                DerivativeCZ = 2 * (Function * DerivativeCZ + DerivativeZ * DerivativeC);
            }
            return DerivativeCZ;
        }
    }
}
