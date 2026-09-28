using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ResolveIQ.Data;
using ResolveIQ.Models;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using ResolveIQ.DTOs;
using ResolveIQ.Services;

namespace ResolveIQ.Controllers
{
    [Authorize(Roles = "User")]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAiService _aiService;

        public UserController(ApplicationDbContext context, IAiService aiService)
        {
            _context = context;
            _aiService = aiService;
        }

        private int GetCurrentUserId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idClaim, out var id) ? id : 0;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userId = GetCurrentUserId();

            ViewBag.TotalTickets    = await _context.Tickets.CountAsync(t => t.UserId == userId);
            ViewBag.OpenTickets     = await _context.Tickets.CountAsync(t => t.UserId == userId && t.Status == "Open");
            ViewBag.InProgress      = await _context.Tickets.CountAsync(t => t.UserId == userId && t.Status == "In Progress");
            ViewBag.ResolvedTickets = await _context.Tickets.CountAsync(t => t.UserId == userId && t.Status == "Resolved");
            ViewBag.ClosedTickets   = await _context.Tickets.CountAsync(t => t.UserId == userId && t.Status == "Closed");

            var recentTickets = await _context.Tickets
                .Include(t => t.Category)
                .Include(t => t.Technician)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedDate)
                .Take(5)
                .ToListAsync();

            ViewBag.RecentTickets = recentTickets;
            return View();
        }

        public async Task<IActionResult> MyTickets(string searchString, string statusFilter, string priorityFilter)
        {
            var userId = GetCurrentUserId();
            var query  = _context.Tickets
                .Include(t => t.Category)
                .Include(t => t.Technician)
                .Where(t => t.UserId == userId);

            if (!string.IsNullOrEmpty(searchString))
                query = query.Where(t => t.Title.Contains(searchString));
            if (!string.IsNullOrEmpty(statusFilter))
                query = query.Where(t => t.Status == statusFilter);
            if (!string.IsNullOrEmpty(priorityFilter))
                query = query.Where(t => t.Priority == priorityFilter);

            ViewData["CurrentSearch"]   = searchString;
            ViewData["CurrentStatus"]   = statusFilter;
            ViewData["CurrentPriority"] = priorityFilter;

            return View(await query.OrderByDescending(t => t.CreatedDate).ToListAsync());
        }

        [HttpGet]
        public IActionResult CreateTicket()
        {
            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateTicket(Ticket ticket)
        {
            if (ModelState.IsValid)
            {
                ticket.UserId      = GetCurrentUserId();
                ticket.CreatedDate = DateTime.Now;
                ticket.Status      = "Open";
                _context.Tickets.Add(ticket);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Ticket created successfully.";
                return RedirectToAction(nameof(MyTickets));
            }
            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", ticket.CategoryId);
            return View(ticket);
        }

        public async Task<IActionResult> Details(int id)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Category)
                .Include(t => t.Technician)
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == GetCurrentUserId());
            if (ticket == null) return NotFound();
            return View(ticket);
        }

        [HttpGet]
        public async Task<IActionResult> EditTicket(int id)
        {
            var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id && t.UserId == GetCurrentUserId());
            if (ticket == null) return NotFound();
            if (ticket.Status != "Open") return Forbid();
            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", ticket.CategoryId);
            return View(ticket);
        }

        [HttpPost]
        public async Task<IActionResult> EditTicket(int id, Ticket model)
        {
            var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id && t.UserId == GetCurrentUserId());
            if (ticket == null) return NotFound();
            if (ticket.Status != "Open") return Forbid();

            if (ModelState.IsValid)
            {
                ticket.Title       = model.Title;
                ticket.Description = model.Description;
                ticket.CategoryId  = model.CategoryId;
                ticket.Priority    = model.Priority;
                ticket.UpdatedDate = DateTime.Now;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Ticket updated successfully.";
                return RedirectToAction(nameof(MyTickets));
            }
            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", model.CategoryId);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> DeleteTicket(int id)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == GetCurrentUserId());
            if (ticket == null) return NotFound();
            if (ticket.Status != "Open") return Forbid();
            return View(ticket);
        }

        [HttpPost, ActionName("DeleteTicket")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id && t.UserId == GetCurrentUserId());
            if (ticket == null) return NotFound();
            if (ticket.Status != "Open") return Forbid();
            _context.Tickets.Remove(ticket);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Ticket deleted successfully.";
            return RedirectToAction(nameof(MyTickets));
        }

        // ─── AI ASSISTANCE ENDPOINTS (Suggestions Only) ─────────────────────────

        [HttpPost]
        public async Task<IActionResult> GenerateAiSummary([FromBody] AiSummaryRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Description))
            {
                return Json(new { success = false, message = "Please enter a ticket description first." });
            }

            try
            {
                var summary = await _aiService.GenerateSummaryAsync(request.Description);
                return Json(new { success = true, summary });
            }
            catch (AiServiceException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "AI service is currently unavailable. You can continue creating the ticket manually." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SuggestCategory([FromBody] AiSuggestionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Title) && string.IsNullOrWhiteSpace(request?.Description))
            {
                return Json(new { success = false, message = "Please enter a title or description first." });
            }

            try
            {
                var categories = await _context.Categories.Select(c => c.Name).ToListAsync();
                var suggestedCategory = await _aiService.SuggestCategoryAsync(request.Title, request.Description, categories);
                return Json(new { success = true, category = suggestedCategory });
            }
            catch (AiServiceException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "AI service is currently unavailable. You can continue creating the ticket manually." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SuggestPriority([FromBody] AiSuggestionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Title) && string.IsNullOrWhiteSpace(request?.Description))
            {
                return Json(new { success = false, message = "Please enter a title or description first." });
            }

            try
            {
                var suggestedPriority = await _aiService.SuggestPriorityAsync(request.Title, request.Description);
                return Json(new { success = true, priority = suggestedPriority });
            }
            catch (AiServiceException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "AI service is currently unavailable. You can continue creating the ticket manually." });
            }
        }
    }
}
