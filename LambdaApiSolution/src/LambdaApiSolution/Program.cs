using Amazon.CDK;

namespace LambdaApiSolution
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            App app = new App();
            PipelineStack pipelineStack = new PipelineStack(app, "LambdaApiSolutionPipelineStack");
            app.Synth();
        }
    }
}
