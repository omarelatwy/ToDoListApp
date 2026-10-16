using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToDoListApp.Data;
using ToDoListApp.Models;

namespace ToDoListApp.Controllers
{
    public class TodoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public TodoController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: Todo (Index + Sorting + Filtering + Pagination)
        public async Task<IActionResult> Index(DateTime? searchDeadline, int page = 1)
        {
            int pageSize = 5;
            var query = _context.TodoItems.AsQueryable();

            if (searchDeadline.HasValue)
            {
                query = query.Where(t => t.Deadline.Date == searchDeadline.Value.Date);
            }

            // Order by Deadline Ascending (nearest deadline first)
            query = query.OrderBy(t => t.Deadline);

            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            page = Math.Max(1, Math.Min(page, totalPages > 0 ? totalPages : 1));

            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.SearchDeadline = searchDeadline?.ToString("yyyy-MM-dd");

            return View(items);
        }

        // GET: Todo/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Todo/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TodoItemCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                string uniqueFileName = string.Empty;
                if (model.File != null)
                {
                    string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }
                    uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.File.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.File.CopyToAsync(fileStream);
                    }
                }

                var todoItem = new TodoItem
                {
                    Title = model.Title,
                    Description = model.Description,
                    Deadline = model.Deadline,
                    FilePath = "/uploads/" + uniqueFileName
                };

                _context.TodoItems.Add(todoItem);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // GET: Todo/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var item = await _context.TodoItems.FindAsync(id);
            if (item == null) return NotFound();

            var viewModel = new TodoItemEditViewModel
            {
                Id = item.Id,
                Title = item.Title,
                Description = item.Description,
                Deadline = item.Deadline,
                ExistingFilePath = item.FilePath
            };

            return View(viewModel);
        }

        // POST: Todo/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TodoItemEditViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var item = await _context.TodoItems.FindAsync(id);
                if (item == null) return NotFound();

                item.Title = model.Title;
                item.Description = model.Description;
                item.Deadline = model.Deadline;

                if (model.NewFile != null)
                {
                    if (!string.IsNullOrEmpty(item.FilePath))
                    {
                        string oldPath = Path.Combine(_environment.WebRootPath, item.FilePath.TrimStart('/'));
                        if (System.IO.File.Exists(oldPath))
                        {
                            System.IO.File.Delete(oldPath);
                        }
                    }

                    string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.NewFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.NewFile.CopyToAsync(fileStream);
                    }
                    item.FilePath = "/uploads/" + uniqueFileName;
                }

                _context.Update(item);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // GET: Todo/Download?filePath=...
        public IActionResult Download(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return NotFound();

            string fullPath = Path.Combine(_environment.WebRootPath, filePath.TrimStart('/'));
            if (!System.IO.File.Exists(fullPath)) return NotFound();

            byte[] fileBytes = System.IO.File.ReadAllBytes(fullPath);
            string fileName = Path.GetFileName(fullPath);
            return File(fileBytes, "application/octet-stream", fileName);
        }

        // POST: Todo/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.TodoItems.FindAsync(id);
            if (item == null) return Json(new { success = false, message = "Item not found" });

            if (!string.IsNullOrEmpty(item.FilePath))
            {
                string fullPath = Path.Combine(_environment.WebRootPath, item.FilePath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }

            _context.TodoItems.Remove(item);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}
