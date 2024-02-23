﻿using Microsoft.Extensions.DependencyInjection;
using Ocelot.Errors;
using Ocelot.Requester;
using Ocelot.UnitTests.Responder;

namespace Ocelot.UnitTests.Requester
{
    public class HttpExceptionToErrorMapperTests
    {
        private HttpExceptionToErrorMapper _mapper;
        private readonly ServiceCollection _services;

        public HttpExceptionToErrorMapperTests()
        {
            _services = new ServiceCollection();
            var provider = _services.BuildServiceProvider();
            _mapper = new HttpExceptionToErrorMapper(provider);
        }

        [Fact]
        public void Should_return_default_error_because_mappers_are_null()
        {
            var error = _mapper.Map(new Exception());

            error.ShouldBeOfType<UnableToCompleteRequestError>();
        }

        [Fact]
        public void Should_return_request_canceled()
        {
            var error = _mapper.Map(new OperationCanceledException());

            error.ShouldBeOfType<RequestCanceledError>();
        }

        [Fact]
        public void Should_return_ConnectionToDownstreamServiceError()
        {
            var error = _mapper.Map(new HttpRequestException());

            error.ShouldBeOfType<ConnectionToDownstreamServiceError>();
        }

        [Fact]
        public void Should_return_request_canceled_for_subtype()
        {
            var error = _mapper.Map(new SomeException());

            error.ShouldBeOfType<RequestCanceledError>();
        }

        [Fact]
        public void Should_return_error_from_mapper()
        {
            IDictionary<Type, Func<Exception, Error>> errorMapping = new Dictionary<Type, Func<Exception, Error>>
            {
                {typeof(TaskCanceledException), e => new AnyError()},
            };

            _services.AddSingleton(errorMapping);

            var provider = _services.BuildServiceProvider();

            _mapper = new HttpExceptionToErrorMapper(provider);

            var error = _mapper.Map(new TaskCanceledException());

            error.ShouldBeOfType<AnyError>();
        }

        private class SomeException : OperationCanceledException
        { }
    }
}
