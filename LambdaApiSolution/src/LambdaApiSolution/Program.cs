using Amazon.CDK;

namespace LambdaApiSolution
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            App app = new App();
            //new LambdaApiSolutionStack(app, "LambdaApiSolutionStack");
            new PipelineStack(app, "LambdaApiSolutionPipelineStack");
            app.Synth();
        }
    }
}
