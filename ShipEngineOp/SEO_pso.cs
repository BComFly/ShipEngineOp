using System;
using System.Collections.Generic;
using System.Linq;
using KSP.Localization;
using UnityEngine;

namespace ShipEngineOptimization
{
    // Represents a single particle in the PSO swarm
    public class Particle
    {
        public float[] Position { get; set; } // Current position (e.g., [x1, x2, ..., xN-1])
        public float[] Velocity { get; set; } // Current velocity
        public float[] PersonalBestPosition { get; set; } // Best position found by this particle
        public float PersonalBestFitness { get; set; } // Fitness of personal best position

        public Particle(int dimensions)
        {
            Position = new float[dimensions];
            Velocity = new float[dimensions];
            PersonalBestPosition = new float[dimensions];
            PersonalBestFitness = float.MaxValue; // Initialize with a very high value
        }
    }

    // The PSO optimizer class
    public class PsoOptimizer
    {
        private readonly int _dimensions; // Number of independent variables to optimize (N-1)
        private readonly int _numVariables; // Total number of variables (N)
        private readonly int _numParticles;
        private readonly int _maxIterations;
        private readonly float _inertiaWeight; // w
        private readonly float _cognitiveWeight; // c1
        private readonly float _socialWeight; // c2
        private readonly float _minBound; // Lower bound for each independent variable
        private readonly float _maxBound; // Upper bound for each independent variable
        private readonly float _totalDv; // The constant for x1 + x2 + ... + xN = Constant
        private readonly float _epsilon = 0f; // Small value to ensure positivity and avoid division by zero

        // New parameters for convergence stopping criterion
        private readonly float _fitnessTolerance;
        private readonly int _consecutiveConvergedIterations;

        //private System.Random _random;
        

        public float[] GlobalBestPosition { get; private set; } // Best position found by the entire swarm (for N-1 variables)
        public float GlobalBestFitness { get; private set; } // Fitness of global best position

        /// <summary>
        /// Initializes a new instance of the PsoOptimizer class for N variables.
        /// </summary>
        /// <param name="numVariables">The total number of variables (N) to optimize (e.g., 3 for a, b, c).</param>
        /// <param name="numParticles">Number of particles in the swarm.</param>
        /// <param name="maxIterations">Maximum number of iterations for the optimization.</param>
        /// <param name="inertiaWeight">Inertia weight (w).</param>
        /// <param name="cognitiveWeight">Cognitive weight (c1).</param>
        /// <param name="socialWeight">Social weight (c2).</param>
        /// <param name="constantSum">The constant K in x1 + x2 + ... + xN = K.</param>
        /// <param name="fitnessTolerance">The tolerance for fitness variation to consider convergence.</param>
        /// <param name="consecutiveConvergedIterations">Number of consecutive iterations fitness must be within tolerance.</param>
        public PsoOptimizer(int numVariables, int numParticles, int maxIterations,
                            float inertiaWeight, float cognitiveWeight, float socialWeight,
                            float constantSum, float fitnessTolerance, int consecutiveConvergedIterations)
        {
            if (numVariables < 2)
            {
                throw new ArgumentException("Number of variables must be at least 2 to apply the sum constraint.");
            }

            _numVariables = numVariables;
            _dimensions = numVariables - 1; // We optimize for N-1 variables
            _numParticles = numParticles;
            _maxIterations = maxIterations;
            _inertiaWeight = inertiaWeight;
            _cognitiveWeight = cognitiveWeight;
            _socialWeight = socialWeight;
            _totalDv = constantSum;
            _fitnessTolerance = fitnessTolerance;
            _consecutiveConvergedIterations = consecutiveConvergedIterations;

            // Bounds for each of the (N-1) independent variables.
            // Each must be positive, and their sum must leave enough room for the last variable to be positive.
            _minBound = _epsilon;
            // Max value for any single independent variable, ensuring space for other N-2 positive variables
            // and the derived N-th variable to also be positive.
            _maxBound = _totalDv - ((_numVariables - 1) * _epsilon);

            //_random = new Random();

            GlobalBestFitness = float.MaxValue; // Initialize with a very high value
            GlobalBestPosition = new float[_dimensions];
        }

        /// <summary>
        /// The objective function f(x) = x + 1/x.
        /// </summary>
        private float Fx(float x)
        {
            // Apply a large penalty if x is too small or non-positive
            if (x <= _epsilon)
            {
                return float.MaxValue;
            }
            return x + (1.0f / x);
        }

        /// <summary>
        /// The combined fitness function to minimize: f(x1) + f(x2) + ... + f(xN).
        /// </summary>
        /// <param name="stageDvList">An array representing [x1, x2, ..., xN-1].</param>
        /// <returns>The fitness value.</returns>
        private float CalculateFitness(float[] stageDvList, List<StageConfig> optimizingStages, List<StageConfig> mockStageList)
        {
            float currentSumOfIndependent = 0f;
            float totalFitness = 0f;

            // Calculate fitness for the first N-1 variables
            for (int i = 0; i < _dimensions; i++)
            {
                float x_i = stageDvList[i];
                if (x_i <= _epsilon) // Penalty for non-positive independent variable
                {
                    return float.MaxValue;
                }
                optimizingStages[i].targetDv = x_i;

                currentSumOfIndependent += x_i;
            }

            //for (int i = 0; i < _dimensions; i++)
            //{
            //    float x_i = stageDvList[i];
            //    if (x_i <= _epsilon) // Penalty for non-positive independent variable
            //    {
            //        return float.MaxValue;
            //    }
            //    totalFitness += Fx(x_i);
            //    currentSumOfIndependent += x_i;
            //}

            // Calculate the N-th (dependent) variable
            float xN = _totalDv - currentSumOfIndependent;
            optimizingStages.Last().targetDv = xN;
            // Apply penalty if the derived N-th variable is not positive
            if (xN <= _epsilon)
            {
                return float.MaxValue;
            }

            totalFitness = SEO_StageWindow.GetTotalWeight(mockStageList);

            return totalFitness;
        }

