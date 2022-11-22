using Grpc.Net.Client;
using MassTransit;
using Sample.grpc.client;

Console.WriteLine("To test methods, press 1  to delete, 2 to get, 3 to post, 4 to patch or 5 to put");
var val = Console.ReadLine();
using var channel = GrpcChannel.ForAddress("http://localhost:5236");

var orderClient = new Order.OrderClient(channel);

var customerClient = new Customer.CustomerClient(channel);

switch (val)
{
    case "1":
        var replyDeleteCustomer = await customerClient.DeleteAsync(new CustomerRequest() { Id = NewId.NextGuid().ToString(), CustomerNumber = "12345" });
        Console.WriteLine($"Reply Delete Customer {replyDeleteCustomer}");
        break;
    case "2":
        var replayGetOrder = await orderClient.GetAsync(new OrderGetRequest() { Id = NewId.NextGuid().ToString() });
        Console.WriteLine($"Reply Get Order {replayGetOrder}");
        break;
    case "3":
        var replayPostOrder = await orderClient.PostAsync(new OrderPostRequest() { Id = NewId.NextGuid().ToString(), CustomerNumber = "12345", PaymentCardNumber = "12345", Notes = "Test" });
        Console.WriteLine($"Reply Post Order {replayPostOrder}");
        break;
    case "4":
        var replayPatchOrder = await orderClient.PatchAsync(new OrderPatchRequest() { Id = NewId.NextGuid().ToString() });
        Console.WriteLine($"Reply Patch Order {replayPatchOrder}");
        break;
    case "5":
        var replayPutOrder = await orderClient.PutAsync(new OrderPutRequest() { Id = NewId.NextGuid().ToString(), CustomerNumber = "12345" });
        Console.WriteLine($"Reply Put Order {replayPutOrder}");
        break;
    default:
        Console.WriteLine("Option invalid");
        break;
}

Console.WriteLine("press any key to exit");
Console.ReadLine();