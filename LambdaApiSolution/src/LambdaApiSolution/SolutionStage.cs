using Amazon.CDK;
using Amazon.JSII.Runtime.Deputy;
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