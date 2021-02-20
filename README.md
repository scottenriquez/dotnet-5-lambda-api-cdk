## Deciding on Which Technology to Use
While infrastructure as code (IaC) has existed within the AWS ecosystem since 2011, adoption has exploded recently due to the ability to manage large amounts of infrastructure at scale and standardize design across an organization. There are almost too many options between CloudFormation (CFN), CDK, and Terraform for IaC and Serverless Application Model (SAM) and Serverless Framework for development. [This article](https://acloudguru.com/blog/engineering/cloudformation-terraform-or-cdk-guide-to-iac-on-aws) from A Cloud Guru quickly sums up the pros and cons of each option. I choose this particular stack for some key reasons:
- CDK allows the infrastructure and the CI/CD pipeline to be described as C# instead of YAML, JSON, or [HCL](https://www.terraform.io/docs/language/syntax/configuration.html)
- CDK provides the ability to inject more robust logic than intrinsic functions in CloudFormation and more modularity as well while still being a native AWS offering
- Docker ensures that the Lambda functions run consistently across local development, builds, and production environments and simplifies dependency management
- [CDK Pipelines](https://docs.aws.amazon.com/cdk/api/latest/docs/@aws-cdk_pipelines.CdkPipeline.html) offer a higher level construct with much less configuration than CodePipeline and streamline management of multiple environments

## GitHub Repository
You can find a complete working example [here](https://github.com/scottenriquez/dotnet-5-lambda-api-cdk).

## Initializing the Project
Ensure that .NET 5 and the latest version of CDK are installed. To create a solution skeleton, run these commands in the root directory:

```shell
# note that CDK uses this directory name as the solution name
mkdir LambdaApiSolution
cd LambdaApiSolution
cdk init app --language=csharp
# creates a CFN stack called CDKToolkit with an S3 bucket for staging purposes and configures IAM permissions for CI/CD
cdk bootstrap --cloudformation-execution-policies arn:aws:iam::aws:policy/AdministratorAccess
cdk deploy
```

In order to use CDK Pipelines later on, a specific flag needs to be added to `cdk.json`:
```json
{
  "app": "dotnet run -p src/LambdaApiSolution/LambdaApiSolution.csproj",
  "context": {
    ..
    "@aws-cdk/core:newStyleStackSynthesis": "true",
    ..
  }
}
```

At the time of writing, the generated CDK template uses .NET Core 3.1. Inside of the `.csproj` file, change the `TargetFramework` tag to `net5.0`.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net5.0</TargetFramework>
  </PropertyGroup>
</Project>
```

From the `/LambdaApiSolution` directory, run these commands to create the serverless skeleton:

```shell
# install the latest version of the .NET Lambda templates
dotnet new -i Amazon.Lambda.Templates
cd src/
# create the function
dotnet new lambda.image.EmptyFunction --name LambdaApiSolution.DockerFunction
# add the projects to the solution file
dotnet sln add LambdaApiSolution.DockerFunction/src/LambdaApiSolution.DockerFunction/LambdaApiSolution.DockerFunction.csproj
dotnet sln add LambdaApiSolution.DockerFunction/test/LambdaApiSolution.DockerFunction.Tests/LambdaApiSolution.DockerFunction.Tests.csproj
# build the solution and run the sample unit test to verify that everything is wired up correctly
dotnet test LambdaApiSolution.sln
```

## Creating the Lambda Infrastructure and Build
First, add the Lambda CDK NuGet package to the CDK project.

```xml
<PackageReference Include="Amazon.CDK.AWS.Lambda" Version="1.90.0" />
```

Then, create the Docker image and Lambda function using CDK constructs in `LambdaApiSolutionStack.cs`:

```csharp
public class LambdaApiSolutionStack : Stack
{
    internal LambdaApiSolutionStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
    {
        // this path is relative to the directory where CDK commands are run
        // the directory must contain a Dockerfile
        DockerImageCode dockerImageCode = DockerImageCode.FromImageAsset("src/LambdaApiSolution.DockerFunction/src/LambdaApiSolution.DockerFunction");
        DockerImageFunction dockerImageFunction = new DockerImageFunction(this, "LambdaFunction", new DockerImageFunctionProps()
        {
            Code = dockerImageCode,
            Description = ".NET 5 Docker Lambda function"
        });
    }
}
```

Lastly, update the `Dockerfile` in the Lambda function project like so to build the C# code:

```dockerfile
FROM public.ecr.aws/lambda/dotnet:5.0
FROM mcr.microsoft.com/dotnet/sdk:5.0 as build-image

ARG FUNCTION_DIR="/build"
ARG CONFIGURATION="release"
ENV PATH="/root/.dotnet/tools:${PATH}"

RUN apt-get update && apt-get -y install zip

RUN mkdir $FUNCTION_DIR
WORKDIR $FUNCTION_DIR
COPY Function.cs LambdaApiSolution.DockerFunction.csproj aws-lambda-tools-defaults.json $FUNCTION_DIR/
RUN dotnet tool install -g Amazon.Lambda.Tools

RUN mkdir -p build_artifacts
RUN if [ "$CONFIGURATION" = "debug" ]; then dotnet lambda package --configuration Debug --package-type zip; else dotnet lambda package --configuration Release --package-type zip; fi
RUN if [ "$CONFIGURATION" = "debug" ]; then cp -r /build/bin/Debug/net5.0/publish/* /build/build_artifacts; else cp -r /build/bin/Release/net5.0/publish/* /build/build_artifacts; fi

FROM public.ecr.aws/lambda/dotnet:5.0

COPY --from=build-image /build/build_artifacts/ /var/task/
CMD ["LambdaApiSolution.DockerFunction::LambdaApiSolution.DockerFunction.Function::FunctionHandler"]
```

At this point, you can now deploy the changes with the `cdk deploy` command. The Lambda function can be tested via the AWS Console. The easiest way to do so is to navigate to the CloudFormation stack, click on the function resource, and then create a test event with the string `"hello"` as the input. Note that this should not be a JSON object because the event handler's parameter currently accepts a single string.

## Integrating API Gateway
Add the following packages to the CDK project:

```xml
<PackageReference Include="Amazon.CDK.AWS.APIGatewayv2" Version="1.90.0" />
<PackageReference Include="Amazon.CDK.AWS.APIGatewayv2.Integrations" Version="1.90.0" />
```

Next, you can add the API Gateway resources to the stack immediately after the `DockerImageFunction` in `LambdaApiSolutionStack.cs`:

```csharp
HttpApi httpApi = new HttpApi(this, "APIGatewayForLambda", new HttpApiProps()
{
    ApiName = "APIGatewayForLambda",
    CreateDefaultStage = true,
    CorsPreflight = new CorsPreflightOptions()
    {
        AllowMethods = new [] { HttpMethod.GET },
        AllowOrigins = new [] { "*" },
        MaxAge = Duration.Days(10)
    }
});
```
Then, create a Lambda proxy integration and a route for the function:

```csharp
LambdaProxyIntegration lambdaProxyIntegration = new LambdaProxyIntegration(new LambdaProxyIntegrationProps()
{
    Handler = dockerImageFunction,
    PayloadFormatVersion = PayloadFormatVersion.VERSION_2_0
});
httpApi.AddRoutes(new AddRoutesOptions()
{
    Path = "/casing",
    Integration = lambdaProxyIntegration,
    Methods = new [] { HttpMethod.POST }
});
```

I used `/casing` for the path since the sample Lambda function returns an upper and lower case version of the input string. Finally, it's helpful to display the endpoint URL using a CFN output for testing.

```csharp
// adding entropy to prevent a name collision
string guid = Guid.NewGuid().ToString();
CfnOutput apiUrl = new CfnOutput(this, "APIGatewayURLOutput", new CfnOutputProps()
{
    ExportName = $"APIGatewayEndpointURL-{guid}",
    Value = httpApi.ApiEndpoint
});
```

With these changes to the resources, the Lambda function can be invoked by a `POST` request. The handler method parameters in `Function.cs` need to be updated for the request body to be passed in.

```csharp
// replace the string parameter with a proxy request parameter
public Casing FunctionHandler(APIGatewayProxyRequest apiGatewayProxyRequest, ILambdaContext context)
{
    // update the input to use the proxy
    string input = apiGatewayProxyRequest.Body;
    return new Casing(input.ToLower(), input.ToUpper());
}
```

After successfully deploying the changes, the function can be tested in two ways. The first way is through an HTTP client like Postman. Add a string to the body parameter of the `POST` request. This action tests the full integration with API Gateway as well as the Lambda function. To test via the Lambda Console, update the test event from before to match the `APIGatewayProxyRequest` parameter:

```json
{
  "body": "hello"
}
```

## Adding CI/CD Using CDK Pipelines
For this example, the source code resides in GitHub as opposed to CodeCommit. To grant the CI/CD pipeline access to the repository, a personal access token with `repo` permissions must be created via GitHub and stored in Secrets Manager as a plaintext format object. Note that for this codebase, I've named my secret `GitHub-Token`.

Next, add the following packages to the CDK project:
```xml
<PackageReference Include="Amazon.CDK.AWS.CodeBuild" Version="1.90.0" />
<PackageReference Include="Amazon.CDK.AWS.CodeDeploy" Version="1.90.0" />
<PackageReference Include="Amazon.CDK.AWS.CodePipeline" Version="1.90.0" />
<PackageReference Include="Amazon.CDK.AWS.CodePipeline.Actions" Version="1.90.0" />
<PackageReference Include="Amazon.CDK.Pipelines" Version="1.90.0" />
```

With these dependencies loaded, create a class called `PipelineStack.cs`. The following code creates a self-mutating CDK Pipeline, adds a GitHub source action to fetch the code using the token from Secrets Manager, and synthesizes the solution's CDK:

```csharp
using Amazon.CDK;
using Amazon.CDK.AWS.CodeBuild;
using Amazon.CDK.AWS.CodePipeline;
using Amazon.CDK.AWS.CodePipeline.Actions;
using Amazon.CDK.Pipelines;

namespace LambdaApiSolution
{
    public class PipelineStack : Stack
    {
        internal PipelineStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            Artifact_ sourceArtifact = new Artifact_();
            Artifact_ cloudAssemblyArtifact = new Artifact_();
            CdkPipeline pipeline = new CdkPipeline(this, "LambdaApiSolutionPipeline", new CdkPipelineProps()
            {
                CloudAssemblyArtifact = cloudAssemblyArtifact,
                PipelineName = "LambdaApiSolutionPipeline",
                SourceAction = new GitHubSourceAction(new GitHubSourceActionProps()
                {
                    ActionName = "GitHubSource",
                    Output = sourceArtifact,
                    OauthToken = SecretValue.SecretsManager(Constants.GitHubTokenSecretsManagerId),
                    Owner = Constants.Owner,
                    Repo = Constants.RepositoryName,
                    Branch = Constants.Branch,
                    Trigger = GitHubTrigger.POLL
                }),
                SynthAction = new SimpleSynthAction(new SimpleSynthActionProps()
                {
                    Environment = new BuildEnvironment
                    {
                        // required for .NET 5
                        // https://docs.aws.amazon.com/codebuild/latest/userguide/build-env-ref-available.html
                        BuildImage = LinuxBuildImage.STANDARD_5_0
                    },
                    SourceArtifact = sourceArtifact,
                    CloudAssemblyArtifact = cloudAssemblyArtifact,
                    Subdirectory = "LambdaApiSolution",
                    InstallCommands = new[] {"npm install -g aws-cdk"},
                    BuildCommands = new[] {"dotnet build src/LambdaApiSolution.sln"},
                    SynthCommand = "cdk synth"
                })
            });
        }
    }
}

```

Remove the following line from `Program.cs` since the pipeline will deploy the API from now on:
```csharp
new LambdaApiSolutionStack(app, "LambdaApiSolutionStack");
```

Delete the previous stack, commit the latest changes to the source code so that they'll be available when the pipeline fetches the repo, and finally deploy the pipeline:

```shell
cdk destroy
git add .
git commit -m "Adding source code to GitHub repository"
git push origin main
cdk deploy LambdaApiSolutionPipelineStack
```

## Creating Multiple Environments
From now on, the pipeline will manage changes instead of manual `cdk deploy` commands. By merely pushing changes to the `main` branch, the pipeline will update itself and the other resources. The last feature in this example is adding development, test, and production environments. Rather than creating more stacks, we can leverage stages instead. Each environment will have a stage that makes a separate stack plus actions like approvals or integration testing. First, a stage must be defined in code. For this example, a stage will only contain an API stack.

```csharp
using Amazon.CDK;
using Construct = Constructs.Construct;

namespace LambdaApiSolution
{
   public class SolutionStage : Stage
   {
      public SolutionStage(Construct scope, string id, IStageProps props = null) : base(scope, id, props)
      {
         LambdaApiSolutionStack lambdaApiSolutionStack = new LambdaApiSolutionStack(this, "Solution");
      }
   }
}
```

To implement the stages, navigate back to `PipelineStack.cs` and append the following code after the pipeline declaration:

```csharp
CdkStage developmentStage = pipeline.AddApplicationStage(new SolutionStage(this, "Development"));
CdkStage testStage = pipeline.AddApplicationStage(new SolutionStage(this, "Test"));
testStage.AddManualApprovalAction(new AddManualApprovalOptions()
{
    ActionName = "PromoteToProduction"
});
CdkStage productionStage = pipeline.AddApplicationStage(new SolutionStage(this, "Production"));
```

## Next Steps
The Lambda function, API Gateway, and multi-environment CI/CD pipeline are now in place. More Lambda functions can be added as separate C# projects. More stacks can be created and added to `SolutionStage.cs`.