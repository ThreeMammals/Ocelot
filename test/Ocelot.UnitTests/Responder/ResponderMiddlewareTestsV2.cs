using System.Collections.Generic;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Moq;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Errors;
using Ocelot.Logging;
using Ocelot.Responder;
using Ocelot.Responder.Middleware;
using Ocelot.Responses;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Responder
{
    public class ResponderMiddlewareTestsV2
    {
        private readonly Mock<IHttpResponder> _responder;
        private readonly Mock<IRequestScopedDataRepository> _scopedRepository;
        private readonly Mock<IErrorsToHttpStatusCodeMapper> _codeMapper;
        private readonly Mock<RequestDelegate> _next;
        private readonly Mock<IOcelotLoggerFactory> _loggerFactory;
        private readonly Mock<IOcelotLogger> _logger;
        private readonly Mock<HttpContext> _httpContext;
        private ResponderMiddleware _middleware;
        private OkResponse<HttpResponseMessage> _response;
        private int _mappedStatusCode;
        private List<Error> _pipelineErrors;

        public ResponderMiddlewareTestsV2()
        {
            _responder = new Mock<IHttpResponder>();
            _codeMapper = new Mock<IErrorsToHttpStatusCodeMapper>();
            _next = new Mock<RequestDelegate>();
            _logger = new Mock<IOcelotLogger>();
            _scopedRepository = new Mock<IRequestScopedDataRepository>();
            _loggerFactory = new Mock<IOcelotLoggerFactory>();
            _httpContext = new Mock<HttpContext>();

            _loggerFactory
                .Setup(lf => lf.CreateLogger<ResponderMiddleware>())
                .Returns(_logger.Object);

            _middleware = new ResponderMiddleware(_next.Object, _responder.Object, _loggerFactory.Object, _scopedRepository.Object, _codeMapper.Object);

            GivenTheHttpResponseMessageIs(new HttpResponseMessage());
        }

        [Fact]
        public void NoPipelineErrors()
        {
            this.Given(x => x.GivenThereAreNoPipelineErrors())
                .When(x => x.WhenICallTheMiddleware())
                .Then(_ => ThenTheNextMiddlewareIsCalled())
                .And(x => x.ThenThereAreNoErrorsOnTheHttpContext())
                .BDDfy();
        }

        [Fact]
        public void PipelineErrors()
        {
            this.Given(_ => GivenThereArePipelineErrors())
                .And(_ => GivenTheErrorsCanBeMappedToAStatusCode())
                .When(_ => WhenICallTheMiddleware())
                .Then(_ => ThenTheNextMiddlewareIsCalled())
                .And(x => x.ThenTheErrorsAreLogged())
                .And(_ => ThenTheErrorsAreMappedToAnHttpStatus())
                .And(_ => ThenAnErrorResponseIsSetOnTheHttpContext())
                .BDDfy();
        }

        private void GivenTheHttpResponseMessageIs(HttpResponseMessage response)
        {
            _response = new OkResponse<HttpResponseMessage>(response);
            _scopedRepository
                .Setup(x => x.Get<HttpResponseMessage>(It.IsAny<string>()))
                .Returns(_response);
        }

        private void GivenThereAreNoPipelineErrors()
        {
            GivenThereArePipelineErrors(new List<Error>());
        }

        private void GivenThereArePipelineErrors()
        {
            GivenThereArePipelineErrors(new List<Error>() { new AnyError() });
        }

        private void GivenThereArePipelineErrors(List<Error> pipelineErrors)
        {
            _pipelineErrors = pipelineErrors;

            _scopedRepository
                .Setup(x => x.Get<bool>("OcelotMiddlewareError"))
                .Returns(new OkResponse<bool>(_pipelineErrors.Count != 0));

            _scopedRepository
                .Setup(sr => sr.Get<List<Error>>("OcelotMiddlewareErrors"))
                .Returns(new OkResponse<List<Error>>(_pipelineErrors));
        }

        private void GivenTheErrorsCanBeMappedToAStatusCode()
        {
            _mappedStatusCode = 500; //TODO: autofixture
            _codeMapper.Setup(cm => cm.Map(It.IsAny<List<Error>>()))
            .Returns(_mappedStatusCode);
        }

        private void WhenICallTheMiddleware()
        {
            _middleware.Invoke(_httpContext.Object).GetAwaiter().GetResult();
        }

        private void ThenTheNextMiddlewareIsCalled()
        {
            _next.Verify(n => n(_httpContext.Object), Times.Once);
        }

        private void ThenTheErrorsAreMappedToAnHttpStatus()
        {
            _codeMapper.Verify(cm => cm.Map(_pipelineErrors), Times.Once);
        }

        private void ThenTheErrorsAreLogged()
        {
            _logger.Verify(l => l.LogError($"{_pipelineErrors.Count} pipeline errors found in ResponderMiddleware. Setting error response status code"), Times.Once);
        }

        private void ThenThereAreNoErrorsOnTheHttpContext()
        {
            _responder.Verify(r => r.SetErrorResponseOnContext(It.IsAny<HttpContext>(), It.IsAny<int>()), Times.Never);
        }

        private void ThenAnErrorResponseIsSetOnTheHttpContext()
        {
            _responder.Verify(r => r.SetErrorResponseOnContext(_httpContext.Object, _mappedStatusCode), Times.Once);
        }
    }
}
