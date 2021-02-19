using Amazon.CDK;

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
