using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NewLifeThriftShop.Data;
using NewLifeThriftShop.Models;

namespace NewLifeThriftShop.Controllers
{
    public class OrdersController : Controller
    {
        private readonly NewLifeThriftShop_NewContext _context;
        private readonly PaymentsController _paymentsController;
        private readonly OrderItemsController _orderItemController;
        private readonly CartItemsController _cartItemController;


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
            ClaimsPrincipal currentUser = this.User;
            var currentUserID = currentUser.FindFirst(ClaimTypes.NameIdentifier).Value;
            return View(await _context.Order.Where(i => i.CustomerId == currentUserID)
                .Include(i => i.OrderItems)
                .ThenInclude(i => i.Product)
                .ToListAsync());
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }
            ClaimsPrincipal currentUser = this.User;
            var currentUserID = currentUser.FindFirst(ClaimTypes.NameIdentifier).Value;
            var order = await _context.Order.Where(i => i.CustomerId == currentUserID)
                .Include(i => i.OrderItems)
                .ThenInclude(i => i.Product)
                .FirstAsync();

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
                string orderId = System.Guid.NewGuid().ToString();
                order.OrderId = orderId;
                order.CustomerId = currentUserID;
                order.Price = totalPrice;
                order.Status = "SUBMITTED";
                order.CreatedAt = DateTime.Now;
                _context.Add(order);
                await _context.SaveChangesAsync();

                Payment payment = new Payment();
                payment.CustomerId = currentUserID;
                payment.PaymentMethod = paymentType;
                payment.Price = totalPrice;
                payment.OrderId = orderId;

                await _paymentsController.Create(payment);

                var cartItemList = await _context.CartItem.Where(i => i.UserId == currentUserID).Include(i => i.Product).ToListAsync();
                foreach (var cartItem in cartItemList)
                {
                    var orderItem = new OrderItem();
                    orderItem.ProductId = cartItem.ProductId;
                    orderItem.Quantity = cartItem.Quantity;
                    orderItem.Price = cartItem.Product.Price * cartItem.Quantity;
                    orderItem.Status = "PENDING";
                    orderItem.OrderId = orderId;

                    await _orderItemController.Create(orderItem);

                    await _cartItemController.DeleteConfirmed(cartItem.CartItemId);

                }


                return RedirectToAction(nameof(Index));
            }
            return View(order);
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
    }
}
