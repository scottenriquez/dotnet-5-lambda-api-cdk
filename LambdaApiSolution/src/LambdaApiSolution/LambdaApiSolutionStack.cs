using System;
using Amazon.CDK;
using Amazon.CDK.AWS.APIGatewayv2;
using Amazon.CDK.AWS.APIGatewayv2.Integrations;
using Amazon.CDK.AWS.Lambda;

namespace LambdaApiSolution
{
	public class LambdaApiSolutionStack : Stack
	{
		internal LambdaApiSolutionStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
		{
			DockerImageCode dockerImageCode =
				DockerImageCode.FromImageAsset("src/LambdaApiSolution.DockerFunction/src/LambdaApiSolution.DockerFunction");
			DockerImageFunction dockerImageFunction = new DockerImageFunction(this, "LambdaFunction",
				new DockerImageFunctionProps()
				{
					Code = dockerImageCode,
					Description = ".NET 5 Docker Lambda function"
				});
			HttpApi httpApi = new HttpApi(this, "APIGatewayForLambda", new HttpApiProps()
			{
				ApiName = "APIGatewayForLambda",
				CreateDefaultStage = true,
				CorsPreflight = new CorsPreflightOptions()
				{
					AllowMethods = new[] {HttpMethod.GET},
					AllowOrigins = new[] {"*"},
					MaxAge = Duration.Days(10)
				}
			});
			LambdaProxyIntegration lambdaProxyIntegration = new LambdaProxyIntegration(new LambdaProxyIntegrationProps()
			{
				Handler = dockerImageFunction,
				PayloadFormatVersion = PayloadFormatVersion.VERSION_2_0
			});
			httpApi.AddRoutes(new AddRoutesOptions()
			{
				Path = "/casing",
				Integration = lambdaProxyIntegration,
				Methods = new[] {HttpMethod.POST}
			});
			string guid = Guid.NewGuid().ToString();
			CfnOutput apiUrl = new CfnOutput(this, "APIGatewayURLOutput", new CfnOutputProps()
			{
				ExportName = $"APIGatewayEndpointURL-{guid}",
				Value = httpApi.ApiEndpoint
			});
		}
	}
}