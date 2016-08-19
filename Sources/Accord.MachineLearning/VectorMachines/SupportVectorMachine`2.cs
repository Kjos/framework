﻿// Accord Machine Learning Library
// The Accord.NET Framework
// http://accord-framework.net
//
// Copyright © César Souza, 2009-2016
// cesarsouza at gmail.com
//
//    This library is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 2.1 of the License, or (at your option) any later version.
//
//    This library is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//    Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public
//    License along with this library; if not, write to the Free Software
//    Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
//

namespace Accord.MachineLearning.VectorMachines
{
    using System;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using Accord.Statistics.Kernels;
    using Accord.Statistics.Links;
    using Accord.Statistics.Models.Regression;
    using Accord.Math;
    using Accord.MachineLearning;
    using Accord.Statistics;

    /// <summary>
    ///  Sparse Kernel Support Vector Machine (kSVM)
    /// </summary>
    /// 
    /// <remarks>
    /// <para>
    ///   The original optimal hyperplane algorithm (SVM) proposed by Vladimir Vapnik in 1963 was a
    ///   linear classifier. However, in 1992, Bernhard Boser, Isabelle Guyon and Vapnik suggested
    ///   a way to create non-linear classifiers by applying the kernel trick (originally proposed
    ///   by Aizerman et al.) to maximum-margin hyperplanes. The resulting algorithm is formally
    ///   similar, except that every dot product is replaced by a non-linear kernel function.</para>
    /// <para>
    ///   This allows the algorithm to fit the maximum-margin hyperplane in a transformed feature space.
    ///   The transformation may be non-linear and the transformed space high dimensional; thus though
    ///   the classifier is a hyperplane in the high-dimensional feature space, it may be non-linear in
    ///   the original input space.</para> 
    ///   
    /// <para>
    ///   The machines are also able to learn sequence classification problems in which the input vectors
    ///   can have arbitrary length. For an example on how to do that, please see the documentation page 
    ///   for the <see cref="DynamicTimeWarping">DynamicTimeWarping kernel</see>.</para>
    ///   
    /// <para>
    ///   References:
    ///   <list type="bullet">
    ///     <item><description><a href="http://en.wikipedia.org/wiki/Support_vector_machine">
    ///       http://en.wikipedia.org/wiki/Support_vector_machine </a></description></item>
    ///     <item><description><a href="http://www.kernel-machines.org/">
    ///       http://www.kernel-machines.org/ </a></description></item>
    ///   </list></para>  
    /// </remarks>
    /// 
    /// <example>
    ///   <code>
    ///   // Example XOR problem
    ///   double[][] inputs =
    ///   {
    ///       new double[] { 0, 0 }, // 0 xor 0: 1 (label +1)
    ///       new double[] { 0, 1 }, // 0 xor 1: 0 (label -1)
    ///       new double[] { 1, 0 }, // 1 xor 0: 0 (label -1)
    ///       new double[] { 1, 1 }  // 1 xor 1: 1 (label +1)
    ///   };
    ///   
    ///   // Dichotomy SVM outputs should be given as [-1;+1]
    ///   int[] labels =
    ///   {
    ///       // 1,  0,  0, 1
    ///          1, -1, -1, 1
    ///   };
    ///   
    ///   // Create a Kernel Support Vector Machine for the given inputs
    ///   KernelSupportVectorMachine machine = new KernelSupportVectorMachine(new Gaussian(0.1), inputs[0].Length);
    ///   
    ///   // Instantiate a new learning algorithm for SVMs
    ///   SequentialMinimalOptimization smo = new SequentialMinimalOptimization(machine, inputs, labels);
    ///   
    ///   // Set up the learning algorithm
    ///   smo.Complexity = 1.0;
    ///   
    ///   // Run the learning algorithm
    ///   double error = smo.Run();
    ///   
    ///   // Compute the decision output for one of the input vectors
    ///   int decision = System.Math.Sign(machine.Compute(inputs[0]));
    ///   </code>
    /// </example>
    /// 
    /// <seealso cref="Accord.Statistics.Kernels"/>
    /// <seealso cref="KernelSupportVectorMachine"/>
    /// <seealso cref="MulticlassSupportVectorMachine"/>
    /// <seealso cref="MultilabelSupportVectorMachine"/>
    /// 
    /// <seealso cref="Accord.MachineLearning.VectorMachines.Learning.SequentialMinimalOptimization"/>
    /// 
    [Serializable]
    public class SupportVectorMachine<TKernel, TInput> :
        BinaryLikelihoodClassifierBase<TInput>,
        ISupportVectorMachine<TInput>, ICloneable
        where TKernel : IKernel<TInput>
        where TInput : ICloneable
    {

        private TKernel kernel;
        private TInput[] supportVectors;
        private double[] weights;
        private double threshold;

        /// <summary>
        ///   Gets or sets the kernel used by this machine.
        /// </summary>
        /// 
        public TKernel Kernel
        {
            get { return kernel; }
            set { kernel = value; }
        }

        /// <summary>
        ///   Gets whether this machine has been calibrated to
        ///   produce probabilistic outputs (through the <see cref="BinaryLikelihoodClassifierBase{TInput}.Probability(TInput)"/>
        ///   method).
        /// </summary>
        /// 
        public bool IsProbabilistic { get; set; }

        /// <summary>
        ///   Gets or sets the collection of support vectors used by this machine.
        /// </summary>
        /// 
        public TInput[] SupportVectors
        {
            get { return supportVectors; }
            set { supportVectors = value; }
        }

        /// <summary>
        ///   Gets or sets the collection of weights used by this machine.
        /// </summary>
        /// 
        public double[] Weights
        {
            get { return weights; }
            set { weights = value; }
        }

        /// <summary>
        ///   Gets or sets the threshold (bias) term for this machine.
        /// </summary>
        /// 
        public double Threshold
        {
            get { return threshold; }
            set { threshold = value; }
        }


        /// <summary>
        ///   Initializes a new instance of the <see cref="SupportVectorMachine{TKernel, TInput}"/> class.
        /// </summary>
        /// 
        /// <param name="inputs">The length of the input vectors expected by the machine.</param>
        /// <param name="kernel">The kernel function to be used.</param>
        /// 
        public SupportVectorMachine(int inputs, TKernel kernel)
        {
            this.NumberOfInputs = inputs;
            this.NumberOfOutputs = 2;
            this.Kernel = kernel;
        }


        /// <summary>
        /// Computes a class-label decision for a given <paramref name="input" />.
        /// </summary>
        /// <param name="input">The input vector that should be classified into
        /// one of the <see cref="Accord.MachineLearning.ITransform.NumberOfOutputs" /> possible classes.</param>
        /// <returns>
        /// A class-label that best described <paramref name="input" /> according
        /// to this classifier.
        /// </returns>
        public override bool Decide(TInput input)
        {
            double sum = threshold;
            for (int j = 0; j < supportVectors.Length; j++)
                sum += weights[j] * kernel.Function(supportVectors[j], input);
            return Classes.Decide(sum);
        }

        /// <summary>
        /// Computes a numerical score measuring the association between
        /// the given <paramref name="input" /> vector and its most strongly
        /// associated class (as predicted by the classifier).
        /// </summary>
        /// <param name="input">The input vector.</param>
        public override double Score(TInput input)
        {
            double sum = threshold;
            for (int j = 0; j < supportVectors.Length; j++)
                sum += weights[j] * kernel.Function(supportVectors[j], input);
            return sum;
        }

        /// <summary>
        /// Predicts a class label vector for the given input vector, returning the
        /// log-likelihood that the input vector belongs to its predicted class.
        /// </summary>
        /// <param name="input">The input vector.</param>
        public override double LogLikelihood(TInput input)
        {
            double sum = threshold;
            for (int j = 0; j < supportVectors.Length; j++)
                sum += weights[j] * kernel.Function(supportVectors[j], input);
            return -Special.Log1pexp(-sum);
        }

        /// <summary>
        /// Predicts a class label vector for the given input vector, returning the
        /// log-likelihood that the input vector belongs to its predicted class.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <param name="decision">The class label predicted by the classifier.</param>
        public override double LogLikelihood(TInput input, out bool decision)
        {
            double sum = threshold;
            for (int j = 0; j < supportVectors.Length; j++)
                sum += weights[j] * kernel.Function(supportVectors[j], input);
            decision = Classes.Decide(sum);
            return -Special.Log1pexp(-sum);
        }

        /// <summary>
        ///   If this machine has a linear kernel, compresses all
        ///   support vectors into a single parameter vector.
        /// </summary>
        /// 
        public virtual void Compress()
        {
            var linear = Kernel as ILinear<TInput>;

            if (linear == null)
                throw new InvalidOperationException();

            double bias;
            var weights = linear.Compress(Weights, SupportVectors, out bias);

            SupportVectors = new[] { weights };
            Weights = new[] { 1.0 };
            Threshold += bias;

            // TODO: call support vector reduction here
        }


        /// <summary>
        ///   Computes the given input to produce the corresponding output.
        /// </summary>
        /// 
        /// <remarks>
        ///   For a binary decision problem, the decision for the negative
        ///   or positive class is typically computed by taking the sign of
        ///   the machine's output.
        /// </remarks>
        /// 
        /// <param name="inputs">An input vector.</param>
        /// <param name="output">The output of the machine. If this is a 
        ///   <see cref="IsProbabilistic">probabilistic</see> machine, the
        ///   output is the probability of the positive class. If this is
        ///   a standard machine, the output is the distance to the decision
        ///   hyperplane in feature space.</param>
        /// 
        /// <returns>The decision label for the given input.</returns>
        /// 
        [Obsolete("Please use the Decide or Transform methods instead.")]
        public virtual int Compute(TInput inputs, out double output)
        {
            bool decision;
            if (IsProbabilistic)
                output = Probability(inputs, out decision);
            else
                output = Score(inputs, out decision);
            return Classes.ToMinusOnePlusOne(decision);
        }



        /// <summary>
        ///   Computes the given input to produce the corresponding output.
        /// </summary>
        /// 
        /// <remarks>
        ///   For a binary decision problem, the decision for the negative
        ///   or positive class is typically computed by taking the sign of
        ///   the machine's output.
        /// </remarks>
        /// 
        /// <param name="inputs">An input vector.</param>
        /// 
        /// <returns>The output for the given input. In a typical classification
        /// problem, the sign of this value should be considered as the class label.</returns>
        ///  
        [Obsolete("Please use the Decide or Transform methods instead.")]
        public double Compute(TInput inputs)
        {
            if (IsProbabilistic)
                return Probability(inputs);
            return Score(inputs);
        }

        /// <summary>
        ///   Creates a new <see cref="SupportVectorMachine"/> that is
        ///   completely equivalent to a <see cref="LogisticRegression"/>.
        /// </summary>
        /// 
        /// <param name="regression">The <see cref="LogisticRegression"/> to be converted.</param>
        /// 
        /// <returns>
        ///   A <see cref="SupportVectorMachine"/> whose linear weights are
        ///   equivalent to the given <see cref="LogisticRegression"/>'s
        ///   <see cref="GeneralizedLinearRegression.Coefficients"> linear 
        ///   coefficients</see>, properly configured with a <see cref="LogLinkFunction"/>. 
        /// </returns>
        /// 
        public static SupportVectorMachine FromLogisticRegression(LogisticRegression regression)
        {
            return FromWeights(regression.Coefficients, interceptIndex: 0);
        }

        /// <summary>
        ///   Creates a new linear <see cref="SupportVectorMachine"/> 
        ///   with the given set of linear <paramref name="weights"/>.
        /// </summary>
        /// 
        /// <param name="weights">The machine's linear coefficients.</param>
        /// <param name="interceptIndex">The index of the intercept term in the given weights vector.</param>
        /// 
        /// <returns>
        ///   A <see cref="SupportVectorMachine"/> whose linear coefficients
        ///   are defined by the given <paramref name="weights"/> vector.
        /// </returns>
        /// 
        public static SupportVectorMachine FromWeights(double[] weights, int interceptIndex = -1)
        {
            double[] newWeights = weights;
            double bias = 0;

            if (interceptIndex >= 0)
            {
                newWeights = new double[weights.Length - 1];
                for (int i = 0, j = 0; i < weights.Length; i++)
                    if (i != interceptIndex)
                        newWeights[j++] = weights[i];
                bias = weights[interceptIndex];
            }

            return new SupportVectorMachine(newWeights.Length)
            {
                Weights = newWeights,
                Threshold = bias
            };
        }

        /// <summary>
        ///   Converts a <see cref="Accord.Statistics.Kernels.Linear"/>-kernel
        ///   machine into an array of linear coefficients. The first position
        ///   in the array is the <see cref="Threshold"/> value.
        /// </summary>
        /// 
        /// <returns>
        ///   An array of linear coefficients representing this machine.
        /// </returns>
        /// 
        public virtual double[] ToWeights()
        {
            var w = new double[weights.Length + 1];
            for (int i = 0; i < weights.Length; i++)
                w[i + 1] = weights[i];
            w[0] = threshold;

            return w;
        }



        /// <summary>
        ///   Gets the number of inputs accepted by this machine.
        /// </summary>
        /// 
        /// <remarks>
        ///   If the number of inputs is zero, this means the machine
        ///   accepts a indefinite number of inputs. This is often the
        ///   case for kernel vector machines using a sequence kernel.
        /// </remarks>
        /// 
        [Obsolete("Please use NumberOfInputs instead.")]
        public int Inputs
        {
            get { return NumberOfInputs; }
        }

        /// <summary>
        ///   Obsolete.
        /// </summary>
        [Obsolete("A machine in compact form is simply a machine with a single support vector.")]
        public bool IsCompact
        {
            get { return this.SupportVectors.Length == 1; }
        }

        IKernel<TInput> ISupportVectorMachine<TInput>.Kernel
        {
            get { return Kernel; }
            set { Kernel = (TKernel)value; }
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new object that is a copy of this instance.
        /// </returns>
        public virtual object Clone()
        {
            var clone = new SupportVectorMachine<TKernel, TInput>(NumberOfInputs, Kernel);
            clone.SupportVectors = (TInput[])SupportVectors.Clone();
            clone.Weights = (double[])Weights.Clone();
            clone.IsProbabilistic = IsProbabilistic;
            clone.Threshold = Threshold;
            return clone;
        }
    }
}