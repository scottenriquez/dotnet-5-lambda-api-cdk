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