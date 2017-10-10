﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using FluentAssertions;
using Microsoft.OpenApiSpecification.Core.Models;
using Microsoft.OpenApiSpecification.Generation.Models;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.OpenApiSpecification.Generation.Tests.InternalOpenApiDocumentGeneratorTests
{
    [Collection("DefaultSettings")]
    public class InternalOpenApiDocumentGeneratorTest
    {
        private const string TestFilesDirectory = "InternalOpenApiDocumentGeneratorTests/TestFiles";
        private const string TestValidationDirectory = "InternalOpenApiDocumentGeneratorTests/TestValidation";

        private readonly ITestOutputHelper _output;

        public InternalOpenApiDocumentGeneratorTest(ITestOutputHelper output)
        {
            _output = output;
        }

        private static IEnumerable<object[]> GetTestCasesForInvalidDocumentationShouldFailGeneration()
        {
            // Invalid Verb
            yield return new object[]
            {
                "Invalid Verb",
                Path.Combine(TestFilesDirectory, "AnnotationInvalidVerb.xml"),
                new List<string> {Path.Combine(TestFilesDirectory, "OpenApiSpecification.UnitTestSamples.DotNetFrameworkController.dll")},
                9,
                Path.Combine(
                    TestValidationDirectory,
                    "AnnotationInvalidVerb.Json"),
                new List<PathGenerationResult>
                {
                    new PathGenerationResult(
                        "Invalid",
                        "/V1/samples/{id}",
                        string.Format(SpecificationGenerationMessages.InvalidHttpMethod, "Invalid"),
                        GenerationStatus.Failure)
                }
            };

            // Invalid Uri
            yield return new object[]
            {
                "Invalid Uri",
                Path.Combine(TestFilesDirectory, "AnnotationInvalidUri.xml"),
                new List<string> {Path.Combine(TestFilesDirectory, "OpenApiSpecification.UnitTestSamples.DotNetFrameworkController.dll")},
                9,
                Path.Combine(
                    TestValidationDirectory,
                    "AnnotationInvalidUri.Json"),
                new List<PathGenerationResult>
                {
                    new PathGenerationResult(
                        SpecificationGenerationMessages.OperationMethodNotParsedGivenUrlIsInvalid,
                        "http://{host}:9000/V1/samples/{id}?queryBool={queryBool}",
                        string.Format(SpecificationGenerationMessages.InvalidUrl, "http://{host}:9000/V1/samples/{id}?queryBool={queryBool}"),
                        GenerationStatus.Failure)
                }
            };

            // Parameters that have no in attributes and not present in the URL.
            yield return new object[]
            {
                "Parameters Without In Attribute And Not Present In URL",
                Path.Combine(TestFilesDirectory, "AnnotationParamWithoutInNotPresentInUrl.xml"),
                new List<string> {Path.Combine(TestFilesDirectory, "OpenApiSpecification.UnitTestSamples.DotNetFrameworkController.dll")},
                9,
                Path.Combine(
                    TestValidationDirectory,
                    "AnnotationParamWithoutInNotPresentInUrl.Json"),
                new List<PathGenerationResult>
                {
                    new PathGenerationResult(
                        OperationMethod.Get.ToString(),
                        "/V1/samples/{id}",
                        string.Format(
                            SpecificationGenerationMessages.MissingInAttribute,
                            string.Join(", ", new List<string> { "sampleHeaderParam2", "sampleHeaderParam3" } )),
                        GenerationStatus.Failure)
                }
            };

            // Conflicting Path and Query Parameters
            yield return new object[]
            {
                "Conflicting Path and Query Parameters",
                Path.Combine(TestFilesDirectory, "AnnotationConflictingPathAndQueryParameters.xml"),
                new List<string> {Path.Combine(TestFilesDirectory, "OpenApiSpecification.UnitTestSamples.DotNetFrameworkController.dll")},
                9,
                Path.Combine(
                    TestValidationDirectory,
                    "AnnotationConflictingPathAndQueryParameters.Json"),
                new List<PathGenerationResult>
                {
                    new PathGenerationResult(
                        OperationMethod.Get.ToString(),
                        "/V1/samples/{id}",
                        string.Format(
                            SpecificationGenerationMessages.ConflictingPathAndQueryParameters,
                            "id",
                            "http://localhost:9000/V1/samples/{id}?queryBool={queryBool}&id={id}" ),
                        GenerationStatus.Failure)
                }
            };

            // Path parameter in the URL is not documented in any param elements.
            yield return new object[]
            {
                "Path Parameter Undocumented",
                Path.Combine(TestFilesDirectory, "AnnotationUndocumentedPathParam.xml"),
                new List<string> {Path.Combine(TestFilesDirectory, "OpenApiSpecification.UnitTestSamples.DotNetFrameworkController.dll")},
                9,
                Path.Combine(
                    TestValidationDirectory,
                    "AnnotationUndocumentedPathParam.Json"),
                new List<PathGenerationResult>
                {
                    new PathGenerationResult(
                        OperationMethod.Get.ToString(),
                        "/V1/samples/{id}",
                        string.Format(
                            SpecificationGenerationMessages.UndocumentedPathParameter,
                            "id",
                            "http://localhost:9000/V1/samples/{id}?queryBool={queryBool}" ),
                        GenerationStatus.Failure)
                }
            };

            // Undocumented Generics
            yield return new object[]
            {
                "Undocumented Generics",
                Path.Combine(TestFilesDirectory, "AnnotationUndocumentedGeneric.xml"),
                new List<string> {Path.Combine(TestFilesDirectory, "OpenApiSpecification.UnitTestSamples.DotNetFrameworkController.dll")},
                9,
                Path.Combine(
                    TestValidationDirectory,
                    "AnnotationUndocumentedGeneric.Json"),
                new List<PathGenerationResult>
                {
                    new PathGenerationResult(
                        OperationMethod.Get.ToString(),
                        "/V3/samples/{id}",
                        SpecificationGenerationMessages.UndocumentedGenericType,
                        GenerationStatus.Failure),
                }
            };

            // Incorrect Order for Generics
            yield return new object[]
            {
                "Incorrect Order for Generics",
                Path.Combine(TestFilesDirectory, "AnnotationIncorrectlyOrderedGeneric.xml"),
                new List<string> {Path.Combine(TestFilesDirectory, "OpenApiSpecification.UnitTestSamples.DotNetFrameworkController.dll")},
                9,
                Path.Combine(
                    TestValidationDirectory,
                    "AnnotationIncorrectlyOrderedGeneric.Json"),
                new List<PathGenerationResult>
                {
                    new PathGenerationResult(
                        OperationMethod.Get.ToString(),
                        "/V3/samples/",
                        SpecificationGenerationMessages.UnorderedGenericType,
                        GenerationStatus.Failure)
                }
            };
        }

        private static IEnumerable<object[]> GetTestCasesForValidDocumentationShouldPassGeneration()
        {
            // Standard, original valid XML document
            yield return new object[]
            {
                "Standard valid XML document",
                Path.Combine(TestFilesDirectory, "Annotation.xml"),
                new List<string> {Path.Combine(TestFilesDirectory, "OpenApiSpecification.UnitTestSamples.DotNetFrameworkController.dll")},
                9,
                Path.Combine(
                    TestValidationDirectory,
                    "Annotation.Json")
            };

            // Valid XML document but with parameters that have no in attributes but are present in the URL.
            yield return new object[]
            {
                "Parameters Without In Attribute But Present In URL",
                Path.Combine(TestFilesDirectory, "AnnotationParamWithoutInButPresentInUrl.xml"),
                new List<string> {Path.Combine(TestFilesDirectory, "OpenApiSpecification.UnitTestSamples.DotNetFrameworkController.dll")},
                9,
                Path.Combine(
                    TestValidationDirectory,
                    "AnnotationParamWithoutInButPresentInUrl.Json")
            };

            // Valid XML document but with one parameter without specified type.
            // The type should simply default to string.
            yield return new object[]
            {
                "Unspecified Type Default to String",
                Path.Combine(TestFilesDirectory, "AnnotationParamNoTypeSpecified.xml"),
                new List<string> {Path.Combine(TestFilesDirectory, "OpenApiSpecification.UnitTestSamples.DotNetFrameworkController.dll")},
                9,
                Path.Combine(
                    TestValidationDirectory,
                    "AnnotationParamNoTypeSpecified.Json")
            };

            // Valid XML document with multiple response types per response code.
            yield return new object[]
            {
                "Multiple Response Types Per Response Code",
                Path.Combine(TestFilesDirectory, "AnnotationMultipleResponseTypes.xml"),
                new List<string> {Path.Combine(TestFilesDirectory, "OpenApiSpecification.UnitTestSamples.DotNetFrameworkController.dll")},
                9,
                Path.Combine(
                    TestValidationDirectory,
                    "AnnotationMultipleResponseTypes.Json")
            };

            // Valid XML document with multiple request types.
            yield return new object[]
            {
                "Multiple Request Types",
                Path.Combine(TestFilesDirectory, "AnnotationMultipleRequestTypes.xml"),
                new List<string> {Path.Combine(TestFilesDirectory, "OpenApiSpecification.UnitTestSamples.DotNetFrameworkController.dll")},
                9,
                Path.Combine(
                    TestValidationDirectory,
                    "AnnotationMultipleRequestTypes.Json")
            };

            // Valid XML document with multiple request content types.
            yield return new object[]
            {
                "Multiple Request Media Types",
                Path.Combine(TestFilesDirectory, "AnnotationMultipleRequestMediaTypes.xml"),
                new List<string> {Path.Combine(TestFilesDirectory, "OpenApiSpecification.UnitTestSamples.DotNetFrameworkController.dll")},
                9,
                Path.Combine(
                    TestValidationDirectory,
                    "AnnotationMultipleRequestMediaTypes.Json")
            };

            // Valid XML document with multiple response content types.
            yield return new object[]
            {
                "Multiple Response Media Types Per Response Code",
                Path.Combine(TestFilesDirectory, "AnnotationMultipleResponseMediaTypes.xml"),
                new List<string> {Path.Combine(TestFilesDirectory, "OpenApiSpecification.UnitTestSamples.DotNetFrameworkController.dll")},
                9,
                Path.Combine(
                    TestValidationDirectory,
                    "AnnotationMultipleResponseMediaTypes.Json")
            };
        }

        [Theory]
        [MemberData(nameof(GetTestCasesForInvalidDocumentationShouldFailGeneration))]
        public void InvalidDocumentationShouldFailGeneration(
            string testCaseName,
            string inputXmlFile,
            IList<string> inputBinaryFiles,
            int expectedPathGenerationResultsCount,
            string expectedJsonFile,
            IList<PathGenerationResult> expectedFailedPathGenerationResults)
        {
            _output.WriteLine(testCaseName);

            var document = XDocument.Load(inputXmlFile);

            var generator = new OpenApiDocumentGenerator();

            var result = generator.GenerateV3Documents(
                document,
                inputBinaryFiles);

            result.Should().NotBeNull();

            _output.WriteLine(JsonConvert.SerializeObject(result));

            result.GenerationStatus.Should().Be(GenerationStatus.Failure);
            result.MainDocument.Should().NotBeNull();
            result.PathGenerationResults.Count.Should().Be(expectedPathGenerationResultsCount);

            var failedPaths = result.PathGenerationResults.Where(
                    p => p.GenerationStatus == GenerationStatus.Failure)
                .ToList();

            var actualDocument = JsonConvert.SerializeObject(result.MainDocument);

            var expectedDocument = File.ReadAllText(expectedJsonFile);

            _output.WriteLine(actualDocument);

            failedPaths.Should().BeEquivalentTo(expectedFailedPathGenerationResults);

            // We are doing serialization and deserialization to force the resulting actual document
            // to have the exact fields we will see in the resulting document based on the contract resolver.
            // Without serialization and deserialization, the actual document may have fields that should
            // not be present, such as empty list fields.
            JsonConvert.DeserializeObject<OpenApiV3SpecificationDocument>(actualDocument)
                .Should()
                .BeEquivalentTo(JsonConvert.DeserializeObject<OpenApiV3SpecificationDocument>(expectedDocument));
        }

        [Fact]
        public void NoOperationsToParseShouldReturnEmptyDocument()
        {
            var path = Path.Combine(TestFilesDirectory, "AnnotationNoOperationsToParse.xml");

            var document = XDocument.Load(path);

            var generator = new OpenApiDocumentGenerator();

            var result = generator.GenerateV3Documents(document, new List<string>());

            result.Should().NotBeNull();
            result.GenerationStatus.Should().Be(GenerationStatus.Success);
            result.MainDocument.Should().BeNull();
            result.PathGenerationResults.Should()
                .BeEquivalentTo(
                    new List<PathGenerationResult>
                    {
                        new PathGenerationResult(
                            SpecificationGenerationMessages.NoOperationElementFoundToParse,
                            GenerationStatus.Success)
                    });
        }

        [Theory]
        [MemberData(nameof(GetTestCasesForValidDocumentationShouldPassGeneration))]
        public void ValidDocumentationShouldPassGeneration(
            string testCaseName,
            string inputXmlFile,
            IList<string> inputBinaryFiles,
            int expectedPathGenerationResultsCount,
            string expectedJsonFile)
        {
            _output.WriteLine(testCaseName);

            var document = XDocument.Load(inputXmlFile);

            var generator = new OpenApiDocumentGenerator();

            var result = generator.GenerateV3Documents(
                document,
                inputBinaryFiles);
            
            result.Should().NotBeNull();

            _output.WriteLine(JsonConvert.SerializeObject(result));

            result.GenerationStatus.Should().Be(GenerationStatus.Success);
            result.MainDocument.Should().NotBeNull();
            result.PathGenerationResults.Count.Should().Be(expectedPathGenerationResultsCount);

            var actualDocument = JsonConvert.SerializeObject(result.MainDocument);

            var expectedDocument = File.ReadAllText(expectedJsonFile);

            _output.WriteLine(actualDocument);

            JsonConvert.DeserializeObject<OpenApiV3SpecificationDocument>(actualDocument)
                .Should()
                .BeEquivalentTo(
                    JsonConvert.DeserializeObject<OpenApiV3SpecificationDocument>(expectedDocument),
                    o => o.WithStrictOrdering());
        }
    }
}