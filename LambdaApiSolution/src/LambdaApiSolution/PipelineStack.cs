using Amazon.CDK;
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
					SourceArtifact = sourceArtifact,
					CloudAssemblyArtifact = cloudAssemblyArtifact,
					InstallCommands = new [] { "npm install -g aws-cdk" },
					BuildCommands = new [] { "dotnet build LambdaApiSolution/src/LambdaApiSolution.sln" },
					SynthCommand = "cdk synth"
				})
			});
		}
	}
}