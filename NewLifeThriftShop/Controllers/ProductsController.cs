using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewLifeThriftShop.Data;
using NewLifeThriftShop.Models;

namespace NewLifeThriftShop.Controllers
{
    public class ProductsController : Controller
    {
        private readonly NewLifeThriftShop_NewContext _context;
        private readonly UploadFileController _uploadFileController;

        public ProductsController(NewLifeThriftShop_NewContext context)
        {
            _context = context;
            _uploadFileController = new UploadFileController();
        }

        // GET: Products
        public async Task<IActionResult> Index(string message = "")
        {
            ViewBag.Message = message;
            ClaimsPrincipal currentUser = this.User;
            var currentUserID = currentUser.FindFirst(ClaimTypes.NameIdentifier).Value;
            return View(await _context.Product.ToListAsync());
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Product
                .FirstOrDefaultAsync(m => m.ProductId == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // GET: Products/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Products/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductId,ProductName,Category,Price,Quantity")] Product product, List<IFormFile> images)
        {
            if (ModelState.IsValid)
            {
                if (images.Any())
                {
                    if (images[0].Length > 1048576)
                    {
                        RedirectToAction("Index", new { Message = "The file must less than  1 MB." });
                    }
                    product.ImgExt = System.IO.Path.GetExtension(images[0].FileName);
                }
                ClaimsPrincipal currentUser = this.User;
                var currentUserID = currentUser.FindFirst(ClaimTypes.NameIdentifier).Value;
                _context.Add(product);
                await _context.SaveChangesAsync();
                if (images.Any())
                {
                    await _uploadFileController.Upload(images, product.ProductId.ToString());
                }
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Product.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // POST: Products/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProductId,ProductName,Category,Price,Quantity,ImgExt")] Product product, List<IFormFile> images)
        {

            if (id != product.ProductId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (images.Any())
                    {
                        if (!String.IsNullOrEmpty(product.ImgExt))
                        {
                            await _uploadFileController.DeleteImage(id.ToString() + product.ImgExt);
                        }
                        product.ImgExt = System.IO.Path.GetExtension(images[0].FileName);
                    }

                    _context.Update(product);
                    await _context.SaveChangesAsync();

                    if (images.Any())
                    {
                        await _uploadFileController.Upload(images, id.ToString());
                    }
                    
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.ProductId))
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
            return View(product);
        }

        // GET: Products/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Product
                .FirstOrDefaultAsync(m => m.ProductId == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Product.FindAsync(id);
            if (!String.IsNullOrEmpty(product.ImgExt))
            {
                await _uploadFileController.DeleteImage(id.ToString() + product.ImgExt);
            }
            _context.Product.Remove(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Product.Any(e => e.ProductId == id);
        }

        public async Task<IActionResult> ViewCatalog(string SearchString)
        {
            var productQuery = _context.Product.Where(p => p.Quantity > 0);
            if (!string.IsNullOrEmpty(SearchString))
            {
                productQuery = productQuery.Where(s => s.ProductName.Contains(SearchString));
            }
            return View(await productQuery.ToListAsync());
        }
    }
}
