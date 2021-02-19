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
					InstallCommands = new [] { "npm install -g aws-cdk" },
					BuildCommands = new [] { "dotnet build src/LambdaApiSolution.sln" },
					SynthCommand = "cdk synth"
				})
			});
			CdkStage developmentStage = pipeline.AddApplicationStage(new SolutionStage(this, "Development"));
			developmentStage.AddManualApprovalAction(new AddManualApprovalOptions()
			{
				ActionName = "PromoteToTest"
			});
			CdkStage testStage = pipeline.AddApplicationStage(new SolutionStage(this, "Test"));
			testStage.AddManualApprovalAction(new AddManualApprovalOptions()
			{
				ActionName = "PromoteToProduction"
			});
			CdkStage productionStage = pipeline.AddApplicationStage(new SolutionStage(this, "Production"));
		}
	}
}