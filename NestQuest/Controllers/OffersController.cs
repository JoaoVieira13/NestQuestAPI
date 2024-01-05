using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NestQuest.Models;

namespace NestQuest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OffersController : ControllerBase
    {
        private readonly NestQuesteContext _context;

        public OffersController(NestQuesteContext context)
        {
            _context = context;
        }

        // GET: api/Offers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Offer>>> GetOffers()
        {
            var offers = await _context.Offers
                .Include(o => o.Category)
                .Include(o => o.CreatedBy)
                .Include(o => o.Comments)
                .Include(o => o.Hirings)
                .ToListAsync();

            if (offers == null)
            {
                return NotFound();
            }

            return offers;
        }

        // GET: api/Offers/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Offer>> GetOffer(int id)
        {
            var offer = await _context.Offers
                .Include(o => o.Category)
                .Include(o => o.CreatedBy)
                .Include(o => o.Comments)
                .Include(o => o.Hirings)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (offer == null)
            {
                return NotFound();
            }

            return offer;
        }

        // PUT: api/Offers/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutOffer(int id, Offer offer)
        {
            if (id != offer.Id)
            {
                return BadRequest();
            }

            _context.Entry(offer).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OfferExists(id))
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

        // POST: api/Offers
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Offer>> PostOffer(Offer offer)
        {
            // Verificar se a categoria já existe no contexto pelo nome
            var existingCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.Name == offer.Category.Name);

            // Se a categoria não existir, adicioná-la ao contexto
            if (existingCategory == null)
            {
                // Criar uma nova categoria sem especificar o ID
                var newCategory = new Category { Name = offer.Category.Name };

                // Adicionar a nova categoria ao contexto
                _context.Categories.Add(newCategory);

                // Salvar as mudanças para obter o ID gerado
                await _context.SaveChangesAsync();

                // Atribuir a nova categoria à oferta
                offer.Category = newCategory;
            }
            else
            {
                // Se a categoria existir, associar a oferta a essa categoria
                offer.Category = existingCategory;
            }

            // Verificar se o usuário existe no contexto
            if (offer.CreatedBy != null)
            {
                // Tentar encontrar o usuário no contexto pelo ID
                var existingUser = await _context.Users.FindAsync(offer.CreatedBy.Id);

                // Se o usuário não existir, retornar um erro
                if (existingUser == null)
                {
                    return BadRequest("O usuário especificado não existe.");
                }

                // Atribuir o usuário à oferta
                offer.CreatedBy = existingUser;
            }

            // Adicionar a oferta ao contexto
            _context.Offers.Add(offer);

            // Salvar as mudanças no banco de dados
            await _context.SaveChangesAsync();

            // Retornar a resposta
            return CreatedAtAction("GetOffer", new { id = offer.Id }, offer);
        }

        // DELETE: api/Offers/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOffer(int id)
        {
            if (_context.Offers == null)
            {
                return NotFound();
            }
            var offer = await _context.Offers.FindAsync(id);
            if (offer == null)
            {
                return NotFound();
            }

            _context.Offers.Remove(offer);
            await _context.SaveChangesAsync();

            return NoContent(); 
        }

        // POST: api/Offers/5/status
        [HttpPost("{id}/status")]
        public async Task<IActionResult> PutOfferStatus(int id, [FromBody] UpdateOfferStatus updateOffer)
        {
            if (updateOffer == null)
            {
                return BadRequest();
            }
            
            var existingOffer = await _context.Offers.FindAsync(id);

            if (existingOffer == null)
            {
                return NotFound();
            }

            existingOffer.Status = updateOffer.Status;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OfferExists(id))
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

        // GET: api/Offers/search
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Offer>>> SearchOffers([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Search query cannot be empty!");

            var searchResults = await _context.Offers
                .Include(o => o.Category)
                .Include(o => o.CreatedBy)
                .Include(o => o.Comments)
                .Where(o => EF.Functions.Like(o.Title, $"%{query}%") || EF.Functions.Like(o.Description, $"%{query}%"))
                .ToListAsync();

            return Ok(searchResults);
        }

        // GET: api/Offers/filter
        [HttpGet("filter")]
        public async Task<ActionResult<IEnumerable<Offer>>> SearchOffers(
            [FromQuery] string? query,
            [FromQuery] int? categoryId,
            [FromQuery] int? placeId)
        {
            if (string.IsNullOrWhiteSpace(query) && !categoryId.HasValue && !placeId.HasValue)
            {
                return BadRequest("Search query, category, or place must be provided!");
            }

            var queryable = _context.Offers
                .Include(o => o.Category)
                .Include(o => o.CreatedBy)
                .Include(o => o.Comments)
                .Where(o => string.IsNullOrWhiteSpace(query) || EF.Functions.Like(o.Title, $"%{query}%") || EF.Functions.Like(o.Description, $"%{query}%"));

            if (categoryId.HasValue)
            {
                queryable = queryable.Where(o => o.Category.Id == categoryId.Value);
            }

            if (placeId.HasValue)
            {
                queryable = queryable.Where(o => (int)o.Place == placeId.Value);
            }
            
            var searchResults = await queryable.ToListAsync();

            return Ok(searchResults);
        }

        private bool OfferExists(int id)
        {
            return (_context.Offers?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
