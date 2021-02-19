using Amazon.Lambda.APIGatewayEvents;
using Xunit;
using Amazon.Lambda.TestUtilities;

namespace LambdaApiSolution.DockerFunction.Tests
{
    public class FunctionTest
    {
        [Fact]
        public void TestToUpperFunction()
        {
            Function function = new Function();
            TestLambdaContext context = new TestLambdaContext();
            APIGatewayProxyRequest apiGatewayProxyRequest = new APIGatewayProxyRequest()
            {
                Body = "hello world"
            };
            Casing casing = function.FunctionHandler(apiGatewayProxyRequest, context);
            Assert.Equal("hello world", casing.Lower);
            Assert.Equal("HELLO WORLD", casing.Upper);
        }
    }
}
