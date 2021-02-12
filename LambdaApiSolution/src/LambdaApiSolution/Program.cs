using Amazon.CDK;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LambdaApiSolution
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();
            new LambdaApiSolutionStack(app, "LambdaApiSolutionStack");
            app.Synth();
        }
    }
}
