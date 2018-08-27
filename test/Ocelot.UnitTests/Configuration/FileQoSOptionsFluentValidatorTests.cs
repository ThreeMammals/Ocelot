namespace Ocelot.UnitTests.Configuration
{
    using System;
    using FluentValidation.Results;
    using Microsoft.Extensions.DependencyInjection;
    using Ocelot.Configuration.File;
    using Ocelot.Configuration.Validator;
    using Ocelot.Requester;
    using Requester;
    using Shouldly;
    using TestStack.BDDfy;
    using Xunit;

    public class FileQoSOptionsFluentValidatorTests
    {
        private FileQoSOptionsFluentValidator _validator;
        private IServiceProvider _serviceProvider;
        private readonly IServiceCollection _serviceCollection;
        private ValidationResult _result;
        private FileQoSOptions _qosOptions;

        public FileQoSOptionsFluentValidatorTests()
        {
            _serviceCollection = new ServiceCollection();
        }

        [Fact]
        public void should_be_invalid_if_no_qos_delegate()
        {
            var qosOptions = new FileQoSOptions
            {
                DurationOfBreak = 1000,
                ExceptionsAllowedBeforeBreaking = 100,
                TimeoutValue = 2000
            };

            this.Given(_ => GivenTheFollowing(qosOptions))
                .When(_ => WhenIValidate())
                .Then(_ => ValidationFailed())
                .BDDfy();
        }

        [Fact]
        public void should_be_valid_if_qos_delegate_registered()
        {
            var qosOptions = new FileQoSOptions
            {
                DurationOfBreak = 1000,
                ExceptionsAllowedBeforeBreaking = 100,
                TimeoutValue = 2000
            };

            this.Given(_ => GivenTheFollowing(qosOptions))
                .And(_ => GivenAQosDelegate())
                .When(_ => WhenIValidate())
                .Then(_ => ValidationSuccess())
                .BDDfy();
        }

        private void ValidationSuccess()
        {
            _result.IsValid.ShouldBeTrue();
        }

        private void GivenAQosDelegate()
        {
            QosDelegatingHandlerDelegate del = (a, b) => new FakeDelegatingHandler();
            _serviceCollection.AddSingleton<QosDelegatingHandlerDelegate>(del);
        }

        private void ValidationFailed()
        {
            _result.IsValid.ShouldBeFalse();
        }

        private void GivenTheFollowing(FileQoSOptions qosOptions)
        {
            _qosOptions = qosOptions;
        }

        private void WhenIValidate()
        {
            _serviceProvider = _serviceCollection.BuildServiceProvider();
            _validator = new FileQoSOptionsFluentValidator(_serviceProvider);
            _result = _validator.Validate(_qosOptions);
        }
    }
}
