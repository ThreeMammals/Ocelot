﻿namespace Ocelot.UnitTests.Requester
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Ocelot.Errors;
    using Ocelot.Requester;
    using Responder;
    using Shouldly;
    using Xunit;

    public class HttpExeptionToErrorMapperTests
    {
        private HttpExeptionToErrorMapper _mapper;
        private readonly ServiceCollection _services;

        public HttpExeptionToErrorMapperTests()
        {
            _services = new ServiceCollection();
            var provider = _services.BuildServiceProvider();
            _mapper = new HttpExeptionToErrorMapper(provider);
        }

        [Fact]
        public void should_return_default_error_because_mappers_are_null()
        {
            var error = _mapper.Map(new Exception());

            error.ShouldBeOfType<UnableToCompleteRequestError>();
        }

        [Fact]
        public void should_return_error_from_mapper()
        {
            var errorMapping = new Dictionary<Type, Func<Exception, Error>>
            {
                {typeof(TaskCanceledException), e => new AnyError()},
            };

            _services.AddSingleton(errorMapping);

            var provider = _services.BuildServiceProvider();

            _mapper = new HttpExeptionToErrorMapper(provider);

            var error = _mapper.Map(new TaskCanceledException());

            error.ShouldBeOfType<AnyError>();
        }
    }
}
