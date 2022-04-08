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
    public class CartItemsController : Controller
    {
        private readonly NewLifeThriftShop_NewContext _context;

        public CartItemsController(NewLifeThriftShop_NewContext context)
        {
            _context = context;
        }

        // GET: CartItems
        public async Task<IActionResult> Index(string Message = "")
        {
            ClaimsPrincipal currentUser = this.User;
            var currentUserID = currentUser.FindFirst(ClaimTypes.NameIdentifier).Value;
            var cartItemList = await _context.CartItem.Where(i => i.UserId == currentUserID).Include(i => i.Product).ToListAsync();
            double totalPrice = 0;
            foreach (var item in cartItemList)
            {
                totalPrice += item.Product.Price * Convert.ToDouble(item.Quantity);
            }
            ViewBag.TotalPrice = totalPrice;
            ViewBag.IsEmpty = totalPrice == 0;
            ViewBag.Message = Message;
            return View(cartItemList);
        }

        // GET: CartItems/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cartItem = await _context.CartItem
                .Include(i => i.Product)
                .FirstOrDefaultAsync(m => m.CartItemId == id);
            if (cartItem == null)
            {
                return NotFound();
            }

            return View(cartItem);
        }

        // GET: CartItems/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: CartItems/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CartItemId,ProductId,Quantity,UserId")] CartItem cartItem, string productId)
        {
            if (ModelState.IsValid) 
            {
                ClaimsPrincipal currentUser = this.User;
                var currentUserID = currentUser.FindFirst(ClaimTypes.NameIdentifier).Value;
                var product = _context.Product.FindAsync(int.Parse(productId)).Result;
                cartItem.Product = product;
                cartItem.UserId = currentUserID;
                var existRecord = await _context.CartItem
                .Include(i => i.Product)
                .FirstOrDefaultAsync(m => m.ProductId == int.Parse(productId) && m.UserId == currentUserID);
                if (existRecord != null)
                {
                    return RedirectToAction("Edit", new
                    {
                        id = existRecord.CartItemId       
                    }); ;
                }
                
                cartItem.Quantity = 1;
                _context.Add(cartItem);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(cartItem);
        }

        // GET: CartItems/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var cartItem = await _context.CartItem
                .Include(i => i.Product)
                .FirstOrDefaultAsync(m => m.CartItemId == id);
            if (cartItem == null)
            {
                return NotFound();
            }
            return View(cartItem);
        }

        // POST: CartItems/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CartItemId,ProductId,Quantity,UserId")] CartItem cartItem)
        {
            if (id != cartItem.CartItemId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var product = _context.Product.FindAsync(cartItem.ProductId).Result;
                    cartItem.Product = product;
                    _context.Update(cartItem);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CartItemExists(cartItem.CartItemId))
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
            return View(cartItem);
        }

        // GET: CartItems/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cartItem = await _context.CartItem
                .FirstOrDefaultAsync(m => m.CartItemId == id);
            if (cartItem == null)
            {
                return NotFound();
            }

            return View(cartItem);
        }

        // POST: CartItems/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await DeleteCartItem(id);
            return RedirectToAction(nameof(Index));
        }

        public async Task DeleteCartItem(int id)
        {
            var cartItem = await _context.CartItem.FindAsync(id);
            _context.CartItem.Remove(cartItem);
            await _context.SaveChangesAsync();
        }

        private bool CartItemExists(int id)
        {
            return _context.CartItem.Any(e => e.CartItemId == id);
        }
    }
}
