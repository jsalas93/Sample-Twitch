using Grpc.Core;
using MassTransit;
using Sample.Contracts;

namespace Sample.grpc.Services
{
    public class CustomerService : Customer.CustomerBase
    {

        private readonly ILogger<CustomerService> _logger;
        private readonly IPublishEndpoint _publishEndpoint;
        public CustomerService(ILogger<CustomerService> logger, IPublishEndpoint publishEndpoint)
        {
            _logger = logger;
            _publishEndpoint = publishEndpoint;
        }

        public override async Task<CustomerResponse> Delete(CustomerRequest request, ServerCallContext context)
        {
            await _publishEndpoint.Publish<CustomerAccountClosed>(new
            {
                CustomerId = request.Id,
                request.CustomerNumber
            });
            return await Task.FromResult(new CustomerResponse
            {
                DefaultStatusCode = StatusCodes.Status200OK
            });
        }
    }
}
