﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;
using OpenTK;

namespace AmazingMandelbrot
{
    class SoundGenerator
    {
        WaveOut waveOut;
        FractalWaveProvider waveProvider;
        public Complex C;
        public Complex[,] CoefficientArray;
        public bool Playing = false;
        public double FrequencyScale=1;
        public double VolumeScale=1;
        public SoundGenerator()
        {
            waveOut = new WaveOut();
            waveProvider=new FractalWaveProvider();
            waveOut.Init(waveProvider);
        }
        public void Start()
        {
            waveOut.Play();
            Playing = true;
        }
        public void UpdateParameters()
        {
            waveProvider.C = C;
            waveProvider.fractalMath.CoefficientArray = CoefficientArray;
            waveProvider.period = (int)(100 / FrequencyScale);
            waveProvider.Volume = VolumeScale;
        }
        public void Stop()
        {
            waveOut.Stop();
            Playing = false;
        }
    }
    class FractalWaveProvider : ISampleProvider
    {
        public WaveFormat WaveFormat { get; private set; }
        public FractalMath fractalMath;
        int Phase = 0;
        public int period = 100;
        public double Volume=1;
        public Complex Z = new Complex();
        public Complex C = new Complex();
        Complex AveragePoint=new Complex();
        Vector2 RotationPoint=new Vector2(1,0);
        double PreviousHeight;
        double Height;
        public FractalWaveProvider(int sampleRate = 44100)
        {
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1);
            fractalMath = new FractalMath();
        }
        public int Read(float[] buffer, int offset, int count)
        {
            
            //set the internal state of the fractalMath helper class to use the proper C value
            fractalMath.SetCoefficients(C);
            for (int n = 0; n < count; n++)
            {
                Phase++;
                if (Phase >= period)//only one fractal step every period amount of buffer steps
                {
                    Phase = 0;
                    PreviousHeight = Height;
                    Complex oldZ = Z;
                    //use helper class to update Z
                    Z = fractalMath.Compute(Z);
                    Vector2 Zv = new Vector2((float)(Z.real - AveragePoint.real), (float)(Z.imag - AveragePoint.imag));
                    Vector2 Projection = Zv * Vector2.Dot(RotationPoint, Zv) / Vector2.Dot(Zv, Zv);
                    //RotationPoint is trying to point along the axis that has the longest difference in Z values
                    RotationPoint += Projection / 10;
                    RotationPoint.Normalize();
                    //AveragePoint is the middlepoint of all the Z values when in a cycle
                    AveragePoint += (Z - AveragePoint) / 10;
                    //the wave height is the projection of (Z-AveragePoint) onto RotationPoint
                    Height = Vector2.Dot(Zv, RotationPoint);
                    //poor attemt at normalizing volume
                    Height = Math.Min(Math.Max(Height, -0.7), 0.7);
                    //reset Z if the iteration diverges or gets stuck
                    if (Z.MagSq() > 6|| (oldZ - Z).MagSq() < 0.001)
                    {
                        Z = new Complex();
                    }
                }
                //lerp parameter between the old Height and the new Height
                double T = Phase / (float)period;
                //use beizer curve to make the lerp value more sine-like
                T = Beizer(T);
                buffer[n + offset] = (float)(PreviousHeight * (1 - T) + Height * (T)) * 0.3f* (float)Volume;
            }
            Phase = Phase % period;
            return count;
        }
        double Beizer(double X)
        {
            return X * X * (3 - X * 2);
        }
    }
}
