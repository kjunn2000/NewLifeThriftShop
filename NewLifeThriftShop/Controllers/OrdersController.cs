using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NewLifeThriftShop.Areas.Identity.Data;
using NewLifeThriftShop.Data;
using NewLifeThriftShop.Models;
using Newtonsoft.Json;

namespace NewLifeThriftShop.Controllers
{
    public class OrdersController : Controller
    {
        private readonly NewLifeThriftShop_NewContext _context;
        private readonly PaymentsController _paymentsController;
        private readonly OrderItemsController _orderItemController;
        private readonly CartItemsController _cartItemController;

        private const string queueName = "NewLifeSQSOrderQueue";


        public OrdersController(NewLifeThriftShop_NewContext context)
        {
            _context = context;
            _paymentsController = new PaymentsController(context);
            _orderItemController = new OrderItemsController(context);
            _cartItemController = new CartItemsController(context);
        }

        // GET: Orders
        public async Task<IActionResult> Index()
        {
            if (this.User.IsInRole("Admin"))
            {
                return View(await _context.Order
                    .Where(o => o.Status != "PENDING" && o.Status!= "CANCELLED")
                    .Include(i => i.OrderItems)
                    .ThenInclude(i => i.Product)
                    .ToListAsync());
            }
            ClaimsPrincipal currentUser = this.User;
            var currentUserID = currentUser.FindFirst(ClaimTypes.NameIdentifier).Value;
            return View(await _context.Order.Where(i => i.CustomerId == currentUserID)
                .Include(i => i.OrderItems)
                .ThenInclude(i => i.Product)
                .ToListAsync());
        }

        private List<string> getAWSCredentialInfo()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            IConfigurationRoot configure = builder.Build();
            List<string> credentialInfo = new List<string>();
            credentialInfo.Add(configure["AWSCredential:AccessKey"]);
            credentialInfo.Add(configure["AWSCredential:SecretKey"]);
            credentialInfo.Add(configure["AWSCredential:SessionToken"]);

            return credentialInfo;
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Order
                .Include(i => i.OrderItems)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(i => i.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // GET: Orders/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Orders/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrder([Bind("OrderId,CustomerId,Price,Status,CreatedAt")] Order order, double totalPrice, string paymentType)
        {
            if (ModelState.IsValid)
            {
                ClaimsPrincipal currentUser = this.User;
                var currentUserID = currentUser.FindFirst(ClaimTypes.NameIdentifier).Value;

                var cartItemList = await _context.CartItem
                    .Where(i => i.UserId == currentUserID)
                    .Include(i => i.Product).ToListAsync();

                var product = IsCartItemHasStock(cartItemList);
                if (!string.IsNullOrEmpty(product))
                {
                    return RedirectToAction("Index", "CartItems", new
                    {
                        Message = "The stock of product - " +
                        product + " is not enough."
                    });
                }

                string orderId = System.Guid.NewGuid().ToString();
                order.OrderId = orderId;
                order.CustomerId = currentUserID;
                order.Price = totalPrice;
                order.Status = "PENDING";
                order.CreatedAt = DateTime.Now;
                _context.Add(order);
                await _context.SaveChangesAsync();

                Payment payment = new Payment();
                payment.CustomerId = currentUserID;
                payment.PaymentMethod = paymentType;
                payment.Price = totalPrice;
                payment.OrderId = orderId;
                payment.CreatedAt = DateTime.Now;

                await _paymentsController.Create(payment);

                foreach (var cartItem in cartItemList)
                {
                    var orderItem = new OrderItem();
                    orderItem.ProductId = cartItem.ProductId;
                    orderItem.Quantity = cartItem.Quantity;
                    orderItem.Price = cartItem.Product.Price * cartItem.Quantity;
                    orderItem.OrderId = orderId;

                    cartItem.Product.Quantity -= cartItem.Quantity;
                    _context.Update(cartItem.Product);
                    await _context.SaveChangesAsync();

                    await _orderItemController.Create(orderItem);

                    await _cartItemController.DeleteCartItem(cartItem.CartItemId);

                }

                await SubmitOrderToSns(order);

                return RedirectToAction(nameof(Index));
            }
            return View(order);
        }

        public string IsCartItemHasStock (List<CartItem> cartItems)
        {
            foreach (var item in cartItems)
            {
                if (item.Quantity>item.Product.Quantity)
                {
                    return item.Product.ProductName;
                }
            }
            return "";
        }

        // GET: Orders/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Order.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            return View(order);
        }

