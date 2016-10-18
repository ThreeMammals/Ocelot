using System.Collections.Generic;
using System.Linq;
using Ocelot.Library.Errors;
using Ocelot.Library.RequestBuilder;
using Ocelot.Library.Responses;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.RequestBuilder
{
    public class ConfigurationHeadersExtractorTests
    {
        private Dictionary<string, string> _dictionary;
        private readonly IConfigurationHeaderExtrator _configurationHeaderExtrator;
        private Response<ConfigurationHeaderExtractorProperties> _result;

        public ConfigurationHeadersExtractorTests()
        {
            _configurationHeaderExtrator = new ConfigurationHeaderExtrator();
        }

        [Fact]
        public void returns_no_instructions_error()
        {
            this.Given(x => x.GivenTheDictionaryIs(new Dictionary<string, string>()
            {
                {"CustomerId", ""},
            }))
                .When(x => x.WhenICallTheExtractor())
                .Then(
                    x =>
                        x.ThenAnErrorIsReturned(new ErrorResponse<ConfigurationHeaderExtractorProperties>(
                            new List<Error>
                            {
                                new NoInstructionsError(">")
                            })))
                .BDDfy();
        }

        [Fact]
        public void returns_no_instructions_not_for_claims_error()
        {
            this.Given(x => x.GivenTheDictionaryIs(new Dictionary<string, string>()
            {
                {"CustomerId", "Cheese[CustomerId] > value"},
            }))
                .When(x => x.WhenICallTheExtractor())
                .Then(
                    x =>
                        x.ThenAnErrorIsReturned(new ErrorResponse<ConfigurationHeaderExtractorProperties>(
                            new List<Error>
                            {
                                new InstructionNotForClaimsError()
                            })))
                .BDDfy();
        }

        [Fact]
        public void can_parse_entry_to_work_out_properties_with_key()
        {
            this.Given(x => x.GivenTheDictionaryIs(new Dictionary<string, string>()
            {
                {"CustomerId", "Claims[CustomerId] > value"},
            }))
                .When(x => x.WhenICallTheExtractor())
                .Then(
                    x =>
                        x.ThenTheClaimParserPropertiesAreReturned(
                            new OkResponse<ConfigurationHeaderExtractorProperties>(
                                new ConfigurationHeaderExtractorProperties("CustomerId", "CustomerId", "", 0))))
                .BDDfy();
        }

        [Fact]
        public void can_parse_entry_to_work_out_properties_with_key_delimiter_and_index()
        {
            this.Given(x => x.GivenTheDictionaryIs(new Dictionary<string, string>()
            {
                {"UserId", "Claims[Subject] > value[0] > |"},
            }))
                .When(x => x.WhenICallTheExtractor())
                .Then(
                    x =>
                        x.ThenTheClaimParserPropertiesAreReturned(
                            new OkResponse<ConfigurationHeaderExtractorProperties>(
                                new ConfigurationHeaderExtractorProperties("UserId", "Subject", "|", 0))))
                .BDDfy();
        }

        private void ThenAnErrorIsReturned(Response<ConfigurationHeaderExtractorProperties> expected)
        {
            _result.IsError.ShouldBe(expected.IsError);
            _result.Errors[0].ShouldBeOfType(expected.Errors[0].GetType());
        }

        private void ThenTheClaimParserPropertiesAreReturned(Response<ConfigurationHeaderExtractorProperties> expected)
        {
            _result.Data.ClaimKey.ShouldBe(expected.Data.ClaimKey);
            _result.Data.Delimiter.ShouldBe(expected.Data.Delimiter);
            _result.Data.Index.ShouldBe(expected.Data.Index);
            _result.IsError.ShouldBe(expected.IsError);
        }

        private void WhenICallTheExtractor()
        {
            var first = _dictionary.First();
            _result = _configurationHeaderExtrator.Extract(first.Key, first.Value);
        }

        private void GivenTheDictionaryIs(Dictionary<string, string> dictionary)
        {
            _dictionary = dictionary;
        }
    }
}
