using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResolveIQ.Data;
using ResolveIQ.DTOs;
using ResolveIQ.Models;
using ResolveIQ.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ResolveIQ.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TicketsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAiService _aiService;

        public TicketsApiController(ApplicationDbContext context, IAiService aiService)
        {
            _context = context;
            _aiService = aiService;
        }

        // =========================================================================
        // 1. GET ALL TICKETS: GET /api/TicketsApi
        // =========================================================================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TicketDto>>> GetAllTickets()
        {
            var tickets = await _context.Tickets
                .Include(t => t.Category)
                .Include(t => t.User)
                .Include(t => t.Technician)
                .OrderByDescending(t => t.CreatedDate)
                .Select(t => new TicketDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    Priority = t.Priority,
                    Status = t.Status,
                    CreatedDate = t.CreatedDate,
                    UpdatedDate = t.UpdatedDate,
                    ResolutionNote = t.ResolutionNote,
                    UserId = t.UserId,
                    UserName = t.User != null ? t.User.FullName : string.Empty,
                    CategoryId = t.CategoryId,
                    CategoryName = t.Category != null ? t.Category.Name : string.Empty,
                    TechnicianId = t.TechnicianId,
                    TechnicianName = t.Technician != null ? t.Technician.FullName : null
                })
                .ToListAsync();

            return Ok(tickets);
        }

        // =========================================================================
        // 2. GET TICKET BY ID: GET /api/TicketsApi/{id}
        // =========================================================================
        [HttpGet("{id}")]
        public async Task<ActionResult<TicketDto>> GetTicketById(int id)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Category)
                .Include(t => t.User)
                .Include(t => t.Technician)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null)
            {
                return NotFound(new { message = $"Ticket with ID {id} was not found." });
            }

            var dto = new TicketDto
            {
                Id = ticket.Id,
                Title = ticket.Title,
                Description = ticket.Description,
                Priority = ticket.Priority,
                Status = ticket.Status,
                CreatedDate = ticket.CreatedDate,
                UpdatedDate = ticket.UpdatedDate,
                ResolutionNote = ticket.ResolutionNote,
                UserId = ticket.UserId,
                UserName = ticket.User?.FullName ?? string.Empty,
                CategoryId = ticket.CategoryId,
                CategoryName = ticket.Category?.Name ?? string.Empty,
                TechnicianId = ticket.TechnicianId,
                TechnicianName = ticket.Technician?.FullName
            };

            return Ok(dto);
        }

        // =========================================================================
        // 3. CREATE TICKET: POST /api/TicketsApi
        // =========================================================================
        [HttpPost]
        public async Task<ActionResult<TicketDto>> CreateTicket([FromBody] CreateTicketDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Validate that the assigned User exists
            var user = await _context.Users.FindAsync(createDto.UserId);
            if (user == null)
            {
                return BadRequest(new { message = $"User with ID {createDto.UserId} does not exist." });
            }

            // Validate that the Category exists
            var category = await _context.Categories.FindAsync(createDto.CategoryId);
            if (category == null)
            {
                return BadRequest(new { message = $"Category with ID {createDto.CategoryId} does not exist." });
            }

            // Validate priority values
            var validPriorities = new[] { "Low", "Medium", "High" };
            if (!validPriorities.Contains(createDto.Priority))
            {
                return BadRequest(new { message = "Priority must be either 'Low', 'Medium', or 'High'." });
            }

            // Server generates CreatedDate and initial Status = Open
            var ticket = new Ticket
            {
                Title = createDto.Title,
                Description = createDto.Description,
                Priority = createDto.Priority,
                CategoryId = createDto.CategoryId,
                UserId = createDto.UserId,
                Status = "Open",
                CreatedDate = DateTime.Now
            };

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            var responseDto = new TicketDto
            {
                Id = ticket.Id,
                Title = ticket.Title,
                Description = ticket.Description,
                Priority = ticket.Priority,
                Status = ticket.Status,
                CreatedDate = ticket.CreatedDate,
                UpdatedDate = ticket.UpdatedDate,
                ResolutionNote = ticket.ResolutionNote,
                UserId = ticket.UserId,
                UserName = user.FullName,
                CategoryId = ticket.CategoryId,
                CategoryName = category.Name,
                TechnicianId = null,
                TechnicianName = null
            };

            return CreatedAtAction(nameof(GetTicketById), new { id = ticket.Id }, responseDto);
        }

        // =========================================================================
        // 4. UPDATE TICKET: PUT /api/TicketsApi/{id}
        // =========================================================================
        [HttpPut("{id}")]
        public async Task<ActionResult<TicketDto>> UpdateTicket(int id, [FromBody] UpdateTicketDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var ticket = await _context.Tickets
                .Include(t => t.User)
                .Include(t => t.Category)
                .Include(t => t.Technician)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null)
            {
                return NotFound(new { message = $"Ticket with ID {id} was not found." });
            }

            // Validate Category
            var category = await _context.Categories.FindAsync(updateDto.CategoryId);
            if (category == null)
            {
                return BadRequest(new { message = $"Category with ID {updateDto.CategoryId} does not exist." });
            }

            // Validate Technician if assigned
            Technician? technician = null;
            if (updateDto.TechnicianId.HasValue)
            {
                technician = await _context.Technicians.FindAsync(updateDto.TechnicianId.Value);
                if (technician == null)
                {
                    return BadRequest(new { message = $"Technician with ID {updateDto.TechnicianId.Value} does not exist." });
                }
            }

            // Validate Status
            var validStatuses = new[] { "Open", "Assigned", "In Progress", "Resolved", "Closed" };
            if (!validStatuses.Contains(updateDto.Status))
            {
                return BadRequest(new { message = "Status must be 'Open', 'Assigned', 'In Progress', 'Resolved', or 'Closed'." });
            }

            // Validate Priority
            var validPriorities = new[] { "Low", "Medium", "High" };
            if (!validPriorities.Contains(updateDto.Priority))
            {
                return BadRequest(new { message = "Priority must be 'Low', 'Medium', or 'High'." });
            }

            // Update only designated fields
            ticket.Title = updateDto.Title;
            ticket.Description = updateDto.Description;
            ticket.Priority = updateDto.Priority;
            ticket.Status = updateDto.Status;
            ticket.CategoryId = updateDto.CategoryId;
            ticket.TechnicianId = updateDto.TechnicianId;
            ticket.ResolutionNote = updateDto.ResolutionNote;
            ticket.UpdatedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            var responseDto = new TicketDto
            {
                Id = ticket.Id,
                Title = ticket.Title,
                Description = ticket.Description,
                Priority = ticket.Priority,
                Status = ticket.Status,
                CreatedDate = ticket.CreatedDate,
                UpdatedDate = ticket.UpdatedDate,
                ResolutionNote = ticket.ResolutionNote,
                UserId = ticket.UserId,
                UserName = ticket.User?.FullName ?? string.Empty,
                CategoryId = ticket.CategoryId,
                CategoryName = category.Name,
                TechnicianId = ticket.TechnicianId,
                TechnicianName = technician?.FullName ?? ticket.Technician?.FullName
            };

            return Ok(responseDto);
        }

        // =========================================================================
        // 5. DELETE TICKET: DELETE /api/TicketsApi/{id}
        // =========================================================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTicket(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null)
            {
                return NotFound(new { message = $"Ticket with ID {id} was not found." });
            }

            _context.Tickets.Remove(ticket);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // =========================================================================
        // AI ASSISTANCE ENDPOINTS (Suggestions Only)
        // =========================================================================

        [HttpPost("ai/summary")]
        public async Task<ActionResult<AiResponseDto>> GenerateSummary([FromBody] AiSummaryRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Description))
            {
                return BadRequest(new AiResponseDto { Success = false, Message = "Description cannot be empty for AI summary generation." });
            }

            try
            {
                var summary = await _aiService.GenerateSummaryAsync(request.Description);
                return Ok(new AiResponseDto { Success = true, Result = summary });
            }
            catch (AiServiceException ex)
            {
                return Ok(new AiResponseDto { Success = false, Message = ex.Message });
            }
            catch (Exception)
            {
                return Ok(new AiResponseDto { Success = false, Message = "AI service is currently unavailable. You can continue creating the ticket manually." });
            }
        }

        [HttpPost("ai/suggest-category")]
        public async Task<ActionResult<AiResponseDto>> SuggestCategory([FromBody] AiSuggestionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Title) && string.IsNullOrWhiteSpace(request?.Description))
            {
                return BadRequest(new AiResponseDto { Success = false, Message = "Title or description must be provided." });
            }

            try
            {
                var categories = await _context.Categories.Select(c => c.Name).ToListAsync();
                var suggestedCategory = await _aiService.SuggestCategoryAsync(request.Title, request.Description, categories);
                return Ok(new AiResponseDto { Success = true, Result = suggestedCategory });
            }
            catch (AiServiceException ex)
            {
                return Ok(new AiResponseDto { Success = false, Message = ex.Message });
            }
            catch (Exception)
            {
                return Ok(new AiResponseDto { Success = false, Message = "AI service is currently unavailable. You can continue creating the ticket manually." });
            }
        }

        [HttpPost("ai/suggest-priority")]
        public async Task<ActionResult<AiResponseDto>> SuggestPriority([FromBody] AiSuggestionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Title) && string.IsNullOrWhiteSpace(request?.Description))
            {
                return BadRequest(new AiResponseDto { Success = false, Message = "Title or description must be provided." });
            }

            try
            {
                var suggestedPriority = await _aiService.SuggestPriorityAsync(request.Title, request.Description);
                return Ok(new AiResponseDto { Success = true, Result = suggestedPriority });
            }
            catch (AiServiceException ex)
            {
                return Ok(new AiResponseDto { Success = false, Message = ex.Message });
            }
            catch (Exception)
            {
                return Ok(new AiResponseDto { Success = false, Message = "AI service is currently unavailable. You can continue creating the ticket manually." });
            }
        }
    }
}
