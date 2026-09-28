using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResolveIQ.Data;
using ResolveIQ.Models;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ResolveIQ.Controllers
{
    [Authorize(Roles = "Technician")]
    public class TechnicianController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TechnicianController(ApplicationDbContext context)
        {
            _context = context;
        }

        private async Task<int?> GetCurrentTechnicianId()
        {
            var email = User.FindFirstValue(ClaimTypes.Name);
            var tech  = await _context.Technicians.FirstOrDefaultAsync(t => t.Email == email);
            return tech?.Id;
        }

        public async Task<IActionResult> Dashboard()
        {
            var techId = await GetCurrentTechnicianId();
            if (techId == null) return Content("Technician profile not found.");

            ViewBag.AssignedCount   = await _context.Tickets.CountAsync(t => t.TechnicianId == techId && t.Status == "Assigned");
            ViewBag.InProgressCount = await _context.Tickets.CountAsync(t => t.TechnicianId == techId && t.Status == "In Progress");
            ViewBag.ResolvedCount   = await _context.Tickets.CountAsync(t => t.TechnicianId == techId && t.Status == "Resolved");
            ViewBag.HighPriority    = await _context.Tickets.CountAsync(t => t.TechnicianId == techId && t.Priority == "High");

            var recentTickets = await _context.Tickets
                .Include(t => t.User)
                .Include(t => t.Category)
                .Where(t => t.TechnicianId == techId)
                .OrderByDescending(t => t.CreatedDate)
                .Take(5)
                .ToListAsync();

            ViewBag.RecentTickets = recentTickets;
            return View();
        }

        public async Task<IActionResult> AssignedTickets()
        {
            var techId = await GetCurrentTechnicianId();
            if (techId == null) return Content("Technician profile not found.");

            var tickets = await _context.Tickets
                .Include(t => t.User)
                .Include(t => t.Category)
                .Where(t => t.TechnicianId == techId)
                .OrderByDescending(t => t.CreatedDate)
                .ToListAsync();

            return View(tickets);
        }

        public async Task<IActionResult> Details(int id)
        {
            var techId = await GetCurrentTechnicianId();
            var ticket = await _context.Tickets
                .Include(t => t.User)
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t => t.Id == id && t.TechnicianId == techId);

            if (ticket == null) return NotFound();
            return View(ticket);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateTicket(int id, string status, string resolutionNote)
        {
            var techId = await GetCurrentTechnicianId();
            var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id && t.TechnicianId == techId);
            if (ticket == null) return NotFound();

            var validStatuses = new[] { "Assigned", "In Progress", "Resolved", "Closed" };
            if (validStatuses.Contains(status))
                ticket.Status = status;

            ticket.ResolutionNote = resolutionNote;
            ticket.UpdatedDate    = DateTime.Now;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Ticket updated successfully.";
            return RedirectToAction(nameof(Details), new { id = ticket.Id });
        }
    }
}
