﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmazingMandelbrot
{
    class FractalMath
    {
        public Complex[,] CoefficientArray;
        public int MaxPeriod = 50;
        public Complex[] PolynomialConstants;//condensed constants to optimize away all of the C coefficients
        Complex[] PolynomialConstantsDerC;//shifted and scaled version of the above to match up with the power rule for derivatives of C
        const double Flowbail = 1000000;
        public int FindPeriod(Complex C)
        {
            PolynomialConstants= GetCoefficients(C, CoefficientArray);
            
            for (int p = 1; p < MaxPeriod; p++)
            {
                Complex Z0 = C;
                for (int i = 0; i < 20; i++)//newtons method
                {
                    Complex Z1 = Z0;
                    Complex DZ1 = new Complex(1, 0);
                    for (int j = 0; j < p; j++)
                    {
                        DZ1 = ComputeDerivative(Z1) * DZ1;
                        Z1 = Compute(Z1);
                    }
                    Z0 -= (Z1 - Z0) / (DZ1 - new Complex(1, 0));
                }
                //Complex FixedPoint = Z0;
                Complex DZ0 = new Complex(1, 0);
                for (int j = 0; j < p; j++)
                {
                    DZ0 = ComputeDerivative(Z0) * DZ0;
                    Z0 = Compute(Z0);
                }
                if (DZ0.MagSq() < 1.0)
                {
                    return p;
                }
            }
            return 0;
        }
        public Complex[] GetCoefficients(Complex C, Complex[,] CoefficientArray)
        {
            Complex[] Output = new Complex[CoefficientArray.GetLength(0)];
            for (int i = 0; i < CoefficientArray.GetLength(0); i++)
            {
                Complex PowerC = new Complex(1, 0);
                for (int j = 0; j < CoefficientArray.GetLength(1); j++)
                {
                    Output[i] += PowerC * CoefficientArray[i, j];
                    PowerC *= C;
                }
                
            }
            return Output;
        }
        public void SetCoefficients(Complex C)
        {
            PolynomialConstants = GetCoefficients(C, CoefficientArray);
        }
        public Complex[] GetDerivativeCoefficients(Complex C, Complex[,] CoefficientArray)
        {
            Complex[] Output = new Complex[CoefficientArray.GetLength(0)];
            for (int i = 0; i < CoefficientArray.GetLength(0); i++)
            {
                Complex PowerC = new Complex(1, 0);
                for (int j = 1; j < CoefficientArray.GetLength(1); j++)
                {
                    Output[i] += PowerC * CoefficientArray[i, j]*j;
                    PowerC *= C;
                }
            }
            return Output;
        }
        public Complex[] GetSecondDerivativeCoefficients(Complex C, Complex[,] CoefficientArray)
        {
            Complex[] Output = new Complex[CoefficientArray.GetLength(0)];
            for (int i = 0; i < CoefficientArray.GetLength(0); i++)
            {
                Complex PowerC = new Complex(1, 0);
                for (int j = 2; j < CoefficientArray.GetLength(1); j++)
                {
                    Output[i] += PowerC * CoefficientArray[i, j] * j*(j-1);
                    PowerC *= C;
                }
            }
            return Output;
        }
        public Complex Compute(Complex Z)
        {
            Complex NewZ = new Complex(0, 0);
            for (int i = CoefficientArray.GetLength(0) - 1; i >= 0; i--)
            {
                NewZ = Z* NewZ + PolynomialConstants[i];
            }
            return NewZ;
        }
        public Complex ComputeDerivative(Complex Z)
        {
            Complex NewZ = new Complex(0, 0);
            for (int i = CoefficientArray.GetLength(0) - 1; i >= 1; i--)
            {
                NewZ = Z * NewZ + PolynomialConstants[i]*i;
            }
            return NewZ;
        }
        public Complex FollowFractalFlow(Complex[,] Polynomial, Complex[,] DifferencePolynomial, double dt, Complex Startpoint,int Iterations,out Complex Divergence)
        {
            Complex[] PolynomialConstants = GetCoefficients(Startpoint, Polynomial);
            Complex[] PolynomialConstantsDerC = GetDerivativeCoefficients(Startpoint, Polynomial);
            Complex[] PolynomialConstantsDerC2 = GetSecondDerivativeCoefficients(Startpoint, Polynomial);
            Complex[] DifferenceConstants = GetCoefficients(Startpoint, DifferencePolynomial);
            Complex[] DifferenceConstantsDerC = GetDerivativeCoefficients(Startpoint, DifferencePolynomial);
            int ArrayMax = Polynomial.GetLength(0)-1;
            int ArrayMax2 = DifferencePolynomial.GetLength(0)-1;

            Complex Z = new Complex();
            Complex DC = new Complex();
            Complex DC2 = new Complex();
            Complex DT = new Complex();
            Complex DT2 = new Complex();
            Complex DCT = new Complex();
            for (int L = 0; L < Iterations; L++)
            {
                Complex DerZ = new Complex();//n*z^(n-1)*c[n]
                Complex DerZ2 = new Complex(); //n*(n-1)*z^(n-2)*c[n]
                Complex DerZC = new Complex();//n*z^(n-1)*dc[n]
                Complex DerC = new Complex();//z^n*dc[n]
                Complex DerC2 = new Complex();//z^n*dc2[n]
                Complex NewZ = new Complex();//z^n*c[n]
                Complex DiffZ = new Complex();
                Complex DiffDerZ = new Complex();
                Complex DiffDerC = new Complex();
                for (int i = ArrayMax2; i >= 1; i--)
                {
                    DiffDerZ = Z * DiffDerZ + DifferenceConstants[i] * i;
                }
                for (int i = ArrayMax2; i >= 0; i--)
                {
                    DiffDerC = Z * DiffDerC + DifferenceConstantsDerC[i];
                    DiffZ = Z * DiffZ + DifferenceConstants[i];
                }

                for (int i = ArrayMax; i >= 2; i--)
                {
                    DerZ2 = Z * DerZ2 + PolynomialConstants[i] * i*(i-1);
                }
                for (int i = ArrayMax; i >= 1; i--)
                {
                    DerZ = Z * DerZ + PolynomialConstants[i] * i;
                    DerZC = Z * DerZC + PolynomialConstantsDerC[i] * i;
                }
                for (int i = ArrayMax; i >= 0; i--)
                {
                    DerC2 = Z * DerC2+ PolynomialConstantsDerC2[i];
                    DerC = Z * DerC + PolynomialConstantsDerC[i];
                    NewZ = Z * NewZ + PolynomialConstants[i];
                }

                DCT = DerZ * DCT + DT * DC * DerZ2 + DT * DerZC + DC * DiffDerZ + DiffDerC;

                DT2 = DerZ * DT2 + DT * DT * DerZ2 + 2 * DT * DiffDerZ;
                DC2 = DerZ * DC2 + DC * DC * DerZ2 + 2 * DC * DerZC + DerC2;

                DT = DerZ * DT + DiffZ;
                DC = DerZ * DC + DerC;

                Z = NewZ;
                if (DC.MagSq() > Flowbail || DT.MagSq() > Flowbail)
                {
                    break;
                }
            }
            Complex B = -DC / DT;
            Complex A = -(2 * B * DCT + B * B * DT2 + DC2) / DT;
            double C = -dt;
            Complex Discriminant = B*B - 2 * A* C;
            Complex Solution1 = (-B + Discriminant.Sqrt()) / A;
            Complex Solution2 = (-B - Discriminant.Sqrt()) / A;
            Complex Movement = Solution1.MagSq() < Solution2.MagSq() ? Solution1 : Solution2;

            Divergence = (DC2 * DT - DCT * DC) / (DC* DC);

            return Startpoint + Movement;
        }

        public Complex GetFractalFlow(Complex[,] Polynomial, Complex[,] DifferencePolynomial, Complex Startpoint, int Iterations)
        {
            Complex[] PolynomialConstants = GetCoefficients(Startpoint, Polynomial);
            Complex[] PolynomialConstantsDerC = GetDerivativeCoefficients(Startpoint, Polynomial);
            Complex[] DifferenceConstants = GetCoefficients(Startpoint, DifferencePolynomial);
            int ArrayMaxZ = Polynomial.GetLength(0);
            int ArrayMax = ArrayMaxZ - 1;
            Complex Z = new Complex();
            Complex DC = new Complex();
            Complex DT = new Complex(0, 0);
            for (int L = 0; L < Iterations; L++)
            {
                Complex DerZ = new Complex(0, 0);
                Complex DerC = new Complex(0, 0);
                Complex NewZ = new Complex(0, 0);
                Complex DiffZ = DifferenceConstants[DifferencePolynomial.GetLength(0) - 1];
                for (int i = DifferencePolynomial.GetLength(0) - 2; i >= 0; i--)
                {
                    DiffZ = Z * DiffZ + DifferenceConstants[i];
                }

                for (int i = ArrayMax; i >= 1; i--)
                {
                    DerZ = Z * DerZ + PolynomialConstants[i] * i;
                    DerC = Z * DerC + PolynomialConstantsDerC[i];
                    NewZ = Z * NewZ + PolynomialConstants[i];
                }
                DT = DerZ * DT + DiffZ;

                DC = DC * DerZ + Z * DerC + PolynomialConstantsDerC[0];

                Z = Z * NewZ + PolynomialConstants[0];
                if (DC.MagSq() > Flowbail || DT.MagSq() > Flowbail)
                {
                    break;
                }
            }

            Complex R = -DT / DC;
            return R;
        }
        public static Complex[,] GetCoefficientArrayFromPolynomial(List<PolynomialParser2.PolynomialNode> polynomial,int dependentVariable, Complex[] Constants)
        {
            int MaxZ=0;
            int MaxVar=0;
            for (int i = 0; i < polynomial.Count; i++)
            {
                if(polynomial[i].Variables.ContainsKey(0))
                {
                    MaxZ = Math.Max(MaxZ, polynomial[i].Variables[0]);
                }
                if (polynomial[i].Variables.ContainsKey(dependentVariable)&& dependentVariable>0)
                {
                    MaxVar = Math.Max(MaxVar, polynomial[i].Variables[dependentVariable]);
                }
            }

            Complex[,] arr = new Complex[MaxZ+1, MaxVar+1];
            for (int i = 0; i < polynomial.Count; i++)
            {
                Complex C = polynomial[i].Scalar;
                int IndexZ = 0;
                int IndexVar = 0;
                for (int j = 0; j < polynomial[i].Variables.Count; j++)
                {
                    int v = polynomial[i].Variables.Keys.ElementAt(j);
                    if (v == 0)
                        IndexZ = polynomial[i].Variables[v];
                    else if(v == dependentVariable)
                        IndexVar = polynomial[i].Variables[v];
                    else
                    {
                        for (int k = 0; k < polynomial[i].Variables[v]; k++)
                        {
                            C *= Constants[v];
                        }
                    }
                }
                arr[IndexZ, IndexVar] = C;
            }
            return arr;
        }
    }
}
