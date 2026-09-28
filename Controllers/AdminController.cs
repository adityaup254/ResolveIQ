using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ResolveIQ.Data;
using ResolveIQ.Models;
using ResolveIQ.Services;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ResolveIQ.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ─── DASHBOARD ───────────────────────────────────────────────────────────
        public async Task<IActionResult> Dashboard()
        {
            // Ticket counts by status
            ViewBag.TotalTickets      = await _context.Tickets.CountAsync();
            ViewBag.OpenTickets       = await _context.Tickets.CountAsync(t => t.Status == "Open");
            ViewBag.AssignedTickets   = await _context.Tickets.CountAsync(t => t.Status == "Assigned");
            ViewBag.InProgressTickets = await _context.Tickets.CountAsync(t => t.Status == "In Progress");
            ViewBag.ResolvedTickets   = await _context.Tickets.CountAsync(t => t.Status == "Resolved");
            ViewBag.ClosedTickets     = await _context.Tickets.CountAsync(t => t.Status == "Closed");
            ViewBag.HighPriority      = await _context.Tickets.CountAsync(t => t.Priority == "High");

            // User / Technician counts
            ViewBag.TotalUsers        = await _context.Users.CountAsync(u => u.Role != "Admin");
            ViewBag.TotalTechnicians  = await _context.Technicians.CountAsync();

            // Recent tickets (latest 8)
            var recentTickets = await _context.Tickets
                .Include(t => t.User)
                .Include(t => t.Category)
                .OrderByDescending(t => t.CreatedDate)
                .Take(8)
                .ToListAsync();

            ViewBag.RecentTickets = recentTickets;
            return View();
        }

        // ─── TICKETS ─────────────────────────────────────────────────────────────
        public async Task<IActionResult> Tickets(string searchString, string statusFilter, string priorityFilter, int? categoryFilter)
        {
            var query = _context.Tickets
                .Include(t => t.User)
                .Include(t => t.Category)
                .Include(t => t.Technician)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
                query = query.Where(t => t.Title.Contains(searchString) || t.Id.ToString() == searchString);
            if (!string.IsNullOrEmpty(statusFilter))
                query = query.Where(t => t.Status == statusFilter);
            if (!string.IsNullOrEmpty(priorityFilter))
                query = query.Where(t => t.Priority == priorityFilter);
            if (categoryFilter.HasValue)
                query = query.Where(t => t.CategoryId == categoryFilter.Value);

            ViewBag.Categories    = new SelectList(_context.Categories, "Id", "Name");
            ViewData["CurrentSearch"]   = searchString;
            ViewData["CurrentStatus"]   = statusFilter;
            ViewData["CurrentPriority"] = priorityFilter;
            ViewData["CurrentCategory"] = categoryFilter;

            return View(await query.OrderByDescending(t => t.CreatedDate).ToListAsync());
        }

        public async Task<IActionResult> Details(int id)
        {
            var ticket = await _context.Tickets
                .Include(t => t.User).Include(t => t.Category).Include(t => t.Technician)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (ticket == null) return NotFound();
            return View(ticket);
        }

        [HttpGet]
        public async Task<IActionResult> EditTicket(int id)
        {
            var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id);
            if (ticket == null) return NotFound();
            ViewBag.Categories  = new SelectList(_context.Categories, "Id", "Name", ticket.CategoryId);
            ViewBag.Technicians = new SelectList(_context.Technicians.Where(t => t.IsActive), "Id", "FullName", ticket.TechnicianId);
            return View(ticket);
        }

        [HttpPost]
        public async Task<IActionResult> EditTicket(int id, Ticket model)
        {
            var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id);
            if (ticket == null) return NotFound();

            if (ModelState.IsValid)
            {
                ticket.Title        = model.Title;
                ticket.Description  = model.Description;
                ticket.CategoryId   = model.CategoryId;
                ticket.Priority     = model.Priority;
                ticket.Status       = model.Status;
                if (model.TechnicianId != null && ticket.TechnicianId == null && model.Status == "Open")
                    ticket.Status = "Assigned";
                ticket.TechnicianId = model.TechnicianId;
                ticket.UpdatedDate  = DateTime.Now;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Ticket updated successfully.";
                return RedirectToAction(nameof(Tickets));
            }

            ViewBag.Categories  = new SelectList(_context.Categories, "Id", "Name", model.CategoryId);
            ViewBag.Technicians = new SelectList(_context.Technicians.Where(t => t.IsActive), "Id", "FullName", model.TechnicianId);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> DeleteTicket(int id)
        {
            var ticket = await _context.Tickets.Include(t => t.User).FirstOrDefaultAsync(t => t.Id == id);
            if (ticket == null) return NotFound();
            return View(ticket);
        }

        [HttpPost, ActionName("DeleteTicket")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null) return NotFound();
            _context.Tickets.Remove(ticket);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Ticket deleted successfully.";
            return RedirectToAction(nameof(Tickets));
        }

        // ─── USER MANAGEMENT ─────────────────────────────────────────────────────
        public async Task<IActionResult> Users(string searchString)
        {
            var query = _context.Users.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
                query = query.Where(u => u.FullName.Contains(searchString) || u.Email.Contains(searchString));
            ViewData["CurrentSearch"] = searchString;
            return View(await query.OrderBy(u => u.FullName).ToListAsync());
        }

        public async Task<IActionResult> UserDetails(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();
            ViewBag.TicketCount = await _context.Tickets.CountAsync(t => t.UserId == id);
            return View(user);
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> EditUser(int id, User model)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            if (ModelState.IsValid)
            {
                user.FullName = model.FullName;
                user.Email    = model.Email;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "User updated successfully.";
                return RedirectToAction(nameof(Users));
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(int id)
        {
            var currentAdminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (id == currentAdminId)
            {
                TempData["ErrorMessage"] = "You cannot deactivate your own account.";
                return RedirectToAction(nameof(Users));
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"User '{user.FullName}' has been {(user.IsActive ? "activated" : "deactivated")}.";
            return RedirectToAction(nameof(Users));
        }

        // ─── TECHNICIAN MANAGEMENT ───────────────────────────────────────────────
        public async Task<IActionResult> Technicians()
        {
            var techs = await _context.Technicians
                .Select(t => new
                {
                    Tech          = t,
                    TicketCount   = _context.Tickets.Count(tk => tk.TechnicianId == t.Id)
                })
                .ToListAsync();

            // Pass as dynamic list to view via ViewBag
            ViewBag.TechData = techs;
            return View(await _context.Technicians.ToListAsync());
        }

        [HttpGet]
        public IActionResult AddTechnician() => View(new Technician());

        [HttpPost]
        public async Task<IActionResult> AddTechnician(Technician model)
        {
            if (ModelState.IsValid)
            {
                _context.Technicians.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Technician added successfully.";
                return RedirectToAction(nameof(Technicians));
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditTechnician(int id)
        {
            var tech = await _context.Technicians.FindAsync(id);
            if (tech == null) return NotFound();
            return View(tech);
        }

        [HttpPost]
        public async Task<IActionResult> EditTechnician(int id, Technician model)
        {
            var tech = await _context.Technicians.FindAsync(id);
            if (tech == null) return NotFound();

            if (ModelState.IsValid)
            {
                tech.FullName = model.FullName;
                tech.Email    = model.Email;
                tech.Phone    = model.Phone;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Technician updated successfully.";
                return RedirectToAction(nameof(Technicians));
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleTechnicianStatus(int id)
        {
            var tech = await _context.Technicians.FindAsync(id);
            if (tech == null) return NotFound();
            tech.IsActive = !tech.IsActive;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Technician '{tech.FullName}' has been {(tech.IsActive ? "activated" : "deactivated")}.";
            return RedirectToAction(nameof(Technicians));
        }

        public async Task<IActionResult> TechnicianDetails(int id)
        {
            var tech = await _context.Technicians.FindAsync(id);
            if (tech == null) return NotFound();
            ViewBag.AssignedTickets = await _context.Tickets.CountAsync(t => t.TechnicianId == id);
            ViewBag.ResolvedTickets = await _context.Tickets.CountAsync(t => t.TechnicianId == id && t.Status == "Resolved");
            return View(tech);
        }

        // ─── CATEGORY MANAGEMENT ─────────────────────────────────────────────────
        public async Task<IActionResult> Categories()
        {
            var categories = await _context.Categories
                .Select(c => new { Cat = c, TicketCount = _context.Tickets.Count(t => t.CategoryId == c.Id) })
                .ToListAsync();
            ViewBag.CategoryData = categories;
            return View(await _context.Categories.ToListAsync());
        }

        [HttpGet]
        public IActionResult AddCategory() => View(new Category());

        [HttpPost]
        public async Task<IActionResult> AddCategory(Category model)
        {
            if (ModelState.IsValid)
            {
                _context.Categories.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Category added successfully.";
                return RedirectToAction(nameof(Categories));
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditCategory(int id)
        {
            var cat = await _context.Categories.FindAsync(id);
            if (cat == null) return NotFound();
            return View(cat);
        }

        [HttpPost]
        public async Task<IActionResult> EditCategory(int id, Category model)
        {
            var cat = await _context.Categories.FindAsync(id);
            if (cat == null) return NotFound();

            if (ModelState.IsValid)
            {
                cat.Name = model.Name;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Category updated successfully.";
                return RedirectToAction(nameof(Categories));
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var cat = await _context.Categories.FindAsync(id);
            if (cat == null) return NotFound();

            bool hasTickets = await _context.Tickets.AnyAsync(t => t.CategoryId == id);
            if (hasTickets)
            {
                TempData["ErrorMessage"] = "This category cannot be deleted because it is being used by existing tickets.";
                return RedirectToAction(nameof(Categories));
            }

            _context.Categories.Remove(cat);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Category deleted successfully.";
            return RedirectToAction(nameof(Categories));
        }
    }
}
