using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MassTransit;
using Sample.Contracts;
using System;

namespace Sample.grpc.Services
{
    public class OrderService : Order.OrderBase
    {
        private readonly ILogger<OrderService> _logger;
        private readonly IRequestClient<SubmitOrder> _submitOrderRequestClient;
        private readonly ISendEndpointProvider _sendEndpointProvider;
        private readonly IRequestClient<CheckOrder> _checkOrderClient;
        private readonly IPublishEndpoint _publishEndpoint;
        public OrderService(ILogger<OrderService> logger, IPublishEndpoint publishEndpoint, IRequestClient<SubmitOrder> submitOrderRequestClient,
            ISendEndpointProvider sendEndpointProvider, IRequestClient<CheckOrder> checkOrderClient)
        {
            _logger = logger;
            _submitOrderRequestClient = submitOrderRequestClient;
            _sendEndpointProvider = sendEndpointProvider;
            _checkOrderClient = checkOrderClient;
            _publishEndpoint = publishEndpoint;
        }

        public override async Task<OrderGetResponse> Get(OrderGetRequest request, ServerCallContext context)
        {
            var (status, notFound) = await _checkOrderClient.GetResponse<OrderStatus, OrderNotFound>(new { OrderId = request.Id });

            if (status.IsCompletedSuccessfully)
            {
                var response = await status;

                return await Task.FromResult(new OrderGetResponse
                {
                    OrderId = response.Message.OrderId.ToString(),
                    State = response.Message.State,
                    DefaultStatusCode = StatusCodes.Status200OK
                });
            }
            else
            {
                var response = await notFound;
                return await Task.FromResult(new OrderGetResponse
                {
                    OrderId = response.Message.OrderId.ToString(),
                    DefaultStatusCode = StatusCodes.Status404NotFound
                });
            }
        }

        public override async Task<OrderPostResponse> Post(OrderPostRequest request, ServerCallContext context)
        {
            var (accepted, rejected) = await _submitOrderRequestClient.GetResponse<OrderSubmissionAccepted, OrderSubmissionRejected>(new
            {
                OrderId = request.Id,
                InVar.Timestamp,
                request.CustomerNumber,
                request.PaymentCardNumber,
                request.Notes
            });

            if (accepted.IsCompletedSuccessfully)
            {
                var response = await accepted;

                return await Task.FromResult(new OrderPostResponse
                {
                    DefaultStatusCode = StatusCodes.Status202Accepted,
                    OrderId = response.Message.OrderId.ToString(),
                    Timestamp = Timestamp.FromDateTimeOffset(response.Message.Timestamp),
                    CustomerNumber = response.Message.CustomerNumber
                });
            }

            if (accepted.IsCompleted)
            {
                await accepted;

                return await Task.FromResult(new OrderPostResponse
                {
                    DefaultStatusCode = 500,
                    Detail = "Order was not accepted"
                });
            }
            else
            {
                var response = await rejected;
                return await Task.FromResult(new OrderPostResponse
                {
                    DefaultStatusCode = StatusCodes.Status400BadRequest,
                    OrderId = response.Message.OrderId.ToString(),
                    Timestamp = Timestamp.FromDateTimeOffset(response.Message.Timestamp),
                    CustomerNumber = response.Message.CustomerNumber,
                    Reason = response.Message.Reason
                });
            }
        }

        public override async Task<OrderPatchResponse> Patch(OrderPatchRequest request, ServerCallContext context)
        {
            await _publishEndpoint.Publish<OrderAccepted>(new
            {
                OrderId = request.Id,
                InVar.Timestamp,
            });
            return await Task.FromResult(new OrderPatchResponse
            {
                DefaultStatusCode = StatusCodes.Status202Accepted
            });
        }

        public override async Task<OrderPutResponse> Put(OrderPutRequest request, ServerCallContext context)
        {
            var endpoint = await _sendEndpointProvider.GetSendEndpoint(new Uri("queue:submit-order"));

            await endpoint.Send<SubmitOrder>(new
            {
                OrderId = request.Id,
                InVar.Timestamp,
                request.CustomerNumber
            });

            return await Task.FromResult(new OrderPutResponse
            {
                DefaultStatusCode = StatusCodes.Status202Accepted
            });
        }
    }
}
