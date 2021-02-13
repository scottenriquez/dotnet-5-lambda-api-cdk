using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Xunit;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;

using LambdaApiSolution.DockerFunction;

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
