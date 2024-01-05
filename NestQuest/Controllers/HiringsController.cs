using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NestQuest.Enum;
using NestQuest.Models;

namespace NestQuest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HiringsController : ControllerBase
    {
        private readonly NestQuesteContext _context;

        public HiringsController(NestQuesteContext context)
        {
            _context = context;
        }

        // GET: api/Hirings
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Hiring>>> GetHirings()
        {
          if (_context.Hirings == null)
          {
              return NotFound();
          }
            return await _context.Hirings.ToListAsync();
        }

        // GET: api/HiringsByUserOffers/{userId}
        [HttpGet("HiringsByUserOffers/{userId}")]
        public async Task<ActionResult<IEnumerable<Hiring>>> GetHiringsByUserOffers(int userId)
        {
            var hirings = await _context.Hirings
                .Where(h => _context.Offers.Any(o => o.Id == h.OfferId && o.CreatedBy.Id == userId))
                .ToListAsync();

            if (hirings == null)
            {
                return NotFound();
            }

            return hirings;
        }

        // GET: api/Hirings/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Hiring>> GetHiring(int id)
        {
          if (_context.Hirings == null)
          {
              return NotFound();
          }
            var hiring = await _context.Hirings.FindAsync(id);

            if (hiring == null)
            {
                return NotFound();
            }

            return hiring;
        }

        // PUT: api/Hirings/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutHiring(int id, Hiring hiring)
        {
            if (id != hiring.Id)
            {
                return BadRequest();
            }

            _context.Entry(hiring).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!HiringExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Hirings
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Hiring>> PostHiring(Hiring hiring)
        {
            // Carrega a oferta associada e verifica o status
            var offer = _context.Offers.FirstOrDefault(o => o.Id == hiring.OfferId);

            if (offer == null)
            {
                return BadRequest("Oferta não encontrada");
            }

            if (offer.StatusName == "CLOSED")
            {
                return BadRequest("A oferta está fechada e não pode ser contratada.");
            }

            _context.Hirings.Add(hiring);
            _context.SaveChanges();

            return Ok(hiring);
        }

        // DELETE: api/Hirings/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteHiring(int id)
        {
            if (_context.Hirings == null)
            {
                return NotFound();
            }
            var hiring = await _context.Hirings.FindAsync(id);
            if (hiring == null)
            {
                return NotFound();
            }

            _context.Hirings.Remove(hiring);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool HiringExists(int id)
        {
            return (_context.Hirings?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
