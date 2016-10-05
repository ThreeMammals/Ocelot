using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Moq;
using Ocelot.Library.Infrastructure.Responses;
using Ocelot.Library.Infrastructure.Services;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Services
{
    public class RequestDataServiceTests
    {
        private IRequestDataService _requestDataService;
        private IHttpContextAccessor _httpContextAccesor;
        private string _key;
        private object _toAdd;
        private Response<int[]> _result;

        public RequestDataServiceTests()
        {
            _httpContextAccesor = new HttpContextAccessor();
            _httpContextAccesor.HttpContext = new DefaultHttpContext();
            _requestDataService = new RequestDataService(_httpContextAccesor);
        }

        [Fact]
        public void should_add_item()
        {
            this.Given(x => x.GivenIHaveAnItemToAdd("blahh", new [] {1,2,3,4}))
                .When(x => x.WhenIAddTheItem())
                .Then(x => x.ThenTheItemIsAdded())
                .BDDfy();
        }

        [Fact]
        public void should_get_item()
        {
            this.Given(x => x.GivenThereIsAnItemInTheContext("chest"))
                .When(x => x.WhenIGetTheItem())
                .Then(x => x.ThenTheItemIsReturned())
                .BDDfy();
        }

        private void ThenTheItemIsReturned()
        {
            _result.IsError.ShouldBeFalse();
            _result.Data.ShouldNotBeNull();
        }

        private void WhenIGetTheItem()
        {
            _result = _requestDataService.Get<int[]>(_key);
        }

        private void GivenThereIsAnItemInTheContext(string key)
        {
            _key = key;
            var data = new[] {5435345};
            _httpContextAccesor.HttpContext.Items.Add(key, data);
        }

        private void GivenIHaveAnItemToAdd(string key, object toAdd)
        {
            _key = key;
            _toAdd = toAdd;
        }

        private void WhenIAddTheItem()
        {
            _requestDataService.Add(_key, _toAdd);
        }

        private void ThenTheItemIsAdded()
        {
            object obj;
            _httpContextAccesor.HttpContext.Items.TryGetValue(_key, out obj).ShouldBeTrue();
        }
    }
}