        /// <summary>
        /// Runs the Particle Swarm Optimization algorithm.
        /// </summary>
        public void Optimize(List<StageConfig> optimizingStages, List<StageConfig> mockStageList)
        {
            List<Particle> particles = new List<Particle>();

            // Variables for convergence check
            float previousGlobalBestFitness = float.MaxValue;
            int consecutiveConvergedCount = 0;

            // 1. Initialize particles
            for (int i = 0; i < _numParticles; i++)
            {
                Particle particle = new Particle(_dimensions);

                // Initialize position and velocity randomly within bounds
                for (int d = 0; d < _dimensions; d++)
                {
                    // Random position within [_minBound, _maxBound]
                    particle.Position[d] = _minBound + (UnityEngine.Random.value * (_maxBound - _minBound));
                    // Random velocity (e.g., between -(_maxBound - _minBound) and +(_maxBound - _minBound))
                    particle.Velocity[d] = (UnityEngine.Random.value * 2f - 1f) * (_maxBound - _minBound);
                }

                // Ensure initial position sum of independent variables satisfies the constraint
                // This ensures the derived N-th variable will be positive.
                while (particle.Position.Sum() >= _totalDv - _epsilon)
                {
                    for (int d = 0; d < _dimensions; d++)
                    {
                        particle.Position[d] = _minBound + (UnityEngine.Random.value * (_maxBound - _minBound));
                    }
                }

                // Calculate initial fitness
                float currentFitness = CalculateFitness(particle.Position, optimizingStages, mockStageList);

                // Set initial personal best
                Array.Copy(particle.Position, particle.PersonalBestPosition, _dimensions);
                particle.PersonalBestFitness = currentFitness;

                // Update global best if this particle is better
                if (currentFitness < GlobalBestFitness)
                {
                    GlobalBestFitness = currentFitness;
                    Array.Copy(particle.Position, GlobalBestPosition, _dimensions);
                }

                particles.Add(particle);
            }

            // 2. Main PSO loop
            for (int iter = 0; iter < _maxIterations; iter++)
            {
                // Store global best from previous iteration for convergence check
                previousGlobalBestFitness = GlobalBestFitness;

                foreach (Particle particle in particles)
                {
                    // Generate random numbers for cognitive and social components
                    float r1 = UnityEngine.Random.value; // For personal best attraction
                    float r2 = UnityEngine.Random.value; // For global best attraction

                    // Update velocity
                    for (int d = 0; d < _dimensions; d++)
                    {
                        float cognitiveComponent = _cognitiveWeight * r1 * (particle.PersonalBestPosition[d] - particle.Position[d]);
                        float socialComponent = _socialWeight * r2 * (GlobalBestPosition[d] - particle.Position[d]);
                        particle.Velocity[d] = (_inertiaWeight * particle.Velocity[d]) + cognitiveComponent + socialComponent;

                        // Optional: Clamp velocity to prevent explosion
                        // float maxVel = (_maxBound - _minBound) / 2.0f; // Example max velocity
                        // if (particle.Velocity[d] > maxVel) particle.Velocity[d] = maxVel;
                        // if (particle.Velocity[d] < -maxVel) particle.Velocity[d] = -maxVel;
                    }

                    // Update position
                    for (int d = 0; d < _dimensions; d++)
                    {
                        particle.Position[d] += particle.Velocity[d];

                        // Clamp position within overall bounds
                        particle.Position[d] = Mathf.Max(_minBound, Mathf.Min(particle.Position[d], _maxBound));
                    }

                    // Re-check and adjust position if sum of independent variables is too high
                    // This is crucial for maintaining the constraint and ensuring the last variable is positive.
                    float currentSumOfIndependent = particle.Position.Sum();
                    if (currentSumOfIndependent >= _totalDv - _epsilon)
                    {
                        // If sum is too high, scale down independent variables proportionally
                        // to ensure their sum is just below _constantSum.
                        float scaleFactor = (_totalDv - _epsilon) / currentSumOfIndependent;
                        for (int d = 0; d < _dimensions; d++)
                        {
                            particle.Position[d] *= scaleFactor;
                            // Ensure positivity after scaling
                            if (particle.Position[d] < _epsilon) particle.Position[d] = _epsilon;
                        }
                    }

                    // Calculate new fitness
                    float currentFitness = CalculateFitness(particle.Position, optimizingStages, mockStageList);

                    // Update personal best
                    if (currentFitness < particle.PersonalBestFitness)
                    {
                        Array.Copy(particle.Position, particle.PersonalBestPosition, _dimensions);
                        particle.PersonalBestFitness = currentFitness;
                    }

                    // Update global best
                    if (currentFitness < GlobalBestFitness)
                    {
                        GlobalBestFitness = currentFitness;
                        Array.Copy(particle.Position, GlobalBestPosition, _dimensions);
                    }
                }

                // Check for convergence after all particles have updated
                if (Mathf.Abs(GlobalBestFitness - previousGlobalBestFitness) < _fitnessTolerance)
                {
                    consecutiveConvergedCount++;
                }
                else
                {
                    consecutiveConvergedCount = 0; // Reset if significant change occurred
                }

                if (consecutiveConvergedCount >= _consecutiveConvergedIterations)
                {
                    Debug.Log($"\nOptimization converged after {iter + 1} iterations.");
                    break; // Exit the loop
                }

                //Debug.Log($"Iteration {iter + 1}: Global Best Fitness = {GlobalBestFitness:F6}");
            }
        }
    }
}
