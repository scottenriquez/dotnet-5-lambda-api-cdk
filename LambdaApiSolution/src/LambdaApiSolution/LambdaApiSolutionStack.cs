using Amazon.CDK;
using Amazon.CDK.AWS.Lambda;

namespace LambdaApiSolution
{
    public class LambdaApiSolutionStack : Stack
    {
        internal LambdaApiSolutionStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            DockerImageCode dockerImageCode = DockerImageCode.FromImageAsset("src/LambdaApiSolution.DockerFunction/src/LambdaApiSolution.DockerFunction");
            DockerImageFunction dockerImageFunction = new DockerImageFunction(this, "LambdaFunction", new DockerImageFunctionProps()
            {
                Code = dockerImageCode,
                Description = ".NET 5 Docker Lambda function"
            });
        }
    }
}