        // POST: Orders/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("OrderId,CustomerId,Price,Status,CreatedAt")] Order order)
        {
            if (id != order.OrderId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(order);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(order.OrderId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(order);
        }

        // GET: Orders/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Order
                .FirstOrDefaultAsync(m => m.OrderId == id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var order = await _context.Order.FindAsync(id);
            _context.Order.Remove(order);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OrderExists(string id)
        {
            return _context.Order.Any(e => e.OrderId == id);
        }

        public async Task SubmitOrderToSns(Order order)
        {
            var contentQueryStr = "CustomerId=" +
                order.CustomerId.ToString() +
                "&OrderId=" + order.OrderId +
                "&CreatedAt=" + order.CreatedAt.ToString() +
                "&Price=" + order.Price.ToString();

            var queryString = new Dictionary<string, string>()
            {
                { "message", contentQueryStr },
                {   "topic", "arn:aws:sns:us-east-1:570877075017:NewLifeOrder"}
            };

            var client = new HttpClient();
            client.BaseAddress = new Uri("https://bl0838c7h8.execute-api.us-east-1.amazonaws.com/test/");

            var requestUri = QueryHelpers.AddQueryString("handleorder", queryString);

            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
  
            await client.SendAsync(request);
        }

        public async Task<IActionResult> ViewPendingOrders(string message = "")
        {
            ViewBag.msg = message;
            List<string> credentialInfo = getAWSCredentialInfo();
            var sqsClient = new AmazonSQSClient(credentialInfo[0], credentialInfo[1], credentialInfo[2], Amazon.RegionEndpoint.USEast1);
            var response = await sqsClient.GetQueueUrlAsync(new GetQueueUrlRequest { QueueName = queueName });
            List<KeyValuePair<OrderDto, string>> oderListQueue = new List<KeyValuePair<OrderDto, string>>();
            try
            {
                ReceiveMessageRequest receiveMessageRequest = new ReceiveMessageRequest();
                receiveMessageRequest.QueueUrl = response.QueueUrl;
                receiveMessageRequest.MaxNumberOfMessages = 10;
                receiveMessageRequest.WaitTimeSeconds = 20;
                receiveMessageRequest.VisibilityTimeout = 20;

                ReceiveMessageResponse receiveMessageResponse = await sqsClient.ReceiveMessageAsync(receiveMessageRequest);

                if (receiveMessageResponse.Messages.Count != 0)
                {
                    for (int i = 0; i < receiveMessageResponse.Messages.Count; i++)
                    {
                        System.Diagnostics.Debug.WriteLine(i);
                        QueueResponse queueResponse = JsonConvert.DeserializeObject<QueueResponse>(receiveMessageResponse.Messages[i].Body);

                        var dict = HttpUtility.ParseQueryString(queueResponse.Message);
                        System.Diagnostics.Debug.WriteLine(dict);
                        string json = JsonConvert.SerializeObject(dict.Cast<string>().ToDictionary(k => k, v => dict[v]));
                        System.Diagnostics.Debug.WriteLine(json);

                        OrderDto order = JsonConvert.DeserializeObject<OrderDto>(json);
                        System.Diagnostics.Debug.Write(order);

                        var receiptHandle = receiveMessageResponse.Messages[i].ReceiptHandle;
                        oderListQueue.Add(new KeyValuePair<OrderDto, string>(order, receiptHandle));
                    }
                }
                return View(oderListQueue);
            }
            catch (Exception e)
            {
                return View(oderListQueue);
            }
        }

        public async Task<IActionResult> AcceptOrder(string receiptHandler, string orderId)
        {
            try
            {
                var order = _context.Order.FindAsync(orderId).Result;
                order.Status = "PREPARING";
                _context.Update(order);
                await _context.SaveChangesAsync();
                await DeleteMessage(receiptHandler);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> RejectOrder(string receiptHandler, string orderId)
        {
            try
            {
                var order = _context.Order.FindAsync(orderId).Result;
                order.Status = "CANCELLED";
                _context.Update(order);
                await _context.SaveChangesAsync();
                await DeleteMessage(receiptHandler);
                return RedirectToAction("ViewPendingOrders");
            }
            catch (Exception ex)
            {
                return RedirectToAction("ViewPendingOrders");
            }
        }

        public async Task DeleteMessage(string receiptHandler)
        {
            try
            {
                List<string> credentialInfo = getAWSCredentialInfo();
                var sqsClient = new AmazonSQSClient(credentialInfo[0], credentialInfo[1], credentialInfo[2], Amazon.RegionEndpoint.USEast1);
                var response = await sqsClient.GetQueueUrlAsync(new GetQueueUrlRequest { QueueName = queueName });

                var delRequest = new DeleteMessageRequest
                {
                    QueueUrl = response.QueueUrl,
                    ReceiptHandle = receiptHandler
                };
                var delResponse = await sqsClient.DeleteMessageAsync(delRequest);
            }
            catch (AmazonSQSException ex)
            {
                System.Diagnostics.Debug.Print(ex.ToString());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print(ex.ToString());
            }
        }
    }
}
